using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Models.Editor;
using InternetInterface.Models;
using NHibernate.Engine;
using NHibernate.Type;

namespace InforoomInternet.Initializers
{
	public class ActiveRecord : ActiveRecordInitializer
	{
		public ActiveRecord()
		{
			Assemblies = new[] { "InforoomInternet", "InternetInterface" };
			AdditionalTypes = new[] { typeof(MenuField), typeof(SiteContent), typeof(SubMenuField) };
		}

		public override void Initialize(IConfigurationSource config)
		{
			base.Initialize(config);

			SetupFilters();
		}

		public static void SetupFilters()
		{
			var configuration = ActiveRecordMediator
				.GetSessionFactoryHolder()
				.GetAllConfigurations()
				.First();
			configuration.FilterDefinitions.Add("HiddenTariffs",
				new FilterDefinition("HiddenTariffs", "", new Dictionary<string, IType>(), true));
			var mapping = configuration.GetClassMapping(typeof(Tariff));
			mapping.AddFilter("HiddenTariffs", "Hidden = 0");
		}
	}
}