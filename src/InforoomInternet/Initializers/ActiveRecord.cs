using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Controllers;
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
			var namespaces = new[] { "Common.Web.Ui.Models.Wiki", "Common.Web.Ui.Models.Editor" };
			AdditionalTypes = typeof(BaseContentController).Assembly.GetTypes()
				.Where(t => namespaces.Contains(t.Namespace))
				.ToArray();
		}
	}
}