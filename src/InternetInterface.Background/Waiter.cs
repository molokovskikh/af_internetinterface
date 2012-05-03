using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using Billing;
using Castle.ActiveRecord;
using Common.Tools;
using Common.Web.Ui.Models.Jobs;
using InternetInterface.Helpers;
using InternetInterface.Models;
using log4net;

namespace InternetInterface.Background
{
	public class Waiter : RepeatableCommand
	{
		private static ILog log = LogManager.GetLogger(typeof (Waiter));
		private List<ActiveRecordJob> jobs = new List<ActiveRecordJob>();

		public Waiter()
		{
			Delay = (int) TimeSpan.FromHours(1).TotalMilliseconds;
			Action = () => {
				SendProcessor.Process();

				using(new SessionScope())
					jobs.Each(j => j.Run());
			};
		}

		public void DoStart()
		{
			StandaloneInitializer.Init(typeof(SendProcessor).Assembly);

			using(new SessionScope())
			{
				var mailer = new Mailer {
					SiteRoot = ConfigurationManager.AppSettings["SiteRoot"]
				};
				var job = new ActiveRecordJob(new BuildInvoiceTask(mailer)) {Name = "InternetInvoce"};
				jobs.Add(job);
			}

			Start();
		}

		public override void Error(Exception e)
		{
			log.Error(e);
		}
	}
}

