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
using InternetInterface.Models;
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
				var runner = new RepeatableCommand(30.Minute(), x => tasks.Each(t => {
					try {
						t.Execute();
					}
					catch(Exception e) {
						log.Error($"Выполнение задачи {t} завершилось ошибкой", e);
					}
				}));
				tasks.Each(t => t.Cancellation = runner.Cancellation);
				return CommandService.Start(args, runner);
			}
			catch(Exception e) {
				log.Error("Ошибка при запуске приложения", e);
				return 1;
			}
		}
	}
}