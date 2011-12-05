using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Topshelf.Configuration.Dsl;
using Topshelf.Shelving;
using log4net.Config;

namespace InternetInterface.Background
{
	public class Bootstrapper : Bootstrapper<Waiter>
	{
		public void InitializeHostedService(IServiceConfigurator<Waiter> cfg)
		{
			XmlConfigurator.Configure();
			cfg.HowToBuildService(n => new Waiter());
			cfg.WhenStarted(s => s.DoStart());
			cfg.WhenStopped(s => s.Stop());
		}
	}
}
