using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Tools;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.Models.Editor;
using InforoomInternet.Models;
using InternetInterface.Controllers;
using InternetInterface.Models;
using NHibernate.Linq;

namespace InforoomInternet.Controllers
{
	[Layout("Main")]
	[Filter(ExecuteWhen.BeforeAction, typeof(NHibernateFilter))]
	[Filter(ExecuteWhen.BeforeAction, typeof(BeforeFilter))]
	public class MainController : BaseController
	{
		private string GetHost()
		{
			var hostAdress = Request.UserHostAddress;
#if DEBUG
			hostAdress = "127.0.0.1";
#endif
			return hostAdress;
		}

		private Lease FindLease()
		{
			var ip = IPAddress.Parse(GetHost());
			return DbSession.Query<Lease>().FirstOrDefault(l => l.Ip == ip);
		}

		public void Index()
		{
			PropertyBag["tariffs"] = Tariff.FindAll();
		}

		public void Zayavka()
		{
			PropertyBag["tariffs"] = Tariff.FindAll();
		}

		public void Main()
		{
		}

		public void HowPay(bool edit)
		{
			SetEdatableAttribute(edit, "HowPay");
		}

		public void Ok()
		{
			if (Flash["application"] == null)
				RedirectToSiteRoot();
		}

		public void OfferContract(bool edit)
		{
		}

		public void PrivateOffice()
		{
		}

		public void Feedback(string clientName, string contactInfo, uint clientId, string appealText, uint autoClientId)
		{
			var ip = Request.UserHostAddress;
			var mailToAdress = "internet@ivrn.net";
#if DEBUG
			mailToAdress = "kvasovtest@analit.net";
#endif
			var lease = FindLease();
			var client = lease.Endpoint != null ? lease.Endpoint.Client : null;
			client = client ?? new Client();
			PropertyBag["client"] = client;

			if (IsPost) {
				var Text = new StringBuilder();
				Text.AppendLine("Обращение клиента через сайт WWW.IVRN.NET \r\n");
				Text.AppendLine("Клиент пришел с адреса: " + ip);
				Text.AppendLine(string.Format("Наша система определила запрос с лицевого счета номер: {0} \r\n", autoClientId));
				Text.AppendLine("Введена информация : \r\n");
				Text.AppendLine("ФИО: " + clientName);
				Text.AppendLine("Контактная информация: " + contactInfo);
				Text.AppendLine("Номер счета: " + clientId);
				Text.AppendLine("Текст обращения: \r\n" + appealText);
				if (!string.IsNullOrEmpty(clientName) && !string.IsNullOrEmpty(contactInfo) && !string.IsNullOrEmpty(appealText)) {
					var userClient = Client.Queryable.Where(c => c.Id == clientId).FirstOrDefault();
					new Appeals {
						Client = userClient,
						Date = DateTime.Now,
						AppealType = AppealType.FeedBack,
						Appeal = Text.ToString()
					}.Save();
					if (userClient != null) {
						contactInfo = contactInfo.Replace("-", string.Empty);
						if (userClient.Contacts != null && !userClient.Contacts.Select(c => c.Text).Contains(contactInfo)) {
							var contact = new Contact(userClient, ContactType.ConnectedPhone, contactInfo);
							DbSession.Save(contact);
						}
					}
					var message = new MailMessage();
					message.To.Add(mailToAdress);
					message.Subject = "Принято новое обращение клиента";
					message.From = new MailAddress("internet@ivrn.net");
					message.Body = Text.ToString();
					var smtp = new SmtpClient();
					smtp.Send(message);
					RedirectToAction("MessageSended", new Dictionary<string, string> { { "clientName", clientName } });
				}
			}

			DbSession.Evict(client);
		}

		public void Assist(string q)
		{
			if (string.IsNullOrEmpty(q))
				return;

			if (Request == null || Request.Headers["X-Requested-With"] != "XMLHttpRequest")
				return;

			Street.GetStreetList(q, Response.Output);
			CancelLayout();
			CancelView();
		}

		[AccessibleThrough(Verb.Post)]
		public void Send([DataBind("application")] Request request)
		{
			if (Validator.IsValid(request)) {
				request.RegDate = DateTime.Now;
				request.ActionDate = DateTime.Now;
				if (AccessFilter.Authorized(Context)) {
					var clientId = Convert.ToUInt32(Session["LoginClient"]);
					var client = Client.Find(clientId);
					request.FriendThisClient = client;
				}
				var phoneNumber = request.ApplicantPhoneNumber.Substring(2, request.ApplicantPhoneNumber.Length - 2).Replace("-", string.Empty);
				request.ApplicantPhoneNumber = phoneNumber;
				request.Save();
				Flash["application"] = request;
				RedirectToAction("Ok");
			}
			else {
				var all = Tariff.FindAll();
				PropertyBag["tariffs"] = all;
				PropertyBag["application"] = request;
				RenderView("Zayavka");
			}
		}

