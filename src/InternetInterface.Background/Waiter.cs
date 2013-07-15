using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using Billing;
using Castle.ActiveRecord;
using Common.Tools;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.Models.Jobs;
using InternetInterface.Helpers;
using InternetInterface.Models;
using log4net;

namespace InternetInterface.Background
{
	public class Waiter : RepeatableCommand
	{
		private static ILog log = LogManager.GetLogger(typeof(Waiter));
		private List<ActiveRecordJob> jobs = new List<ActiveRecordJob>();

		public Waiter()
		{
			Delay = (int)TimeSpan.FromHours(1).TotalMilliseconds;
			Action = () => {
				var tasks = new Task[] {
					new DeleteFixIpIfClientLongDisable(),
					new SendNullTariffLawyerPerson(),
					new SendUnknowEndPoint(),
					new SendSmsNotification()
				};

				DoTask(tasks);

				using (new SessionScope())
					jobs.Each(j => j.Run());
			};
		}

		private void DoTask(Task[] tasks)
		{
			foreach (var task in tasks) {
				task.Execute();
			}
		}

		public void DoStart()
		{
			ActiveRecordStarter.EventListenerComponentRegistrationHook += RemoverListner.Make;
			StandaloneInitializer.Init(typeof(Waiter).Assembly);

			Start();
		}

		public override void Error(Exception e)
		{
			log.Error(e);
		}
	}
}