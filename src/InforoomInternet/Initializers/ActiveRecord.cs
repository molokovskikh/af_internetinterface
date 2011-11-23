using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.ActiveRecord.Framework.Config;
using Common.Web.Ui.Models.Editor;
using InternetInterface.Models;
using NHibernate.Engine;
using NHibernate.Linq;
using NHibernate.Mapping;
using NHibernate.Properties;
using NHibernate.Type;

namespace InforoomInternet.Initializers
{
	public class ActiveRecord
	{
		public void Initialize(IConfigurationSource config)
		{
			ActiveRecordStarter.Initialize(
				new[] {Assembly.Load("InforoomInternet"), Assembly.Load("InternetInterface")/*, Assembly.Load("Common.Web.Ui")*/},
				config, new [] {typeof(MenuField), typeof(SiteContent), typeof(SubMenuField)});

			SetupFilters();
			SetDefaultValues();
		}

		private void SetDefaultValues()
		{
			var configuration = ActiveRecordMediator.GetSessionFactoryHolder().GetAllConfigurations()[0];
			foreach(var clazz in configuration.ClassMappings)
			{
				//тут баг для nested объектов я не выставлю is null
				foreach(var property in clazz.PropertyIterator)
				{
					var getter = property.GetGetter(clazz.MappedClass);
					var type = getter.ReturnType;
					foreach (Column column in property.ColumnIterator)
					{
						//var type = ((SimpleValue)column.Value).Type.ReturnedClass;
						if (String.IsNullOrEmpty(column.DefaultValue)
							&& column.IsNullable
								&& !IsNullableType(type))
						{
							column.IsNullable = false;
							column.DefaultValue = "0";
						}
					}
				}
			}
		}

		private bool IsNullableType(Type type)
		{
			if (!type.IsValueType)
				return true;

			if (type.IsNullable())
				return true;

			return false;
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