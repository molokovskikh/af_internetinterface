using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using Billing;
using Common.Tools;
using log4net;

namespace BillingService
{
	public partial class BllingService : ServiceBase
	{
		private List<RepeatableCommand> commands = new List<RepeatableCommand>();
		private ILog log = LogManager.GetLogger(typeof(BllingService));

		public BllingService()
		{
			InitializeComponent();
		}

		protected override void OnStart(string[] args)
		{
			try {
				var billing = new MainBilling();

				commands.Add(new RepeatableCommand(billing.On, 600000));
				commands.Add(new RepeatableCommand(billing.Run, 180000));

				commands.Each(c => c.Start());
			}
			catch (Exception e) {
				log.Error("Ошибка при запуске сервиса", e);
			}
		}

		protected override void OnStop()
		{
			try {
				RepeatableCommand.StopAll(commands);
			}
			catch (Exception e) {
				log.Error("Ошибка при остановке сервиса", e);
			}
		}
	}
}