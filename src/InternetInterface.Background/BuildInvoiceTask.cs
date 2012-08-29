using System;
using System.Linq;
using Common.Tools.Calendar;
using Common.Web.Ui.Models.Jobs;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Background
{
	public class BuildInvoiceTask : MonthlyJob
	{
		private Mailer _mailer;

		public BuildInvoiceTask(Mailer mailer)
		{
			_mailer = mailer;
			Plan(PlanPeriod.Month, 1.Day());
			Action = () => WithSession(Process);
		}

		public void Process(ISession session)
		{
			var period = DateTime.Today.AddMonths(-1).ToPeriod();
			var begin = period.GetPeriodBegin();
			//окончание надо увеличить на один день что бы выбрать записи за этот день
			var end = period.GetPeriodEnd().AddDays(1);

			var clients = session.Query<Client>()
				.Where(c => c.LawyerPerson != null)
				.Where(c => session.Query<WriteOff>().Any(w => w.Client == c && w.WriteOffDate >= begin && w.WriteOffDate < end))
				.ToList();

			foreach (var client in clients) {
				var writeOffs = session.Query<WriteOff>().Where(w => w.Client == client && w.WriteOffDate >= begin && w.WriteOffDate < end).ToList();

				var invoice = new Invoice(client, period, writeOffs);
				session.Save(invoice);

				_mailer.Invoice(invoice);
				_mailer.Send();
				_mailer.Clear();
			}
		}
	}
}