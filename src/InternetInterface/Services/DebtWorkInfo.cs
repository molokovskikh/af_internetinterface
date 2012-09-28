using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using InternetInterface.Models;

namespace InternetInterface.Services
{
	[ActiveRecord("DebtWorkInfos", Schema = "internet")]
	public class DebtWorkInfo
	{
		public DebtWorkInfo()
		{
		}

		public DebtWorkInfo(ClientService service, decimal sum)
		{
			Service = service;
			Sum = sum;
		}

		[PrimaryKey]
		public uint Id { get; set; }

		[BelongsTo]
		public ClientService Service { get; set; }

		[Property]
		public decimal Sum { get; set; }
	}
}