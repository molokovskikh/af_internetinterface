using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace InternetInterface.Models
{
	public class AgentActions
	{
		public static string WorkedClient
		{
			get { return "WorkedClient"; }
		}

		public static string AgentPayIndex
		{
			get { return "AgentPayIndex"; }
		} 
	}

	[ActiveRecord("AgentTariffs", Schema = "Internet", Lazy = true)]
	public class AgentTariff : ActiveRecordLinqBase<AgentTariff>
	{
		[PrimaryKey]
		public virtual int Id { get; set; }

		[Property]
		public virtual string ActionName { get; set; }

		[Property]
		public virtual decimal Sum { get; set; }
		
		[Property]
		public virtual string Description { get; set; }

		public static AgentTariff GetAction(string action)
		{
			return Queryable.FirstOrDefault(a => a.ActionName == action);
		}

		public static decimal GetPriceForAction(string action)
		{
			var act = GetAction(action);
			if (act != null)
				return GetAction(action).Sum;
			return 0m;
		}
	}
}