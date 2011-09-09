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

		public static void CreatePayment(Partner agent, string comment, decimal sum)
		{
			new PaymentsForAgent {
									Agent = agent,
									RegistrationDate = DateTime.Now,
									Sum = sum,
									Comment = comment
								 }.Save();
		}
	}
}