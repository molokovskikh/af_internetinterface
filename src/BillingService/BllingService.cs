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
	public class RunCommand : RepeatableCommand
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof(RunCommand));

		public RunCommand(Action action, int delay)
			: base(action, delay)
		{
		}

		public override void Error(Exception e)
		{
			_log.Error(e.Message);
		}
	}

	public partial class BllingService : ServiceBase
	{
		private RunCommand computeCommand;
		private RunCommand OnCommand;
		private static MainBilling billing;

		public BllingService()
		{
			InitializeComponent();
		}

		protected override void OnStart(string[] args)
		{
			billing = new MainBilling();

			OnCommand = new RunCommand(billing.On , 600000);
			OnCommand.Start();

			computeCommand = new RunCommand(billing.Run, 86400000);
			computeCommand.Start();
		}

		protected override void OnStop()
		{
			OnCommand.Stop();
			computeCommand.Stop();
		}
	}
}
