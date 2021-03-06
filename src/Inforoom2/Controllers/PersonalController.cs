﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Mvc;
using Common.MySql;
using Common.Tools;
using Inforoom2.Components;
using Inforoom2.Helpers;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using Inforoom2.validators;
using NHibernate.Linq;
using AppealType = Inforoom2.Models.AppealType;
using Client = Inforoom2.Models.Client;
using ClientEndpoint = Inforoom2.Models.ClientEndpoint;
using Contact = Inforoom2.Models.Contact;
using Lease = Inforoom2.Models.Lease;
using Payment = Inforoom2.Models.Payment;
using PaymentForConnect = Inforoom2.Models.PaymentForConnect;
using PhysicalClient = Inforoom2.Models.PhysicalClient;
using PlanHistoryEntry = Inforoom2.Models.PlanHistoryEntry;
using Service = Inforoom2.Models.Services.Service;
using Status = Inforoom2.Models.Status;
using StatusType = Inforoom2.Models.StatusType;
using UserWriteOff = Inforoom2.Models.UserWriteOff;
using WriteOff = Inforoom2.Models.WriteOff;
using System.Configuration;
using NHibernate.Properties;

namespace Inforoom2.Controllers
{
	public class PersonalController : Inforoom2Controller
	{
		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			base.OnActionExecuting(filterContext);
			if (filterContext == null) {
				throw new ArgumentNullException("filterContext");
			}
			//если клиент был залогинен по сети, то HTTPСontext не будет изменен
			//в этом случае можно оттолкнуть от переменной CurrentClient
			if (CurrentClient == null && !filterContext.HttpContext.User.Identity.IsAuthenticated) {
				string loginUrl = "/Account/Login"; // Default Login Url 
				filterContext.Result = new RedirectResult(loginUrl);
			}
		}

		[OutputCache(Duration = 600, Location = System.Web.UI.OutputCacheLocation.Server, VaryByParam = "*", VaryByCustom = "User,Cookies")]
		public ActionResult FirstVisit()
		{
			if (CurrentClient.Lunched && CurrentClient.HasPassportData())
				return RedirectToAction("Profile");
			var physicalClient = DbSession.Get<PhysicalClient>(CurrentClient.PhysicalClient.Id);
			//TODO Придумать что с этим делать
			var unproxy = DbSession.GetSessionImplementation().PersistenceContext.Unproxy(physicalClient);
			ViewBag.PhysicalClient = unproxy;
			return View();
		}

		[HttpPost]
		public ActionResult FirstVisit(PhysicalClient physicalClient)
		{
			var oldphysicalClient = DbSession.Query<PhysicalClient>().First(s => s.Id == physicalClient.Id);

			if (oldphysicalClient.Client.Id != CurrentClient.Id) {
				ErrorMessage("Попытка изменить чужие данные.");
                throw new Exception("Попытка изменить чужие данные.");
			}

			oldphysicalClient.UpdateFirstVisitData(physicalClient);

		var errors = ValidationRunner.Validate(oldphysicalClient);
			if (errors.Length == 0) {
				DbSession.Save(oldphysicalClient);

				var errorMessage = "";
				var address = IPAddress.Parse(this.Request.UserHostAddress);
#if DEBUG
				var lease = DbSession.Query<Lease>().FirstOrDefault(l => l.Endpoint == null);
#else
				var lease = DbSession.Query<Lease>().FirstOrDefault(l => l.Ip == address && l.Endpoint == null);
#endif
				if (lease != null) {
					//пытаемся добавить точку подклбючения, если ее нет
					PhysicalClient.EndpointCreateIfNeeds(DbSession, oldphysicalClient.Client, lease, GetCurrentEmployee(),
						ref errorMessage);
				} else {
					if (oldphysicalClient.Client.Endpoints.Count == 0) {
						errorMessage = $"Ошибка: точка подключения не может быть создана, т.к. сессия отсутствует для IP {address}";
					}
				}

				if (!string.IsNullOrEmpty(errorMessage)) {
					ErrorMessage(errorMessage);
				}
				SuccessMessage("Данные успешно заполнены");

				CurrentClient.Lunched = true;
				DbSession.Save(CurrentClient);
				return RedirectToAction("Profile");
			}
			ViewBag.PhysicalClient = oldphysicalClient;
			DbPreventTransactionCommit();
			return View();
		}

