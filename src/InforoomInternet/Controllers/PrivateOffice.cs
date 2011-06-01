using System;
using System.Collections.Generic;
using System.Linq;
using Castle.MonoRail.Framework;
using InternetInterface.Helpers;
using InternetInterface.Models;

namespace InforoomInternet.Controllers
{
	[Layout("Main")]
	[Filter(ExecuteWhen.BeforeAction, typeof(NHibernateFilter))]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AccessFilter))]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(BeforeFilter))]
	public class PrivateOffice:SmartDispatcherController
	{
		public void IndexOffice(string grouped)
		{
			var clientId = Convert.ToUInt32(Session["LoginClient"]);
			var Client = Clients.Find(clientId);
			PropertyBag["PhysClientName"] = string.Format("{0} {1}", Client.PhysicalClient.Name, Client.PhysicalClient.Patronymic);
			PropertyBag["PhysicalClient"] = Client.PhysicalClient;
			PropertyBag["Client"] = Client;
			//var client = Clients.FindAllByProperty("PhysicalClient", physClient).First();
			/*var writeOffs = WriteOff.FindAll(DetachedCriteria.For(typeof (WriteOff))
			                                 	.Add(Restrictions.Eq("Client", client))).GroupBy(y => new { y.WriteOffDate.Year, y.WriteOffDate.Month });
			PropertyBag["WriteOffs"] = writeOffs.Select(t => t.Sum(y => y.WriteOffSum));*/
            PropertyBag["WriteOffs"] = Client.GetWriteOffs(grouped).OrderBy(e => e.WriteOffDate).ToArray();
			PropertyBag["grouped"] = grouped;
			PropertyBag["Payments"] = Payment.FindAllByProperty("Client", Client).OrderBy(e => e.PaidOn).ToArray();
		}
	}
}