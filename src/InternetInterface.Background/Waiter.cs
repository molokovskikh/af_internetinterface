using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Billing;
using Castle.ActiveRecord;
using Common.Tools;
using Common.Web.Ui.Models.Jobs;
using log4net;

namespace InternetInterface.Background
{
	public class Waiter : RepeatableCommand
	{
		private static ILog log = LogManager.GetLogger(typeof (Waiter));
		private List<ActiveRecordJob> jobs = new List<ActiveRecordJob>();

		public Waiter()
		{
			Delay = (int) TimeSpan.FromMinutes(1).TotalMilliseconds;
			Action = () => {
				SendProcessor.Process();

				using(new SessionScope())
					jobs.Each(j => j.Run());
			};
		}

		public void DoStart()
		{
			MainBilling.InitActiveRecord();

			using(new SessionScope())
			{
				var job = new ActiveRecordJob(new BuildInvoiceTask());
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