		[OutputCache(Duration = 30, Location = System.Web.UI.OutputCacheLocation.Server, VaryByParam = "*", VaryByCustom = "User,Cookies")]
		public new ActionResult Profile()
		{
			if (CurrentClient != null
			    && CurrentClient.RentalHardwareList != null
			    && CurrentClient.StatusChangedOn.HasValue) {
				//Отображение клиенту Варнинга, если через 3 дня начинаютс списания за аренду (и до тех пор, пока он не расчитается и не изменится статус)
				var timeSinceStatusChanged = (SystemTime.Now() - CurrentClient.StatusChangedOn);
				var rentFreeDaysOverCome = CurrentClient.RentalHardwareList.FirstOrDefault(s => s.IsActive &&
				                                                                                timeSinceStatusChanged.Value.Add(
					                                                                                TimeSpan.FromDays(3)) >
				                                                                                TimeSpan.FromDays(
					                                                                                s.Hardware.FreeDays));

				if ((CurrentClient.Status.Type == StatusType.Dissolved || CurrentClient.Status.Type == StatusType.NoWorked) &&
				    rentFreeDaysOverCome != null) {
					var timeSinceWriteOffStarts = (CurrentClient.StatusChangedOn.Value.AddDays(rentFreeDaysOverCome.Hardware.FreeDays));
					string message = "С {0} {1} списания ежедневной платы за аренду";
					if (timeSinceWriteOffStarts.Date > SystemTime.Now().Date) {
						message = string.Format(message, timeSinceWriteOffStarts.ToString("dd.MM.yyyy"), "будут производится");
					}
					else {
						message = string.Format(message, timeSinceWriteOffStarts.ToString("dd.MM.yyyy"), "производятся");
					}
					ErrorMessage(message);
				}
			}


			if (CurrentClient == null)
				return RedirectToAction("Login", "Account");

			if (!CurrentClient.Lunched)
				return RedirectToAction("FirstVisit");

			InitServices();
			ViewBag.Title = "Личный кабинет";
			ViewBag.CurrentClient = CurrentClient;

			var bannerList = DbSession.Query<Banner>().Where(s => s.Enabled
				&& s.Type == BannerType.ForClientPage).OrderBy(s => s.Region).ToList();
			var pathFromConfigUrl = System.Web.Configuration.WebConfigurationManager.AppSettings["inforoom2UploadUrl"];
			if (pathFromConfigUrl == null) {
				throw new Exception("Значение 'inforoom2UploadUrl' отсуствует в Global.config!");
			}
			ViewBag.pathFromConfigURL = pathFromConfigUrl;
			ViewBag.BannerForPage = bannerList.FirstOrDefault(s => s.Region != null && s.Region.Id == CurrentClient.GetRegion().Id)
				?? bannerList.FirstOrDefault(s => s.Region == null);

			return View();
		}

		/// <summary>
		/// Отобажает содержимое файла-плейлиста на экране
		/// </summary>
		/// <returns>Файл плейлиста</returns>
		/// 
		public ActionResult Playlist()
		{
			var text = CurrentClient.Plan.GetPlaylist();
			text = text.Insert(0, "#EXTM3U\n");
			ViewBag.Content = text;
			return View("~/Views/Shared/Empty.cshtml");
		}

