using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Configuration;
using Castle.MonoRail.Framework.Internal;
using Castle.MonoRail.Framework.Services;
using Castle.MonoRail.Views.Brail;
using Common.Web.Ui.Models.Jobs;
using Common.Web.Ui.MonoRailExtentions;

namespace InternetInterface.Helpers
{
	public class StandaloneInitializer
	{
		public static IViewEngineManager Init(Assembly assembly = null)
		{
			if (assembly == null)
				assembly = Assembly.GetEntryAssembly();

			ActiveRecordStarter.Initialize(new[] { Assembly.Load("InternetInterface") }, ActiveRecordSectionHandler.Instance, typeof(JobLog));

			return InitMailer(assembly);
		}

		public static IViewEngineManager InitMailer(Assembly assembly)
		{
			var config = new MonoRailConfiguration();
			config.ViewEngineConfig.ViewEngines.Add(new ViewEngineInfo(typeof(BooViewEngine), false));

			var loader = new FileAssemblyViewSourceLoader("");
			loader.AddAssemblySource(new AssemblySourceInfo(assembly, assembly.GetName().Name + ".Views"));

			var provider = new FakeServiceProvider();
			provider.Services.Add(typeof(IMonoRailConfiguration), config);
			provider.Services.Add(typeof(IViewSourceLoader), loader);

			var manager = new DefaultViewEngineManager();
			manager.Service(provider);
			var options = ((BooViewEngine)manager.ResolveEngine(".brail")).Options;
			var namespaces = options.NamespacesToImport;
			namespaces.Add("Boo.Lang.Builtins");

			BaseMailer.ViewEngineManager = manager;
			return manager;
		}
	}
}