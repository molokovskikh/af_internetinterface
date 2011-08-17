using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using log4net;

namespace BillingService
{
	static class Program
	{
        private static ILog _log = LogManager.GetLogger(typeof(Program));
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main()
		{
            try
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] {
                                                      new BllingService()
                                                  };
                ServiceBase.Run(ServicesToRun);
            }
            catch (Exception e)
            {
                _log.Error("Ошибка при инициализации приложения", e);
            }
		}
	}
}