		/// <summary>
		/// Генерирует файл каналов для текущего пользователя
		/// </summary>
		/// <returns>Файл плейлиста</returns>
		public FileResult PlaylistLink()
		{
			var text = CurrentClient.Plan.GetPlaylist();
			text = text.Insert(0, "#EXTM3U\n");
			var fileContent = Encoding.UTF8.GetBytes(text);
			var contentType = "audio/mpegurl";
			var file = new FileContentResult(fileContent, contentType);
			file.FileDownloadName = "playlist.m3u";
			return file;
		}

		public ActionResult Plans()
		{
			var stateOfClient = CurrentClient.GetWarningState();
			if (stateOfClient != WarningState.NoWarning) {
#if DEBUG
				var endpoint = CurrentClient.Endpoints.FirstOrDefault();
				var lease = DbSession.Query<Lease>().FirstOrDefault(i => i.Endpoint == endpoint);
				var ipstr = lease?.Ip?.ToString();
				return RedirectToAction(stateOfClient.ToString(), "Warning", new { ip = ipstr });
#else
				return RedirectToAction(stateOfClient.ToString(), "Warning");
#endif
			}

			var client = CurrentClient;
			InitPlans(client);
			ViewBag.Title = "Тарифы";
			ViewBag.Client = client;

			return View();
		}

		[OutputCache(Duration = 300, Location = System.Web.UI.OutputCacheLocation.Server, VaryByParam = "*", VaryByCustom = "User,Cookies")]
		public ActionResult Payment()
		{
			const string writeOffAnaliticsFormat = "<br/><sub class='rentWriteOff'>* аренда оборудования - {0}</sub>";
			const string paymentForRouter = "<br/><sub class='rentWriteOff'>* плата за роутер</sub>";
			const string writeOffRent = "ежедневная плата за аренду";
			ViewBag.Title = "Платежи";
			var client = CurrentClient;
			var userWriteOffs =
				DbSession.Query<UserWriteOff>()
					.Where(uwo => uwo.Client.Id == client.Id && uwo.Date > SystemTime.Now().AddMonths(-3) && !uwo.Ignore);
			var writeOffs =
				DbSession.Query<WriteOff>()
					.Where(wo => wo.Client.Id == client.Id && wo.WriteOffDate > SystemTime.Now().AddMonths(-3));
			var payments =
				DbSession.Query<Payment>().Where(p => p.Client.Id == client.Id && p.RecievedOn > SystemTime.Now().AddMonths(-3));

			var historyList = userWriteOffs.Select(userWriteOff => new BillingHistory {
				Date = userWriteOff.Date,
				Sum = userWriteOff.Sum,
				Description = userWriteOff.Comment
			}).ToList();

			var userWiteOffList = writeOffs.ToList().Select(writeOff => {
				//Комментарий о списании за арендуемое оборудование, при его наличии. 
				var commentWriteOff = "";
				if (writeOff.Comment != null
				    && writeOff.Comment.IndexOf(writeOffRent) != -1
				    && writeOff.Comment.IndexOf(":") != -1) {
					commentWriteOff = string.Format(writeOffAnaliticsFormat,
						writeOff.Comment.Substring(0, writeOff.Comment.IndexOf(":")));
				}
				//создаем эл-т отображаемого списка
				return new BillingHistory {
					Date = writeOff.WriteOffDate,
					Sum = writeOff.WriteOffSum,
					Description = "Абонентская плата" + commentWriteOff
				};
			}).ToList();
			historyList.AddRange(userWiteOffList);

			var paymentsList = new List<BillingHistory>();
			foreach (var payment in payments) {
				var obj = new BillingHistory();
				obj.Date = payment.RecievedOn;
				obj.Sum = payment.Sum;
				obj.Comment = payment.Comment;
				obj.WhoRegistered = "Инфорум";
				if (payment.Employee != null && payment.Employee.IsPaymentSystem())
					obj.WhoRegistered = payment.Employee.Name;
				if (payment.Virtual.HasValue && payment.Virtual.Value)
					obj.WhoRegistered += " (бонус)";
				obj.Description = "Пополнение счета"
					//Комментарий о списании за арендуемое оборудование, при его наличии.
				                  + (obj.Comment != null && obj.Comment.IndexOf("роутер") != -1 ? paymentForRouter : "");
				paymentsList.Add(obj);
			}
			historyList.AddRange(paymentsList);

			historyList = historyList.OrderByDescending(h => h.Date).ToList();
			ViewBag.HistoryList = historyList;
			return View();
		}

