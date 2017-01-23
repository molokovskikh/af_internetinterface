using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Billing;
using Common.Tools;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using Inforoom2.Test.Infrastructure.Helpers;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;
using AppealType = Inforoom2.Models.AppealType;
using Client = Inforoom2.Models.Client;
using InternetSettings = Inforoom2.Models.InternetSettings;
using Status = Inforoom2.Models.Status;
using StatusType = Inforoom2.Models.StatusType;

namespace Inforoom2.Test.BillingCustomCheck
{
	[TestFixture]
	class Run : MySeleniumFixture
	{
		protected ISession DbSession;
		protected int DaysToRun = 34;
		protected List<int> ClientsToRun = new List<int>() {
				22661,
				23337,
				23339,
				23341,
				22401,
				23343,
				22387,
				22387,
				4365,
				14873,
				5189
			};

		[SetUp]
		public override void IntegrationSetup()
		{
			//Ставим куки, чтобы не отображался popup
			DbSession = MvcApplication.SessionFactory.OpenSession();

		}

		[TearDown]
		public override void IntegrationTearDown()
		{
			DbSession.Close(); 
		}
		
		[Test, Ignore("Тест только для ручного запуска")]
		public void TestClientsWithBilling()
		{
			var lastPayment = DbSession.Query<Models.WriteOff>().OrderByDescending(s => s.WriteOffDate).First();
			SystemTime.Now = () => lastPayment.WriteOffDate <= DateTime.Now ? DateTime.Now : lastPayment.WriteOffDate;
			for (int i = 0; i < DaysToRun; i++) {
				var date = SystemTime.Now().AddDays(1);
				SystemTime.Now = () => date;
				//PrepareData(i);
				//DbSession.Transaction.Commit();
				RunBillingProcess(true);
				DbSession.Flush();
				Console.WriteLine($"Current Day {SystemTime.Now().ToString()}");
			}
		}

		public void PrepareData(int iteration)
		{
			UpdateDBSession();
			var statusDissolved = Status.Get(StatusType.Dissolved, DbSession);
			foreach (var item in DbSession.Query<Client>().Where(s=>s.Status.Id != statusDissolved.Id)) {
				if (ClientsToRun.Any(s => s == item.Id)) {
					item.PaidDay = false;
					//if (iteration == 0 && item.PhysicalClient?.Plan?.PlanChangerData != null) {
					//	item.PhysicalClient.LastTimePlanChanged =
					//		SystemTime.Now()
					//			.AddDays(-item.PhysicalClient.Plan.PlanChangerData.Timeout+ item.PhysicalClient.Plan.PlanChangerData.NotifyDays.Value);
					//}
					DbSession.Save(item);
					DbSession.Flush();
					continue;
				}
				//Endpoints удалять не нужно TODO: после перехода на новую админку поправить!
				var endpointLog =
					item.Endpoints.Where(e => !e.Disabled && e.Switch != null)
						.Implode(e => String.Format("Удалено подключение: коммутатор {0} порт {1}", e.Switch.Name, e.Port), Environment.NewLine);
				item.Appeals.Add(new Appeal(endpointLog, item, AppealType.System));
				item.Endpoints.Each(s => {
					s.Switch = null;
					s.Port = 0;
					s.Disabled = true;
					s.Ip = null;
					s.StaticIpList.RemoveEach(s.StaticIpList);
					DbSession.Save(s);
				});
				item.Discount = 0;
				item.Disabled = true;
				item.AutoUnblocked = false;
				//затираем дату начала работы, чтобы не списывалась абон плата
				item.WorkingStartDate = null;
				item.SetStatus(statusDissolved);

				DbSession.Save(item);
			}
			DbSession.Transaction.Commit();
			DbSession.Close();
			DbSession = MvcApplication.SessionFactory.OpenSession();
			UpdateDBSession();
		}

		private void UpdateDBSession()
		{
			if (!DbSession.Transaction.IsActive) {
				DbSession.Transaction.Begin();
			} else {
				DbSession.Transaction.Commit();
				DbSession.Transaction.Begin();
			}
		}

		public MainBilling GetBilling()
		{
			MainBilling.InitActiveRecord();
			var billing = new MainBilling();
			return billing;
		}

		/// <summary>
		/// Биллиинг - полный цикл
		/// </summary>
		/// <param name="client"></param>
		public void RunBillingProcess(bool runDefaultWriteoff = false)
		{
			RunBillingProcessClientEndpointSwitcher();
			RunBillingProcessPayments();
			RunBillingProcessPayments();
			if (runDefaultWriteoff) {
				var settings = DbSession.Query<InternetSettings>().FirstOrDefault();
				settings.NextBillingDate = SystemTime.Now();
				settings.LastStartFail = false;
				DbSession.Save(settings);
				DbSession.Flush();

				GetBilling().Run();
			} else {
			RunBillingProcessWriteoffs();
			}
		}

		/// <summary>
		/// обработка списаний, деактивация клиента при характерном балансе, warning
		/// </summary>
		/// <param name="client"></param>
		/// <param name="checkForWarning"></param>
		protected void RunBillingProcessWriteoffs()
		{
			var billing = GetBilling();
			billing.ProcessWriteoffs();
		}

		/// <summary>
		/// обработка платежей, активация/деактивация заказов, активация клиента при характерном балансе
		/// </summary>
		/// <param name="client"></param>
		protected void RunBillingProcessPayments()
		{
			var billing = GetBilling();
			billing.ProcessPayments();
		}

		/// <summary>
		/// активация точек подключения, активация списаний у клиентов (при проставлении BeginWork)
		/// </summary>
		/// <param name="client"></param>
		protected void RunBillingProcessClientEndpointSwitcher()
		{
			var billing = GetBilling();
			billing.SafeProcessClientEndpointSwitcher();
		}

	}
}