		public void SelfRegistration(string referer)
		{
			SetARDataBinder(AutoLoadBehavior.NullIfInvalidKey);

			var lease = FindLease();
			if (!lease.CanSelfRegister()) {
				Redirecter.RedirectRoot(this);
			}

			var physicalClient = new PhysicalClient {
				Apartment = 0,
				Entrance = 0,
				Floor = 0,
			};
			physicalClient.ExternalClientIdRequired = true;
			var settings = new Settings(DbSession);
			var client = new Client(physicalClient, settings);

			//тарифы для самостоятельной регистрации всего скорее будут скрыты
			//тк самостоятельная регистрация предназначена для перевода абонентов
			DbSession.DisableFilter("HiddenTariffs");
			PropertyBag["tariffs"] = DbSession.Query<Tariff>().Where(t => t.CanUseForSelfRegistration)
				.OrderBy(t => t.Name)
				.ToList();
			PropertyBag["physicalClient"] = physicalClient;
			PropertyBag["referer"] = referer;

			if (IsPost) {
				BindObjectInstance(physicalClient, "physicalClient", "ExternalClientId, Surname, Name, Patronymic, PhoneNumber, Tariff");
				if (IsValid(physicalClient)) {
					client.SelfRegistration(lease, Status.Get(StatusType.Worked, DbSession));

					foreach (var contact in client.Contacts)
						DbSession.Save(contact);

					foreach (var payment in client.Payments)
						DbSession.Save(payment);

					DbSession.Save(client);
					DbSession.Flush();

					SceHelper.Login(lease, lease.Ip.ToString());

					Flash["password"] = client.GeneragePassword();
					Redirect("PrivateOffice", "Complete", new { referer });
				}
			}
		}

		public void Warning()
		{
			var referer = "";
			if (!string.IsNullOrEmpty(Request["host"]))
				PropertyBag["referer"] = Request["host"] + Request["url"];

			var hostAdress = IPAddress.Parse(GetHost());
			var lease = FindLease();
#if DEBUG
			if (lease == null)
				lease = new Lease {
					Endpoint = new ClientEndpoint {
						Client = new Client {
							ShowBalanceWarningPage = true,
							RatedPeriodDate = DateTime.Now,
							LawyerPerson = new LawyerPerson {
								Balance = -20000,
								Tariff = 10000
							}
						}
					}
				};
#endif

			ClientEndpoint point = null;
			if (lease != null)
				point = lease.Endpoint;
			else {
				point = StaticIp.Queryable.ToList().Where(ip => {
					if (ip.Ip == hostAdress.ToString())
						return true;
					if (ip.Mask != null) {
						var subnet = SubnetMask.CreateByNetBitLength(ip.Mask.Value);
						var sIp = new IPAddress(RangeFinder.reverseBytesArray(Convert.ToUInt32(NetworkSwitch.SetProgramIp(ip.Ip))));
						if (hostAdress.IsInSameSubnet(sIp, subnet))
							return true;
					}
					return false;
				}).Select(s => s.EndPoint).FirstOrDefault();
			}

			if (lease != null && lease.CanSelfRegister()) {
				RedirectToAction("SelfRegistration", new { referer });
				return;
			}

			if (point == null) {
				Redirecter.RedirectRoot(this);
				return;
			}

			var client = point.Client;

			if (IsPost) {
				int? actualPackageId;
				if (lease != null) {
					actualPackageId = SceHelper.Login(lease, Request.UserHostAddress);
					lease.Endpoint.UpdateActualPackageId(actualPackageId);
					DbSession.SaveOrUpdate(lease.Endpoint);
				}
				else {
					var ips = StaticIp.Queryable.Where(s => s.EndPoint == point).ToList();
					foreach (var staticIp in ips) {
						if (point.PackageId == null)
							continue;
						actualPackageId = SceHelper.Action("login", staticIp.Mask != null ? staticIp.Ip + "/" + staticIp.Mask : staticIp.Ip, "Static_" + staticIp.Id, false, false, point.PackageId.Value);
						point.UpdateActualPackageId(actualPackageId);
						DbSession.SaveOrUpdate(point);
					}
				}
				if (client.ShowBalanceWarningPage) {
					client.ShowBalanceWarningPage = false;
					Appeals.CreareAppeal("Отключена страница Warning, клиент отключил со страницы", client, AppealType.Statistic, false);
				}
				client.Update();
				GoToReferer();
				return;
			}

			PropertyBag["referer"] = referer;
			PropertyBag["Client"] = client;
			if (client.PhysicalClient == null) {
				PropertyBag["LClient"] = client.LawyerPerson;
				RenderView("WarningLawyer");
			}
			else {
				PropertyBag["PClient"] = client.PhysicalClient;
			}
		}

