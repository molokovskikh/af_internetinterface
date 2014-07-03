using System;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using Castle.ActiveRecord;
using Common.Tools;
using Common.Tools.Calendar;
using Common.Tools.Threading;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.Models.Jobs;
using InternetInterface.Helpers;
using log4net;
using log4net.Config;
using NHibernate.Type;

namespace InternetInterface.Background
{
	public class Program
	{
		private static ILog log = LogManager.GetLogger(typeof(Program));

		public static int Main(string[] args)
		{
			try {
				XmlConfigurator.Configure();
				ActiveRecordStarter.EventListenerComponentRegistrationHook += AuditListener.RemoveAuditListener;
				var assembly = typeof(Program).Assembly;
				StandaloneInitializer.Init(assembly);

				var tasks = assembly.GetTypes()
					.Where(t => t.IsClass && !t.IsAbstract && !t.IsInterface && typeof(Task).IsAssignableFrom(t))
					.Select(t => Activator.CreateInstance(t))
					.OfType<Task>()
					.ToArray();
				var runner = new RepeatableCommand(30.Minute(), () => tasks.Each(t => {
					try {
						t.Execute();
					}
					catch(Exception e) {
						log.Error(String.Format("Выполнение задачи {0} завершилось ошибкой", t), e);
					}
				}));

				tasks.Each(t => t.Cancellation = runner.Cancellation);

				var cmd = args.FirstOrDefault();

				if (cmd.Match("uninstall")) {
					CommandService.Uninstall();
					return 0;
				}
				if (cmd.Match("install")) {
					CommandService.Install();
					return 0;
				}
				if (cmd.Match("console")) {
					runner.Start();
					if (Console.IsInputRedirected) {
						Console.WriteLine("Для завершения нажмите ctrl-c");
						Console.CancelKeyPress += (e, a) => runner.Stop();
						runner.Cancellation.WaitHandle.WaitOne();
					}
					else {
						Console.WriteLine("Для завершения нажмите любую клавишу");
						Console.ReadLine();
						runner.Stop();
					}
					runner.Join();
					return 0;
				}

				ServiceBase.Run(new CommandService(runner));
				return 0;
			}
			catch(Exception e) {
				log.Error("Ошибка при запуске приложения", e);
				return 1;
			}
		}
	}
}