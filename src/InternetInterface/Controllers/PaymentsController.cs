﻿using System;
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
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
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
	public class PaymentsController : ARSmartDispatcherController
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
			Binder.Validator = Validator;
			if (IsPost)
			{
				var payment = new BankPayment();
				BindObjectInstance(payment, "payment", AutoLoadBehavior.OnlyNested);
				if (!HasValidationError(payment))
				{
					payment.RegisterPayment();
					payment.Save();
					new Payment {
									Client =
										Client.Queryable.FirstOrDefault(c => c.LawyerPerson != null && c.LawyerPerson == payment.Payer),
									Sum = payment.Sum,
									RecievedOn = payment.RegistredOn,
									PaidOn = payment.PayedOn,
									Agent = Agent.GetByInitPartner()
								}.Save();
					RedirectToReferrer();
					return;
				}
				else
				{
					ArHelper.WithSession(s => ArHelper.Evict(s, new[] { payment }));
					PropertyBag["Payment"] = payment;
				}
			}
			//else
			{
				if (PropertyBag["Payment"] == null)
					PropertyBag["Payment"] = new BankPayment();
				PropertyBag["recipients"] = Recipient.Queryable.OrderBy(r => r.Name).ToList();
				PropertyBag["payments"] = BankPayment.Queryable
					.Where(p => p.RegistredOn >= DateTime.Today)
					.OrderBy(p => p.RegistredOn).ToList();
			}
		}

		public void NotifyInforum()
		{
			foreach (var bankPayment in TempPayments())
			{
				if (bankPayment.Payer == null)
				{
					var mailToAdress = "internet@ivrn.net";
					var messageText = new StringBuilder();
					var type = NHibernateUtil.GetClass(bankPayment);
					foreach (var propertyInfo in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
					{
						if (propertyInfo.GetCustomAttributes(typeof(PropertyAttribute), true).Length > 0)
						{
							var value = propertyInfo.GetValue(bankPayment, null);
							var name = BindingHelper.GetDescription(propertyInfo);
							if (!string.IsNullOrEmpty(name))
								messageText.AppendLine(string.Format("{0} = {1}", name, value));
						}
						if (propertyInfo.GetCustomAttributes(typeof(NestedAttribute), true).Length > 0)
						{
							var class_dicrioprion = BindingHelper.GetDescription(propertyInfo);
							messageText.AppendLine();
							messageText.AppendLine(class_dicrioprion);
							var value_class = propertyInfo.GetValue(bankPayment, null);
							var type_nested = NHibernateUtil.GetClass(value_class);
							foreach (var nested_propertyInfo in type_nested.GetProperties(BindingFlags.Instance | BindingFlags.Public))
							{
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
			//Binder.Validator = Validator;
			var payments = TempPayments();
			if (payments == null)
			{
				Flash["Message"] = Message.Error("Время сесии истекло. Загрузите выписку повторно.");
				RedirectToReferrer();
			}

			foreach (var payment in payments)
			{
				//если зайти в два платежа и отредактировать их
				//то получим двух плательщиков из разных сесей
				//правим это
				if (payment.Payer != null)
					payment.Payer = ActiveRecordLinqBase<LawyerPerson>.Queryable.Where(p => p.Id == payment.Payer.Id).FirstOrDefault(); //IPayer.Find(payment.Payer.Id);

				if (Validator.IsValid(payment))
				{
					payment.RegisterPayment();
					payment.Save();
					if (payment.Payer != null)
					new Payment {
									Client =
										Client.Queryable.FirstOrDefault(c => c.LawyerPerson != null && c.LawyerPerson == payment.Payer),
									Sum = payment.Sum,
									RecievedOn = payment.RegistredOn,
									PaidOn = payment.PayedOn,
									Agent = Agent.GetByInitPartner()
								}.Save();
				}
				else
				{
					ArHelper.WithSession(s => ArHelper.Evict(s, new[] { payment }));
				}
			}

			RedirectToAction("Index",
				new Dictionary<string, string>{
					{"filter.Period.Begin", payments.Min(p => p.PayedOn).ToShortDateString() },
					{"filter.Period.End", payments.Max(p => p.PayedOn).ToShortDateString() }
				});
		}

		public void CancelPayments()
		{
			Session["payments"] = null;
			RedirectToReferrer();
		}

		public void ProcessPayments()
		{
			if (IsPost)
			{
				var file = Request.Files["inputfile"] as HttpPostedFile;
				if (file == null || file.ContentLength == 0)
				{
					PropertyBag["Message"] = Message.Error("Нужно выбрать файл для загрузки");
					return;
				}
				Session["payments"] = BankPayment.Parse(file.FileName, file.InputStream);
				RedirectToReferrer();
			}
			else
			{
				PropertyBag["payments"] = Session["payments"];
			}
		}

		public void EditTemp(uint id)
		{
			var payment = FindTempPayment(id);
			if (IsPost)
			{
				BindObjectInstance(payment, "payment", AutoLoadBehavior.NullIfInvalidKey);
				payment.UpdateInn();
				Flash["Message"] = Message.Notify("Сохранено");
				RedirectToAction("ProcessPayments");
				//RedirectToReferrer();
			}
			else
			{
				PropertyBag["payment"] = payment;
				PropertyBag["recipients"] = Recipient.Queryable.OrderBy(r => r.Name).ToList();
				RenderView("Edit");
			}
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
			var client = Client.Queryable.FirstOrDefault(c => c.LawyerPerson == payment.Payer);
			new UserWriteOff {
			                 	Client = client,
								Sum = payment.Sum,
								Date = DateTime.Now,
								Comment = "Списание в связи с удалением баковского платежа"
			                 }.Save();
			payment.Delete();
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
			Binder.Validator = Validator;
			var payment = BankPayment.TryFind(id);
			if (IsPost)
			{
				var oldBalance = payment.Sum;
				var oldPayer = payment.Payer;
				var oldPayment = payment;
				BindObjectInstance(payment, "payment", AutoLoadBehavior.NullIfInvalidKey);
				var newBalance = payment.Sum;
				var newPayerFlag = oldPayer == null && payment.Payer != null;
				if (!HasValidationError(payment))
				{
					if (oldPayer != null && payment.Payer == oldPayer)
					{
						var client = Client.Queryable.Where(c => c.LawyerPerson.Id == payment.Payer.Id).FirstOrDefault();
						if (newBalance - oldBalance < 0 && payment.Payer != null)
						{
							new UserWriteOff {
							                 	Client = client,
							                 	Sum = oldBalance - newBalance,
							                 	Date = DateTime.Now,
							                 	Comment =
							                 		string.Format("Списание после редактирования банковского платежа (id = {0})", payment.Id)
							                 }.Save();
						}
						if (newBalance - oldBalance > 0 && payment.Payer != null)
						{
							new Payment {
							            	Client = client,
							            	PaidOn = DateTime.Now,
							            	RecievedOn = DateTime.Now,
							            	Sum = newBalance - oldBalance,
							            	LogComment =
							            		string.Format("Зачисление после редактирования банковского платежа (id = {0})", payment.Id)
							            }.Save();
						}
					}
					if (newPayerFlag)
					{
						new Payment {
						            	Client = Client.Queryable.Where(c => c.LawyerPerson.Id == payment.Payer.Id).FirstOrDefault(),
						            	PaidOn = DateTime.Now,
						            	RecievedOn = DateTime.Now,
						            	Sum = payment.Sum,
						            	LogComment =
						            		string.Format(
						            			"Зачисление после редактирования банковского платежа (id = {0}), назначен плательщик", payment.Id)
						            }.Save();
					}
					if (oldPayer != null && oldPayer != payment.Payer)
					{
						new Payment {
						            	Client = Client.Queryable.Where(c => c.LawyerPerson.Id == payment.Payer.Id).FirstOrDefault(),
						            	PaidOn = DateTime.Now,
						            	RecievedOn = DateTime.Now,
						            	Sum = payment.Sum,
						            	LogComment =
						            		string.Format("Зачисление после редактирования банковского платежа (id = {0})", payment.Id)
						            }.Save();
						new UserWriteOff {
						                 	Client = Client.Queryable.Where(c => c.LawyerPerson == oldPayer).FirstOrDefault(),
						                 	Comment =
						                 		string.Format("Списание после смены плательщика, при редактировании банковского платежа №{0}",
						                 		              payment.Id),
						                 	Date = DateTime.Now,
						                 	Sum = oldPayment.Sum
						                 }.Save();
					}
					payment.DoUpdate();
					Flash["Message"] = Message.Notify("Сохранено");
					RedirectToReferrer();
					return;
				}
				else
				{
					ArHelper.WithSession(s => ArHelper.Evict(s, new[] {payment}));
					//PropertyBag["Payment"] = payment;
				}
			}
			//else
			{
				PropertyBag["payment"] = payment;
				PropertyBag["recipients"] = Recipient.Queryable.OrderBy(r => r.Name).ToList();
			}
		}

		[return: JSONReturnBinder]
		public object SearchPayer(string term)
		{
			uint id;
			uint.TryParse(term, out id);
			return ActiveRecordLinq
				.AsQueryable<Client>()
				.Where(p => p.LawyerPerson.Name.Contains(term) || p.Id == id)
				.Take(20)
				.ToList()
				.Select(p => new {
					id = p.LawyerPerson.Id,
					label = String.Format("[{0}]. {1} ИНН {2}", p.Id, p.LawyerPerson.Name, p.LawyerPerson.INN)
				});
		}
	}
}