		[OutputCache(Duration = 600, Location = System.Web.UI.OutputCacheLocation.Server, VaryByCustom = "User,Cookies")]
		public ActionResult Credit()
		{
			ViewBag.Title = "Доверительный платеж";
			return View();
		}

		[OutputCache(Duration = 300, Location = System.Web.UI.OutputCacheLocation.Server, VaryByCustom = "User,Cookies")]
		public ActionResult UserDetails()
		{
			ViewBag.Title = "Данные пользователя";
			return View();
		}

		public ActionResult Service()
		{
			if (CurrentClient.GetWarningState() == WarningState.PhysPassportData) {
#if DEBUG
				var endpoint = CurrentClient.Endpoints.FirstOrDefault();
				var lease = DbSession.Query<Lease>().FirstOrDefault(i => i.Endpoint == endpoint);
				var ipstr = lease?.Ip?.ToString();
				return RedirectToAction("PhysPassportData", "Warning", new { ip = ipstr });
#else
				return RedirectToAction("PhysPassportData", "Warning");
#endif
			}
			ViewBag.Title = "Услуги";
			InitServices();
			return View();
		}

		public ActionResult Notifications()
		{
			var stateOfClient = CurrentClient.GetWarningState();
			if (stateOfClient == WarningState.PhysLowBalance
			    || stateOfClient == WarningState.PhysPassportData) {
#if DEBUG
				var endpoint = CurrentClient.Endpoints.FirstOrDefault();
				var lease = DbSession.Query<Lease>().FirstOrDefault(i => i.Endpoint == endpoint);
				var ipstr = lease?.Ip?.ToString();
				return RedirectToAction(stateOfClient.ToString(), "Warning", new { ip = ipstr });
#else
				return RedirectToAction(stateOfClient.ToString(), "Warning");
#endif
			}
			var notificationContact = CurrentClient.Contacts.FirstOrDefault(c => c.Type == ContactType.NotificationEmailConfirmed || c.Type == ContactType.NotificationEmailRaw);
			if (notificationContact == null) {
				notificationContact = new Contact();
				notificationContact.Type = ContactType.NotificationEmailRaw;
			}
			ViewBag.Contact = notificationContact;
			ViewBag.Title = "Уведомления";
			return View();
		}

		[HttpPost]
		public ActionResult Notifications(Contact contact)
		{
			contact.ContactString = string.IsNullOrEmpty(contact.ContactString) ? "" : contact.ContactString.Trim();
			var client = CurrentClient;
			var errors = ValidationRunner.Validate(contact);
			//Валидация контакта 
			if (errors.Length == 0) {
				var contactValidation = new ValidatorContacts();
				// Принудительная валидация модели контактов по атрибуту ValidatorContacts
				ViewBag.ContactValidation = contactValidation;
				errors = errors.Length != 0
					? errors
					: ValidationRunner.ForcedValidationByAttribute<Contact>(contact, s => s.ContactString, contactValidation);
			}
			if (errors.Length == 0) {
				var notificationContact = client.Contacts.FirstOrDefault(c => c.Type == ContactType.NotificationEmailRaw || c.Type == ContactType.NotificationEmailConfirmed);
				if (notificationContact == null) {
					notificationContact = contact;
					notificationContact.Date = SystemTime.Now();
					notificationContact.Comment = "Пользователь создал из личного кабинета";
					client.Contacts.Add(notificationContact);
					notificationContact.Client = client;
				}
				else {
					notificationContact.ContactString = contact.ContactString;
				}
				if (notificationContact.Type == ContactType.NotificationEmailConfirmed) {
					notificationContact.ClientNotificationEmailRestore();
					var appeal = new Appeal("Клиент отписался от рассылки почтовых уведомлений", client, AppealType.User, GetCurrentEmployee());
					DbSession.Save(appeal);
					DbSession.Save(notificationContact);
					SuccessMessage("Вы успешно отписались от рассылки почтовых уведомлений");
					DbSession.Save(notificationContact);
					return RedirectToAction("Notifications");
				}
				DbSession.Save(client);
				ViewBag.Contact = notificationContact;
				SuccessMessage("На указанный почтовый адрес выслано сообщение с подтверждением.");
				notificationContact.ClientNotificationEmailConfirmationGet(Url.Action("NotificationsEmailConfirmation"));
				return RedirectToAction("Notifications");
			}
			ViewBag.Contact = contact;
			return View();
		}

