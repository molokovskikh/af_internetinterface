using System;
using System.Linq;
using Common.Tools.Calendar;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Background
{
	public class BuildInvoiceTask
	{
		public ISession session;

		public void Process()
		{
			var begin = DateTime.Today.FirstDayOfMonth();
			var end = DateTime.Today.AddMonths(1).FirstDayOfMonth();

			var clients = session.Query<Client>()
				.Where(c => c.LawyerPerson != null)
				.Where(c => session.Query<WriteOff>().Any(w => w.Client == c && w.WriteOffDate >= begin && w.WriteOffDate < end))
				.ToList();

			foreach (var client in clients) {
				var writeOffs = session.Query<WriteOff>().Where(w => w.Client == client).ToList();

				var invoice = new Invoice(client, writeOffs);
				session.Save(invoice);
			}
		}
	}
}