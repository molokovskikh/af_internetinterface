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
		private MemorableRepeatableCommand computeCommand;
		private MemorableRepeatableCommand OnCommand;
		private static MainBilling billing;
		private ILog log = LogManager.GetLogger(typeof(BllingService));

		public BllingService()
		{
			InitializeComponent();
		}

		protected override void OnStart(string[] args)
		{
			try
			{
				billing = new MainBilling();

				OnCommand = new MemorableRepeatableCommand(billing.On, 600000);
				OnCommand.Start();

				computeCommand = new MemorableRepeatableCommand(billing.Run, 180000);
				computeCommand.Start();
			}
			catch (Exception e)
			{
				log.Error("Ошибка при запуске сервиса", e);
			}
		}

		protected override void OnStop()
		{
			try
			{
				OnCommand.Stop();
				computeCommand.Stop();
			}
			catch (Exception e)
			{
				log.Error("Ошибка при остановке сервиса", e);
			}
		}
	}
}
