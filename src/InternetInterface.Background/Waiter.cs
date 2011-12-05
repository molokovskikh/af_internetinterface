using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Billing;
using Common.Tools;
using log4net;

namespace InternetInterface.Background
{
	public class Waiter : RepeatableCommand
	{
		private static ILog log = LogManager.GetLogger(typeof (Waiter));

		public Waiter()
		{
			Delay = (int) TimeSpan.FromMinutes(1).TotalMilliseconds;
			Action = () => {
				new MailEndpointProcessor().Process();
			};
		}

		public void DoStart()
		{
			MainBilling.InitActiveRecord();

			Start();
		}


		public override void Error(Exception e)
		{
			log.Error(e);
		}
	}
}

