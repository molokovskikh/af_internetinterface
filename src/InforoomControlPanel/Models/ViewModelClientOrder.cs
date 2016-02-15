using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Web;
using Inforoom2.Models;

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