		private void SetEdatableAttribute(bool edit, string viewName)
		{
			PropertyBag["Content"] = SiteContent.FindAllByProperty("ViewName", viewName).First().Content;
			if (edit) {
				LayoutName = "TinyMCE";
				PropertyBag["ShowEditLink"] = false;
			}
			else {
				PropertyBag["ShowEditLink"] = true;
			}
		}

		public void WarningPackageId()
		{
			var hostAdress = IPAddress.Parse(Request.UserHostAddress);
#if DEBUG
			hostAdress = DbSession.Query<Lease>().ToList().First().Ip;
#endif
			if (!Client.Our(hostAdress, DbSession)) {
				RedirectToSiteRoot();
				return;
			}

			var lease = FindLease();
			if (lease == null || lease.Endpoint == null || lease.Endpoint.Client == null) {
				SendMessage(null);
			}
			else {
				if (!lease.Endpoint.Client.IsPhysical()) {
					RedirectToSiteRoot();
					return;
				}

				if ((ClientData.Get(lease.Endpoint.Client.Id) == UnknownClientStatus.NoInfo) && !lease.Pool.IsGray) {
					var sceWorker = new SceThread(lease, hostAdress.ToString());
					sceWorker.Go();
				}

				PropertyBag["Client"] = lease.Endpoint.Client;
				PropertyBag["connectIterations"] = !lease.Pool.IsGray;
				var host = Request["host"];
				var rUrl = Request["url"];
				if (!string.IsNullOrEmpty(host))
					PropertyBag["referer"] = host + rUrl;
				else
					PropertyBag["referer"] = string.Empty;
			}
		}

		public void GoToReferer()
		{
			var url = Request.Form["referer"];
			if (String.IsNullOrEmpty(url))
				Redirecter.RedirectRoot(this);
			else
				RedirectToUrl(string.Format("http://{0}", url));
		}

		public void RenewClientConect(uint client)
		{
			ClientData.Set(client, UnknownClientStatus.NoInfo);
			RedirectToReferrer();
		}

		[return: JSONReturnBinder]
		public ReturnInFormInfo GetClientStatus(uint client)
		{
			Thread.Sleep(300);
			var info = ClientData.GetInfo(client);
			if (info != null) {
				if (info.Status == UnknownClientStatus.InProcess)
					return new ReturnInFormInfo {
						Iteration = info.Interation.Value,
						Message = "Ждите, идет подключение к интернет",
						WaitingInfo = true,
						Status = info.Status
					};
				if (info.Status == UnknownClientStatus.Error) {
					SendMessage(client);
					return new ReturnInFormInfo {
						Iteration = 100,
						Message = @"
К сожалению, услуга доступа интернет Вам недоступна. </br>
Чтобы пользоваться услугами интернет необходимо оставить заявку на подлючение, либо авторизоваться, если вы уже подключены.
Если Вы считаете это сообщение ошибочным, пожалуйста, свяжитесь с нами по телефону </br> (473)22-999-87 или сообщите по адресу internet@ivrn.net. Спасибо",
						WaitingInfo = false,
						Status = info.Status
					};
				}
				if (info.Status == UnknownClientStatus.Connected)
					return new ReturnInFormInfo {
						Iteration = 100,
						Message = "Подключение успешно. Вы можете продолжить работу.",
						WaitingInfo = false,
						Status = info.Status
					};
			}
			return new ReturnInFormInfo {
				Iteration = 0,
				Message = "Нет информации о процессе подключения.",
				WaitingInfo = false,
				Status = UnknownClientStatus.NoInfo
			};
		}

		private void SendMessage(uint? client)
		{
			var mailToAdress = "internet@ivrn.net";
			var messageText = new StringBuilder();
#if DEBUG
			mailToAdress = "kvasovtest@analit.net";
#endif
			if (client == null)
				messageText.AppendLine(string.Format("Пришел запрос на страницу WarningPackageId от стороннего клиента (IP: {0})", Request.UserHostAddress));
			else {
				var lease = Lease.Queryable.FirstOrDefault(l => l.Endpoint.Client.Id == client.Value);
				messageText.AppendLine(string.Format("Пришел запрос на страницу WarningPackageId от клиента {0}",
					client.Value.ToString("00000")));
				if (lease.Endpoint.Switch != null) {
					messageText.AppendLine("Свич: " + lease.Endpoint.Switch.Name);
				}
				else {
					messageText.AppendLine("Свич неопределен");
				}
				if (lease.Endpoint.Port != null) {
					messageText.AppendLine("Порт: " + lease.Endpoint.Port);
				}
				else {
					messageText.AppendLine("Порт неопределен");
				}
			}
			var message = new MailMessage();
			message.To.Add(mailToAdress);
			message.Subject = "Преадресация на страницу WarningPackageId";
			message.From = new MailAddress("service@analit.net");
			message.Body = messageText.ToString();
			var smtp = new SmtpClient();
			smtp.Send(message);
		}
	}
}