		public ActionResult Bonus()
		{
			var stateOfClient = CurrentClient.GetWarningState();
			if (stateOfClient == WarningState.PhysLowBalance
			    || stateOfClient == WarningState.PhysPassportData) {
#if DEBUG
				var endpoint = CurrentClient.Endpoints.FirstOrDefault();
				var lease = DbSession.Query<Lease>().FirstOrDefault(i => i.Endpoint == endpoint);
				var ipstr = lease?.Ip?.ToString();
				return RedirectToAction(stateOfClient.ToString(), "Warning", new { ip = ipstr });
#else
				return RedirectToAction(stateOfClient.ToString(), "Warning");
#endif
			}
			ViewBag.Title = "Бонусы";
			return View();
		}

		public ActionResult NotificationsEmailConfirmation(string id)
		{
			var contact = CurrentClient.PhysicalClient.GetClientNotificationEmail(false);
			var currentType = contact.Type;
			contact.ClientNotificationEmailConfirmationSet(id);
			if (currentType != contact.Type && contact.Type == ContactType.NotificationEmailConfirmed) {
				SuccessMessage("Адрес для рассылки почтовых уведомлений подтвержден!");
				DbSession.Save(contact);
				var appeal = new Appeal("Клиент подписался на рассылку почтовых уведомлений", CurrentClient, AppealType.User, GetCurrentEmployee());
				DbSession.Save(appeal);
			}
			else {
				if (currentType == contact.Type && contact.Type == ContactType.NotificationEmailConfirmed) {
					SuccessMessage("Адрес для рассылки уведомлений уже подтвержден.");
				}
				else {
					ErrorMessage("Подтверждение почтового адреса завершилось ошибкой. Повторите запрос на подтверждение.");
				}
			}
			return RedirectToAction("Notifications");
		}

