using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Castle.MonoRail.Framework;
using Common.Tools;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.Models.Editor;
using InforoomInternet.Models;
using InternetInterface.Models;

namespace InforoomInternet.Controllers
{
	[Layout("Main")]
	[Filter(ExecuteWhen.BeforeAction, typeof (NHibernateFilter))]
	[Filter(ExecuteWhen.BeforeAction, typeof (BeforeFilter))]
	public class MainController : SmartDispatcherController
	{
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
			var lease = Lease.FindAll();
			mailToAdress = "kvasovtest@analit.net";
#else
			var lease = Lease.FindAllByProperty("Ip", Convert.ToUInt32(NetworkSwitches.SetProgramIp(ip)));
#endif
			var client = lease.Where(
				l => l.Endpoint != null && l.Endpoint.Client != null && l.Endpoint.Client.PhysicalClient != null).
				Select(l => l.Endpoint.Client).FirstOrDefault();
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
						if (userClient.Contacts != null && !userClient.Contacts.Select(c => c.Text).Contains(contactInfo))
							new Contact {
								Client = userClient,
								Date = DateTime.Now,
								Text = contactInfo,
								Type = ContactType.ConnectedPhone
							}.Save();
					}
					var message = new MailMessage();
					message.To.Add(mailToAdress);
					message.Subject = "Принято новое обращение клиента";
					message.From = new MailAddress("internet@ivrn.net");
					message.Body = Text.ToString();
					var smtp = new SmtpClient("box.analit.net");
					smtp.Send(message);
					RedirectToAction("MessageSended", new Dictionary<string, string> {{"clientName", clientName}});
				}
			}

			ArHelper.WithSession(s => s.Evict(client));
		}

		public void Assist()
		{
			if (Request != null)
				if (Request.Headers["X-Requested-With"] != null)
					if (Request.Headers["X-Requested-With"].Equals("XMLHttpRequest")) {
						var queryString = Request.QueryString.Get("q");
						if (!String.IsNullOrEmpty(queryString)) {
							Street.GetStreetList(queryString, Response.Output);
						}
						else {
							return;
						}
					}
					else {
						return;
					}
			CancelLayout();
			CancelView();
		}

		[AccessibleThrough(Verb.Post)]
		public void Send([DataBind("application")] Request application)
		{
			if (Validator.IsValid(application)) {
				application.RegDate = DateTime.Now;
				application.ActionDate = DateTime.Now;
				var phoneNumber = application.ApplicantPhoneNumber.Substring(2, application.ApplicantPhoneNumber.Length - 2).Replace(
					"-", string.Empty);
				application.ApplicantPhoneNumber = phoneNumber;
				application.Save();
				Flash["application"] = application;
				RedirectToAction("Ok");
			}
			else {
				var all = Tariff.FindAll();
				PropertyBag["tariffs"] = all;
				PropertyBag["application"] = application;
				RenderView("Zayavka");
			}
		}

		public void Warning()
		{
			var hostAdress = Request.UserHostAddress;
#if DEBUG
			hostAdress = "127.0.0.1";
#endif

			var lease = Client.FindByIP(hostAdress);
#if DEBUG
			if (lease == null)
				lease = new Lease
				{
					Endpoint = new ClientEndpoint
					{
						Client = new Client() {
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
				var thisIp = new IPAddress(RangeFinder.reverseBytesArray(Convert.ToUInt32(NetworkSwitches.SetProgramIp(hostAdress))));

				point = StaticIp.Queryable.ToList().Where(ip => {
					if (ip.Ip == hostAdress)
						return true;
					if (ip.Mask != null)
					{
						var subnet = SubnetMask.CreateByNetBitLength(ip.Mask.Value);
						var sIp = new IPAddress(RangeFinder.reverseBytesArray(Convert.ToUInt32(NetworkSwitches.SetProgramIp(ip.Ip))));
						if (thisIp.IsInSameSubnet(sIp, subnet))
							return true;
					}
					return false;
				}).Select(s => s.EndPoint).FirstOrDefault();
			}

			if (point == null) {
				Redirecter.RedirectRoot(Context, this);
				return;
			}

			var clientW = lease != null ? lease.Endpoint.Client : point.Client;

			if (IsPost) {
				if (lease != null) {
					SceHelper.Login(lease, Request.UserHostAddress);
				}
				else {
					var ips = StaticIp.Queryable.Where(s => s.EndPoint == point).ToList();
					foreach (var staticIp in ips) {
						if (point.PackageId == null)
							continue;
						SceHelper.Action("login", staticIp.Mask != null ? staticIp.Ip + "/" + staticIp.Mask : staticIp.Ip, "Static_" + staticIp.Id, false, false, point.PackageId.Value);
					}
				}
				clientW.ShowBalanceWarningPage = false;
				clientW.Update();
				var url = Request.Form["referer"];
				if (String.IsNullOrEmpty(url))
					Redirecter.RedirectRoot(Context, this);
				else
					RedirectToUrl(string.Format("http://{0}", url));
				return;
			}

			var host = Request["host"];
			var rUrl = Request["url"];
			if (!string.IsNullOrEmpty(host))
				PropertyBag["referer"] = host + rUrl;
			else PropertyBag["referer"] = string.Empty;

			var pclient = clientW.PhysicalClient;
			var client = clientW;

			PropertyBag["Client"] = client;

			if (clientW.PhysicalClient == null) {
				PropertyBag["LClient"] = client.LawyerPerson;
				RenderView("WarningLawyer");
				return;
			}

			PropertyBag["PClient"] = pclient;
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
			var hostAdress = Request.UserHostAddress;
#if DEBUG
			hostAdress = NetworkSwitches.GetNormalIp(Lease.FindFirst().Ip);
#endif
			if (Regex.IsMatch(hostAdress, NetworkSwitches.IPRegExp) && !Client.Our(hostAdress)) {
				RedirectToSiteRoot();
				return;
			}

			var lease = Client.FindByIP(hostAdress);
			if (lease == null || lease.Endpoint == null || lease.Endpoint.Client == null) {
				SendMessage(null);
			}
			else {
				if (ClientData.Get(lease.Endpoint.Client.Id) == UnknownClientStatus.NoInfo) {
					var sceWorker = new SceThread(lease, hostAdress);
					sceWorker.Go();
				}

				PropertyBag["client"] = lease.Endpoint.Client.Id;
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
				Redirecter.RedirectRoot(Context, this);
			else
				RedirectToUrl(string.Format("http://{0}", url));
			return;
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
К сожалению, услуга доступа интернет Вам временно заблокирована. </br>
Если Вы считаете это ошибочным, пожалуйста, свяжитесь с нами по телефону </br> (473)22-999-87 или сообщите по адресу internet@ivrn.net. Спасибо.",
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
			var smtp = new SmtpClient("box.analit.net");
			smtp.Send(message);
		}
	}
}