using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;

namespace InternetInterface.Models
{
	[ActiveRecord("PaymentsForAgent", Schema = "internet", Lazy = true)]
	public class PaymentsForAgent : ActiveRecordLinqBase<PaymentsForAgent>
	{
		[PrimaryKey]
		public virtual int Id { get; set; }

		[BelongsTo]
		public virtual Partner Agent { get; set; }

		[Property]
		public virtual decimal Sum { get; set; }

		[Property]
		public virtual string Comment { get; set; }

		[Property]
		public virtual DateTime RegistrationDate { get; set; }

		[BelongsTo]
		public virtual AgentTariff Action { get; set; }

		public static void CreatePayment(string action, string coment, Partner agent)
		{
			new PaymentsForAgent {
									Agent = agent,
									Comment = coment,
									RegistrationDate = DateTime.Now,
									Sum = AgentTariff.GetPriceForAction(action),
									Action = AgentTariff.GetAction(action)
								 }.Save();
		}

		/*public static List<PaymentsForAgent> GetPayments(Partner agent, Week interval)
		{
			var simplePayments =
				Queryable.Where(p => p.RegistrationDate >= interval.StartDate && p.RegistrationDate <= interval.EndDate).ToList();
			var requestWhoWorks =
				Client.Queryable.Where(
					c =>
					c.BeginWork != null && c.BeginWork.Value >= interval.StartDate && c.BeginWork.Value <= interval.EndDate &&
					c.PhysicalClient != null && c.PhysicalClient.Request != null).
					ToList().Select(c => c.PhysicalClient.Request);
			var bonusForRequest = 0m;
			foreach (var requestWhoWork in requestWhoWorks)
			{
				var Interval = DateHelper.GetWeekInterval(requestWhoWork.RegDate);
				var requestsInInterval =
					Requests.Queryable.Where(r => r.RegDate >= Interval.StartDate && r.RegDate <= Interval.EndDate).
						ToList();
				if (requestsInInterval.Count >= 20)
					bonusForRequest += 100m;
				else
				{
					if (requestsInInterval.Count >= 10)
						bonusForRequest += 50m;
				}
				var weekBonus = true;
				for (int i = 0; i < 5; i++)
				{
					if (requestsInInterval.Count(r => r.RegDate.Date == Interval.StartDate.AddDays(i).Date) <= 0)
						weekBonus = false;
				}
				if (weekBonus)
					bonusForRequest += 50m;
			}
			return new List<PaymentsForAgent>();
		}*/
	}
}