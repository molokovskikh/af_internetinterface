using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Internal;
using Castle.MonoRail.Framework;
using Common.Tools.Calendar;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Helpers;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NHibernate.Linq;
using log4net;
using log4net.Config;
using FileHelper = Common.Tools.FileHelper;

namespace InternetInterface.Printer
{
	public class Program
	{
		public static void Main(string[] args)
		{
			//сохранение счетов\актов производится внутри транзации что бы она гарантировано
			//закончилась и акты стали доступны нужно немного подождать
			Thread.Sleep(1000);

			XmlConfigurator.Configure();
			var logger = LogManager.GetLogger(typeof(Program));
			try {
				var printer = args[1];
				var name = args[0];
				var ids = args[2].Split(',').Select(id => Convert.ToUInt32(id.Trim())).ToArray();

				var brail = StandaloneInitializer.Init();
				IEnumerable documents = null;
				using (new SessionScope(FlushAction.Never)) {
					if (name == "invoice") {
						ArHelper.WithSession(s => {
							documents = s.Query<Invoice>().Where(a => ids.Contains(a.Id))
								.OrderBy(a => a.PayerName)
								.ToArray();
						});
					}
					else if(name == "act") {
						ArHelper.WithSession(s => {
							documents = s.Query<Act>().Where(a => ids.Contains(a.Id))
								.OrderBy(a => a.PayerName)
								.ToArray();
						});
					}
					else if(name == "contract") {
						ArHelper.WithSession(s => {
							documents = s.Query<Contract>().Where(a => ids.Contains(a.Id))
								.OrderBy(a => a.Customer)
								.ToArray();
						});
					}

					Print(brail, printer, name, documents);
				}
			}
			catch (Exception e) {
				logger.Error("Ошибка при печати", e);
			}
		}

		private static void Print(IViewEngineManager brail, string printer, string name, IEnumerable documents)
		{
			var plural = Inflector.Pluralize(name);
			if (String.IsNullOrEmpty(plural))
				plural = name;
			var singular = Inflector.Singularize(name);
			if (String.IsNullOrEmpty(singular))
				singular = name;

			using (new SessionScope(FlushAction.Never)) {
				foreach (var document in documents) {
					var file = Path.Combine(Path.GetTempPath(), Path.ChangeExtension(Path.GetRandomFileName(), ".html"));
					try {
						var arguments = new Dictionary<string, object> {
							{ singular, document },
							{ "doc", document },
							//тк chrome.printer не печатает у него media будет screen а не print
							//что бы стили продолжели работать нужно их исправить
							{ "print", true }
						};
						RenderToFile(brail, plural, file, arguments);
						ChromePrinter(printer, file);
					}
					finally {
						File.Delete(file);
					}
				}
			}
		}

		private static void ChromePrinter(string printer, string file)
		{
			var printerExe = ConfigurationManager.AppSettings["ChromePrinter"];
			var exe = FileHelper.MakeRooted(printerExe);

			try {
				var process = Process.Start(exe, String.Format("\"{0}\" \"{1}\"", printer, new Uri(file)));
				if (!process.WaitForExit((int)30.Second().TotalMilliseconds)) {
					process.Kill();
					throw new Exception(String.Format("Не удалось напечатать документ {0} принтер {1}, процесс печати не завершился за 30с",
						file, printer));
				}
				if (process.ExitCode != 0) {
					throw new Exception(String.Format("Не удалось напечатать документ {0} принтер {1}, процесс печати завершился с кодом ошибки {2}",
						file, printer, process.ExitCode));
				}
			}
			catch (Win32Exception e) {
				if (((uint)e.ErrorCode) == 0x80004005)
					throw new Exception(String.Format("Не удалось найти файл {0}", exe), e);
				throw;
			}
		}

		private static void RenderToFile(IViewEngineManager brail, string name, string file, Dictionary<string, object> arguments)
		{
			using (var writer = new StreamWriter(File.Create(file))) {
				brail.Process(
					Inflector.Capitalize(name) + "/Print",
					"Print",
					writer,
					arguments);
			}
		}
	}
}