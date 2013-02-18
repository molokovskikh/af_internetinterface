using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.ComponentModel;
using System.Text;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.ActiveRecord.Linq;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Helpers;
using Common.Tools;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Helpers;
using InternetInterface.AllLogic;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using InternetInterface.Queries;
using NHibernate;

namespace InternetInterface.Controllers
{
	public class PaymentStatistics
	{
		public PaymentStatistics(List<BankPayment> payments)
		{
			Count = payments.Count;
			Sum = payments.Sum(p => p.Sum);
		}

		public int Count { get; set; }
		public decimal Sum { get; set; }
	}

	[Helper(typeof(ViewHelper))]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter)), System.Runtime.InteropServices.GuidAttribute("5382FACE-DB49-4A02-9E2E-0A512B0D2E49")]
	public class PaymentsController : BaseController
	{
		public void Index([DataBind("filter")] PaymentFilter filter)
		{
			if (filter.Recipient != null && filter.Recipient.Id == 0)
				filter.Recipient = null;

			var payments = filter.Find();
			PropertyBag["filter"] = filter;
			PropertyBag["payments"] = payments;
			PropertyBag["stat"] = new PaymentStatistics(payments);
		}

		public void New()
		{
			if (IsPost) {
				var payment = new BankPayment();
				SetARDataBinder();
				BindObjectInstance(payment, "payment", AutoLoadBehavior.OnlyNested);
				if (!HasValidationError(payment)) {
					payment.RegisterPayment();
					payment.Save();
					new Payment {
						Client = payment.Payer,
						Sum = payment.Sum,
						RecievedOn = payment.RegistredOn,
						PaidOn = payment.PayedOn,
						Agent = Agent.GetByInitPartner(),
						BankPayment = payment
					}.Save();
					RedirectToReferrer();
					return;
				}
				else {
					ArHelper.WithSession(s => ArHelper.Evict(s, new[] { payment }));
					PropertyBag["Payment"] = payment;
				}
			}
			if (PropertyBag["Payment"] == null)
				PropertyBag["Payment"] = new BankPayment();
			PropertyBag["recipients"] = Recipient.Queryable.OrderBy(r => r.Name).ToList();
			PropertyBag["payments"] = BankPayment.Queryable
				.Where(p => p.RegistredOn >= DateTime.Today)
				.OrderBy(p => p.RegistredOn).ToList();
		}

		public void NotifyInforum()
		{
			foreach (var bankPayment in TempPayments()) {
				if (bankPayment.Payer == null) {
					var mailToAdress = "internet@ivrn.net";
					var messageText = new StringBuilder();
					var type = NHibernateUtil.GetClass(bankPayment);
					foreach (var propertyInfo in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
						if (propertyInfo.GetCustomAttributes(typeof(PropertyAttribute), true).Length > 0) {
							var value = propertyInfo.GetValue(bankPayment, null);
							var name = BindingHelper.GetDescription(propertyInfo);
							if (!string.IsNullOrEmpty(name))
								messageText.AppendLine(string.Format("{0} = {1}", name, value));
						}
						if (propertyInfo.GetCustomAttributes(typeof(NestedAttribute), true).Length > 0) {
							var class_dicrioprion = BindingHelper.GetDescription(propertyInfo);
							messageText.AppendLine();
							messageText.AppendLine(class_dicrioprion);
							var value_class = propertyInfo.GetValue(bankPayment, null);
							var type_nested = NHibernateUtil.GetClass(value_class);
							foreach (var nested_propertyInfo in type_nested.GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
								var value = nested_propertyInfo.GetValue(value_class, null);
								var name = BindingHelper.GetDescription(nested_propertyInfo);
								if (!string.IsNullOrEmpty(name))
									messageText.AppendLine(string.Format("{0} = {1}", name, value));
							}
						}
					}
					var message = new MailMessage();
					message.To.Add(mailToAdress);
					message.Subject = "Получен нераспознаный платеж";
					message.From = new MailAddress("service@analit.net");
					message.Body = messageText.ToString();
					var smtp = new SmtpClient("box.analit.net");
					smtp.Send(message);
				}
			}
			Flash["notify_message"] = "Письма отправлены";
			RedirectToAction("ProcessPayments");
		}

		public void SavePayments()
		{
			var payments = TempPayments();
			if (payments == null) {
				Flash["Message"] = Message.Error("Время сесии истекло. Загрузите выписку повторно.");
				RedirectToReferrer();
				return;
			}

			foreach (var payment in payments.ToList()) {
				//если зайти в два платежа и отредактировать их
				//то получим двух плательщиков из разных сесей
				//правим это
				if (payment.Payer != null)
					payment.Payer = ActiveRecordLinqBase<Client>.Queryable.FirstOrDefault(p => p.Id == payment.Payer.Id);

				if (Validator.IsValid(payment)) {
					payment.RegisterPayment();
					payment.Save();
					if (payment.Payer != null)
						new Payment {
							Client = payment.Payer,
							Sum = payment.Sum,
							RecievedOn = payment.RegistredOn,
							PaidOn = payment.PayedOn,
							Agent = Agent.GetByInitPartner(),
							BankPayment = payment
						}.Save();
				}
				else {
					ArHelper.WithSession(s => ArHelper.Evict(s, new[] { payment }));
				}
			}

			Session["payments"] = null;

			RedirectToAction("Index",
				new Dictionary<string, string> {
					{ "filter.Period.Begin", payments.Min(p => p.PayedOn).ToShortDateString() },
					{ "filter.Period.End", payments.Max(p => p.PayedOn).ToShortDateString() }
				});
		}

		public void CancelPayments()
		{
			Session["payments"] = null;
			RedirectToReferrer();
		}

		public void ProcessPayments()
		{
			if (IsPost) {
				var file = Request.Files["inputfile"] as HttpPostedFile;
				if (file == null || file.ContentLength == 0) {
					PropertyBag["Message"] = Message.Error("Нужно выбрать файл для загрузки");
					return;
				}
				var payments = BankPayment.Parse(file.FileName, file.InputStream);
				if (payments.All(p => IsValid(p)))
					Session["payments"] = payments;
				else {
					var errors = payments.Select(p => {
						IsValid(p);
						var summary = Validator.GetErrorSummary(p);
						if (summary != null && summary.HasError) {
							return new { Client = p.PayerClient, Errors = summary.ErrorMessages.ToList() };
						}
						return null;
					}).Where(e => e != null).ToList();
					PropertyBag["errors"] = errors;
					return;
				}
				RedirectToReferrer();
			}
			else {
				var payments = TempPayments();
				if (payments != null)
					payments.Each(p => IsValid(p));
				PropertyBag["payments"] = payments;
			}
		}

		public void EditTemp(uint id)
		{
			var payment = FindTempPayment(id);
			if (IsPost) {
				SetARDataBinder();
				BindObjectInstance(payment, "payment", AutoLoadBehavior.NullIfInvalidKey);
				if (IsValid(payment)) {
					payment.UpdateInn();
					Flash["Message"] = Message.Notify("Сохранено");
					RedirectToAction("ProcessPayments");
					return;
				}
			}
			PropertyBag["payment"] = payment;
			PropertyBag["recipients"] = Recipient.Queryable.OrderBy(r => r.Name).ToList();
			RenderView("Edit");
		}

		private BankPayment FindTempPayment(uint id)
		{
			return TempPayments().First(p => p.GetHashCode() == id);
		}

		private List<BankPayment> TempPayments()
		{
			return (List<BankPayment>)Session["payments"];
		}

		public void Delete(uint id)
		{
			var payment = BankPayment.Find(id);
			if (payment.Payment != null) {
				Cancel(payment.Payment.Id, string.Format("Был удален банковский платеж от {0} на сумму {1}. Комментарий: {2}", payment.PayedOn.ToShortDateString(), payment.Sum, payment.Comment));
			}
			DbSession.Delete(payment);
			RedirectToReferrer();
		}

		public void DeleteTemp(uint id)
		{
			var payment = FindTempPayment(id);
			TempPayments().Remove(payment);
			RedirectToReferrer();
		}

		public void Edit(uint id)
		{
			var payment = BankPayment.TryFind(id);
			if (IsPost) {
				var oldBalance = payment.Sum;
				var oldPayer = payment.Payer;
				var oldPayment = payment;
				SetARDataBinder();
				BindObjectInstance(payment, "payment", AutoLoadBehavior.NullIfInvalidKey);
				var newBalance = payment.Sum;
				var newPayerFlag = oldPayer == null && payment.Payer != null;
				if (!HasValidationError(payment)) {
					if (oldPayer != null && payment.Payer == oldPayer) {
						var client = payment.Payer;
						if (newBalance - oldBalance < 0 && payment.Payer != null) {
							new UserWriteOff {
								Registrator = InitializeContent.Partner,
								Client = client,
								Sum = oldBalance - newBalance,
								Date = DateTime.Now,
								Comment = string.Format("Списание после редактирования банковского платежа (id = {0})", payment.Id)
							}.Save();
						}
						if (newBalance - oldBalance > 0 && payment.Payer != null) {
							new Payment {
								Agent = Agent.GetByInitPartner(),
								Client = client,
								PaidOn = DateTime.Now,
								RecievedOn = DateTime.Now,
								Sum = newBalance - oldBalance,
								LogComment = string.Format("Зачисление после редактирования банковского платежа (id = {0})", payment.Id)
							}.Save();
						}
					}
					if (newPayerFlag) {
						new Payment {
							Agent = Agent.GetByInitPartner(),
							Client = payment.Payer,
							PaidOn = DateTime.Now,
							RecievedOn = DateTime.Now,
							Sum = payment.Sum,
							LogComment = string.Format("Зачисление после редактирования банковского платежа (id = {0}), назначен плательщик", payment.Id)
						}.Save();
					}
					if (oldPayer != null && oldPayer != payment.Payer) {
						new Payment {
							Agent = Agent.GetByInitPartner(),
							Client = payment.Payer,
							PaidOn = DateTime.Now,
							RecievedOn = DateTime.Now,
							Sum = payment.Sum,
							LogComment = string.Format("Зачисление после редактирования банковского платежа (id = {0})", payment.Id),
							BankPayment = payment
						}.Save();
						if (oldPayment.Payment != null) {
							Cancel(oldPayment.Payment.Id, string.Format("Изменение плательщика в банковском платеже {0} с '{1}' на '{2}'", oldPayment.Id, oldPayer.Name, payment.Payer.Name));
						}
						else {
							new UserWriteOff {
								Registrator = InitializeContent.Partner,
								Client = oldPayer,
								Comment = string.Format("Списание после смены плательщика, при редактировании банковского платежа №{0} \r\n Клиент стал: {1}", payment.Id, payment.Payer.Id),
								Date = DateTime.Now,
								Sum = oldPayment.Sum
							}.Save();
							DbSession.Save(new Appeals(string.Format("После смены плательщика в платеже {0} было создано пользовательское списание, так как не был найден привязанный к банковскому платежу физический платеж для отмены", oldPayment.Id), oldPayer, AppealType.System, true));
						}
					}
					payment.DoUpdate();
					Flash["Message"] = Message.Notify("Сохранено");
					RedirectToReferrer();
					return;
				}
				else {
					ArHelper.WithSession(s => ArHelper.Evict(s, new[] { payment }));
				}
			}
			PropertyBag["payment"] = payment;
			PropertyBag["recipients"] = Recipient.Queryable.OrderBy(r => r.Name).ToList();
		}

		[return: JSONReturnBinder]
		public object SearchPayer(string term)
		{
			uint id;
			uint.TryParse(term, out id);
			return ActiveRecordLinq
				.AsQueryable<Client>()
				.Where(p => p.Name.Contains(term) || p.Id == id)
				.Take(20)
				.ToList()
				.Select(p => new {
					id = p.Id,
					label = String.Format("[{0}]. {1}", p.Id, p.Name)
				});
		}

		public void СhangeSaleSettings()
		{
			var setting = SaleSettings.FindFirst();
			PropertyBag["settings"] = setting;

			if (IsPost) {
				SetARDataBinder();
				BindObjectInstance(setting, ParamStore.Form, "settings", AutoLoadBehavior.Always);
				setting.Save();
				Notify("Настройки сохранены");
				RedirectToReferrer();
			}
		}

		public void Cancel(uint id, string comment)
		{
			if (!string.IsNullOrEmpty(comment)) {
				var payment = DbSession.Load<Payment>(id);
				var message = payment.Cancel(comment);
				DbSession.Delete(payment);
				DbSession.Save(message);
				Notify("Отменено");
				EmailHelper.Send("internet@ivrn.net", "Уведомление об отмене платежа", string.Format(@"
Отменено платеж №{0}
Клиент: №{1} - {2}
Сумма: {3}
Оператор: {4}
Комментарий: {5}
", payment.Id, payment.Client.Id, payment.Client.Name, payment.Sum.ToString("#.00"), InitializeContent.Partner.Name, comment));
			}
			else {
				Error("Введите комментарий для отмены платежа");
			}
			RedirectToReferrer();
		}
	}
}