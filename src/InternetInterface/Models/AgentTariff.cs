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
		public static string CreateRequest
		{
			get { return "CreateRequest"; }
		} 
		public static string DeleteRequest
		{
			get { return "DeleteRequest"; }
		}
		public static string CreateClient
		{
			get { return "CreateClient"; }
		}
		public static string RefusedClient
		{
			get { return "RefusedClient"; }
		}
		public static string ConnectClient
		{
			get { return "ConnectClient"; }
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

		public static AgentTariff GetAction(string action)
		{
			return Queryable.Where(a => a.ActionName == action).FirstOrDefault();
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