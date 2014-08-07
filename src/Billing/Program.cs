using System;
using Common.Tools;
using Common.Tools.Threading;
using log4net;

namespace Billing
{
	public class Program
	{
		private static ILog log = LogManager.GetLogger(typeof(Program));

		public static int Main(string[] args)
		{
			try {
				var billing = new MainBilling();
				var cmds = new[] {
					new RepeatableCommand(billing.SafeProcessPayments, 600000),
					new RepeatableCommand(billing.Run, 180000),
				};
				return CommandService.Start(args, cmds);
			}
			catch(Exception e) {
				log.Error("Ошибка при запуске приложения", e);
				return 1;
			}
		}
	}
}