using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Web;
using Inforoom2.Helpers;
using Inforoom2.Models;
using NHibernate;

namespace InforoomControlPanel.Models
{
	public class ViewModelClientOrder
	{
		public int Number { get; set; }
		public int EndPoint { get; set; }
		public string BeginDate { get; set; }
		public string EndDate { get; set; }
		public List<OrderService> OrderServices { get; set; }
		public List<int> ClientEndpoints { get; set; }
	}

	public class ViewModelClientEndpoint
	{
		public ViewModelClientEndpoint()
		{
		}

		public ViewModelClientEndpoint(EndpointStateBox reservedState)
		{ 
			this.Switch = reservedState.ConnectionHelper.Switch;
			this.Ip = reservedState.ConnectionHelper.StaticIp;
			this.Pool = reservedState.ConnectionHelper.Pool ?? 0;
			this.Port = string.IsNullOrEmpty(reservedState.ConnectionHelper.Port) ? 0 : Convert.ToInt32(reservedState.ConnectionHelper.Port);
			this.PackageId = reservedState.ConnectionHelper.PackageId;
			this.Monitoring = reservedState.ConnectionHelper.Monitoring;
			this.LeaseList = new List<Tuple<string, bool>>();
			this.StaticIpList = reservedState.StaticIpList.Select(s => new { @Id = s.Id, @Ip = s.Ip, @Mask = s.Mask, @Subnet = s.GetSubnet() }).ToList();
		}

		public int Id { get; set; }
		public string Ip { get; set; }
		public List<Tuple<string, bool>> LeaseList { get; set; }
		public int Pool { get; set; }
		public int Switch { get; set; }
		public int Port { get; set; }
		public int PackageId { get; set; }
		public bool Monitoring { get; set; }
		public object StaticIpList { get; set; }
		public string ConnectionAddress { get; set; }
	}
}