		/// <summary>
		/// Смена тарифного плана
		/// </summary>
		/// <param name="plan">Тарифный план</param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult ChangePlan([EntityBinder] Plan plan)
		{
			plan = DbSession.Query<Plan>().First(s => s.Id == plan.Id);
			var client = CurrentClient;
			InitPlans(client);
			ViewBag.Client = client;
			var isOnceOnlyUsed =
				DbSession.Query<PlanHistoryEntry>()
					.Any(s => s.Client == client && (s.PlanAfter.Id == plan.Id || s.PlanBefore.Id == plan.Id) && plan.IsOnceOnly);

			if (isOnceOnlyUsed) {
				ErrorMessage("На данный тариф нельзя перейти вновь.");
				return RedirectToAction("Plans");
			}
			//todo - наверно надо подумать как эти провеки засунуть куда следует
			var beginDate = client.WorkingStartDate ?? new DateTime();
			var stoppage = client.PhysicalClient.Plan.StoppageMonths.HasValue ? client.PhysicalClient.Plan.StoppageMonths : 2;
			if (beginDate == DateTime.MinValue) {
				ErrorMessage($"Нельзя менять тариф до получения доступа в сеть.");
				return RedirectToAction("Plans");
			}
			if (beginDate.AddMonths(stoppage.Value) >= SystemTime.Now()) {
				ErrorMessage($"Нельзя менять тариф, в первые {stoppage} месяца после подключения");
				return RedirectToAction("Plans");
			}
			var oldWarningState = client.ShowBalanceWarningPage;
			var oldPlan = client.PhysicalClient.Plan;
			var result = client.PhysicalClient.RequestChangePlan(plan);
			if (result == null) {
				ErrorMessage("Не достаточно средств для смены тарифного плана");
				return RedirectToAction("Plans");
			}
			if (oldWarningState != client.ShowBalanceWarningPage) {
				client.ShowBalanceWarningPage = oldWarningState;
			}
			DbSession.Save(client);
			DbSession.Save(result);
			var warning = (client.GetWorkDays() <= 3) ? " Обратите внимание, что у вас низкий баланс!" : "";
			warning += $"<br/>С Вашего лицевого счета списана стоимость смены тарифа {result.Sum.ToString("F2")} руб.";
			SuccessMessage("Тариф успешно изменен." + warning);
			 
			// добавление записи в историю тарифов пользователя
			var planHistory = new PlanHistoryEntry {
				Client = CurrentClient,
				DateOfChange = SystemTime.Now(),
				PlanAfter = plan,
				PlanBefore = oldPlan,
				Price = oldPlan.GetTransferPrice(plan)
			};
			DbSession.Save(planHistory);

			var msg = string.Format("Изменение тарифа был изменен с '{0}'({1}) на '{2}'({3}). Стоимость перехода: {4} руб.",
				oldPlan.Name, oldPlan.Price, plan.Name, plan.Price, result.Sum.ToString("F2"));
			var appeal = new Appeal(msg, client, AppealType.User) {
				Employee = GetCurrentEmployee()
			};
			DbSession.Save(appeal);
			return RedirectToAction("Plans");
		}

		/// <summary>
		/// Функция дублирована в ServiceController
		/// Нужно удалить ее отовсюду, потому что она говно полное
		/// </summary>
		protected void InitServices()
		{
			var client = CurrentClient;
			var services = DbSession.Query<Service>().Where(s => s.IsActivableFromWeb);
			var blockAccountService = services.FirstOrDefault(i => i is BlockAccountService);
			blockAccountService = BaseModel.UnproxyOrDefault(blockAccountService) as BlockAccountService;

			var deferredPayment = services.FirstOrDefault(i => i is DeferredPayment);
			deferredPayment = BaseModel.UnproxyOrDefault(deferredPayment) as DeferredPayment;
			var inforoomServices = new List<Service> { blockAccountService, deferredPayment };

			ViewBag.Client = client;
			//@todo Убрать исключения для статического IP, когда будет внедрено ручное включение сервиса
			ViewBag.ClientServices =
				client.ClientServices.Where(
					cs => (cs.Service.Name == "Фиксированный ip-адрес" || cs.Service.IsActivableFromWeb) && cs.IsActivated).ToList();
			ViewBag.AvailableServices = inforoomServices;

			ViewBag.BlockAccountService = blockAccountService;
			ViewBag.DeferredPayment = deferredPayment;
		}

		//@todo убрать этот бред - заменить функцией с return
		private void InitPlans(Client client)
		{
			IList<Plan> plans;
			//если адреса нет, показываем все тарифы
			if (client.PhysicalClient.Address != null) {
				//Если у тарифа нет региона, то он доступен во всех регионах
				plans =
					GetList<Plan>()
						.Where(
							p =>
								p.AvailableForOldClients && !p.Disabled && !p.IsServicePlan &&
								(!p.RegionPlans.Any() || p.RegionPlans.Any(r => r.Region == client.PhysicalClient.Address.Region)))
						.ToList();
			}
			else {
				plans =
					GetList<Plan>()
						.Where(p => p.AvailableForOldClients && !p.Disabled && !p.IsServicePlan && !p.RegionPlans.Any())
						.ToList();
			}

			ViewBag.Plans = plans.ToList();
		}
	}
}