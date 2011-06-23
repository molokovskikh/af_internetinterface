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
            PropertyBag["WriteOffs"] = Client.GetWriteOffs(grouped).OrderBy(e => e.WriteOffDate).ToArray();
			PropertyBag["grouped"] = grouped;
			PropertyBag["Payments"] = Payment.FindAllByProperty("Client", Client).OrderBy(e => e.PaidOn).ToArray();
		}

        public void PostponedPayment(uint clientId)
        {
            var client = Clients.Find(clientId);
            var pclient = client.PhysicalClient;
            var message = string.Empty;
            if (client.PostponedPayment != null)
                message += "Повторное использование услуги \"Обещаный платеж невозможно\"";
            if (!client.Disabled)
                message += "Воспользоваться устугой возможно только при отрицательном балансе";
            if (!client.Disabled || !client.AutoUnblocked)
                message += "Услуга \"Обещанный платеж\" недоступна";
            if (client.PostponedPayment == null && client.Disabled && pclient.Balance < 0 && client.AutoUnblocked)
            {
                client.PostponedPayment = DateTime.Now;
                client.Disabled = false;
                var writeOff = pclient.Tariff.GetPrice(client)/client.GetInterval();
                pclient.Balance -= writeOff;
                new WriteOff {
                                 Client = client,
                                 WriteOffDate = DateTime.Now,
                                 WriteOffSum = writeOff
                             }.Save();
                pclient.Update();
                client.Update();
                message += "Услуга \"Обещанный платеж активирована\"";
            }
            PropertyBag["message"] = message;
        }
	}
}