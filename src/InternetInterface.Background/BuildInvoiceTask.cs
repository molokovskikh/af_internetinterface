using System;
using System.Linq;
using Common.Tools;
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
			NotSend = false;
			Plan(PlanPeriod.Month, 1.Day());
			Action = () => WithSession(Process);
		}

		public DateTime MagicDate = new DateTime(2013, 4, 1);
		public bool NotSend { get; set; }

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

			if (DateTime.Today <= MagicDate) {
				foreach (var client in clients) {
					var writeOffs = session.Query<WriteOff>().Where(w => w.Client == client && w.WriteOffDate >= begin && w.WriteOffDate < end).ToList();

					var invoice = new Invoice(client, period, writeOffs);
					session.Save(invoice);

					_mailer.Invoice(invoice);
					_mailer.Send();
					_mailer.Clear();
				}
			}
			if (DateTime.Today >= MagicDate) {
				clients = session.Query<Client>()
					.Where(c => c.LawyerPerson != null)
					.ToList();
				foreach (var client in clients) {
					var orders = session.Query<Orders>().Where(o => o.Client == client).ToList();
					orders = orders.Where(o => o.OrderStatus == OrderStatus.Enabled && o.OrderServices != null).ToList();
					var invoice = new Invoice(client, period, orders);
					session.Save(invoice);
					if (!NotSend) {
						_mailer.Invoice(invoice, ContactType.ActEmail);
						_mailer.Send();
						_mailer.Clear();
					}
					//создаем акты по счетам в предыдущем месяце
					if (DateTime.Today > MagicDate) {
						var invoices = session.Query<Invoice>()
							.Where(i => i.Client.LawyerPerson != null)
							.Where(i => i.Date >= begin && i.Date <= end)
							.ToList();
						var acts = Act.Build(invoices, DateTime.Today);
						foreach (var act in acts) {
							session.Save(act);
							if (!NotSend) {
								_mailer.Act(act);
								_mailer.Send();
								_mailer.Clear();
							}
						}
					}
				}
			}
		}
	}
}