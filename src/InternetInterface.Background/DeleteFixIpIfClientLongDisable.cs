using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using Common.Web.Ui.Helpers;
using InternetInterface.Models;
using NHibernate.Linq;

namespace InternetInterface.Background
{
	public class DeleteFixIpIfClientLongDisable : Task
	{
		protected override void Process()
		{
			var clientWhoDeleteStatic = Session.Query<Client>().Where(c => c.Disabled && c.BlockDate != null && c.BlockDate <= SystemTime.Now().AddDays(-61)).ToList();
			foreach (var client in clientWhoDeleteStatic) {
				foreach (var clientEndpoint in client.Endpoints) {
					clientEndpoint.Ip = null;
					Session.Save(clientEndpoint);
				}
				client.BlockDate = null;
				Session.Save(client);
			}
		}
	}
}
