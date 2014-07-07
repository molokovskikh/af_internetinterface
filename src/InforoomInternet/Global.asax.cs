using System;
using System.Reflection;
using Castle.MonoRail.Framework.Routing;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.MonoRailExtentions;

namespace InforoomInternet
{
	public class Global : WebApplication
	{
		public Global()
			: base(Assembly.Load("InforoomInternet"))
		{
			LibAssemblies.Add(Assembly.Load("Common.Web.Ui"));
			Logger.ErrorSubject = "Ошибка в IVRN";
		}

		private void Application_Start(object sender, EventArgs e)
		{
			try {
				InstallBundle("jquery.calendar.support");
				InstallBundle("jquery.validate");

				Initialize();

				MixedRouteHandler.ConfigRoute();

				RoutingModuleEx.Engine.Add(new PatternRoute("/")
					.DefaultForController().Is("Content")
					.DefaultForAction().Is("Новости"));

				RoutingModuleEx.Engine.Add(new PatternRoute("/Warning")
					.DefaultForController().Is("Main")
					.DefaultForAction().Is("Warning"));

				//Эта страница находится гуглом по запросу Воронеж ООО Инфорум
				RoutingModuleEx.Engine.Add(new PatternRoute("/Main/requisite")
					.DefaultForController().Is("Content")
					.DefaultForAction().Is("Реквизиты"));

				RoutingModuleEx.Engine.Add(new PatternRoute("/Login/<action>")
					.DefaultForController().Is("Login")
					.DefaultForAction().Is("LoginPage"));
			}
			catch (Exception ex) {
				Log.Fatal("Ошибка при запуске страницы.", ex);
			}
		}
	}
}