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
			var physClientId = Convert.ToUInt32(Session["LoginClient"]);
			var physClient = PhisicalClients.Find(physClientId);
			PropertyBag["PhysClientName"] = string.Format("{0} {1}",physClient.Name, physClient.Patronymic);
			PropertyBag["PhysicalClient"] = physClient;
			var client = Clients.FindAllByProperty("PhisicalClient", physClient).First();
			/*var writeOffs = WriteOff.FindAll(DetachedCriteria.For(typeof (WriteOff))
			                                 	.Add(Restrictions.Eq("Client", client))).GroupBy(y => new { y.WriteOffDate.Year, y.WriteOffDate.Month });
			PropertyBag["WriteOffs"] = writeOffs.Select(t => t.Sum(y => y.WriteOffSum));*/
			IList<WriteOff> writeOffs = new List<WriteOff>();
			var gpoupKey = "concat(YEAR(WriteOffDate),'-',MONTH(WriteOffDate),'-',DAYOFMONTH(WriteOffDate))";
			if (grouped == "day")
				gpoupKey = "concat(YEAR(WriteOffDate),'-',MONTH(WriteOffDate),'-',DAYOFMONTH(WriteOffDate))";
			if (grouped == "month")
				gpoupKey = "concat(YEAR(WriteOffDate),'-',MONTH(WriteOffDate))";
			if (grouped == "year")
				gpoupKey = "YEAR(WriteOffDate)";
			ARSesssionHelper<WriteOff>.QueryWithSession(session =>
			                                            	{
			                                            		var query =
			                                            			session.CreateSQLQuery(string.Format(
@"SELECT id, Sum(WriteOffSum) as WriteOffSum, WriteOffDate, Client  FROM internet.WriteOff W
where Client = :clientid
group by {0}", gpoupKey)).AddEntity(typeof(WriteOff));
			                                            		query.SetParameter("clientid", client.Id);
			                                            		writeOffs = query.List<WriteOff>();
																return query.List<WriteOff>();
			                                            	});
			PropertyBag["WriteOffs"] = writeOffs.OrderBy(e => e.WriteOffDate).ToArray();
			PropertyBag["grouped"] = grouped;
			PropertyBag["Payments"] = Payment.FindAllByProperty("Client", physClient).OrderBy(e => e.PaidOn).ToArray();
		}
	}
}