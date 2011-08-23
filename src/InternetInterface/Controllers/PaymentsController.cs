﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.ActiveRecord.Linq;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Tools;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;

namespace AdminInterface.Controllers
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


    //[Layout("Main")]
    [Helper(typeof(ViewHelper))]
    [FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
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
			if (IsPost)
			{
                var payment = new BankPayment();
				BindObjectInstance(payment, "payment", AutoLoadBehavior.OnlyNested);
				payment.RegisterPayment();
				payment.Save();
				RedirectToReferrer();
			}
			else
			{
				PropertyBag["recipients"] = Recipient.Queryable.OrderBy(r => r.Name).ToList();
                PropertyBag["payments"] = BankPayment.Queryable
					.Where(p => p.RegistredOn >= DateTime.Today)
					.OrderBy(p => p.RegistredOn).ToList();
			}
		}

		public void SavePayments()
		{
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

				payment.RegisterPayment();
				payment.Save();
			    new Payment {
			                    Client = Client.Queryable.FirstOrDefault(c=>c.LawyerPerson == payment.Payer),
                                Sum = payment.Sum,
                                RecievedOn = payment.RegistredOn,
                                PaidOn = payment.PayedOn
			                }.Save();
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
            var payment = BankPayment.TryFind(id);
			if (IsPost)
			{
				BindObjectInstance(payment, "payment", AutoLoadBehavior.NullIfInvalidKey);
				payment.DoUpdate();
				Flash["Message"] = Message.Notify("Сохранено");
				RedirectToReferrer();
			}
			else
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
				.AsQueryable<LawyerPerson>()
				.Where(p => p.Name.Contains(term) || p.Id == id)
				.Take(20)
				.ToList()
				.Select(p => new {
					id = p.Id,
					label = String.Format("[{0}]. {1} ИНН {2}", p.Id, p.Name, p.INN)
				});
		}
	}
}