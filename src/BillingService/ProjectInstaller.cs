using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;


namespace BillingService
{
	[RunInstaller(true)]
	public partial class ProjectInstaller : Installer
	{
		public ProjectInstaller()
		{
			Installers.AddRange(new Installer[] {
				new ServiceProcessInstaller {
					Account = ServiceAccount.NetworkService
				},
				new ServiceInstaller {
					DisplayName = "Биллинг доступа в интернет",
					ServiceName = "BillingInternetService",
				}
			});
		}
	}
}