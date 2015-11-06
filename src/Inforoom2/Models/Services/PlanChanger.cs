using System;
using System.Linq;
using Common.Tools;
using Inforoom2.Components;
using Inforoom2.Helpers;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models.Services
{
	[Subclass(0, ExtendsType = typeof(Service), DiscriminatorValue = "PlanChanger")]
	public class PlanChanger : Service
	{
		/// <summary>
		/// при входе на сайт вызывается процедура
		/// </summary> 
		public override void OnWebsiteVisit(ControllerAndServiceMediator mediator, ISession session, Client client)
		{
			mediator.UrlRedirectAction = "";
			mediator.UrlRedirectController = "";
			bool currentRedirectFromCurrentController = true;
			var spletedUrl = mediator.UrlCurrent.Split('/');
			if (spletedUrl.Length > 1) {
				var currentController = spletedUrl[1].ToLower();
				var currentAction = spletedUrl.Length > 2 ? spletedUrl[2].ToLower() : "";
				currentRedirectFromCurrentController =
					!((currentController == "account" && currentAction == "login")
					  || (currentController == "account" && currentAction == "logout"));
			}
			var urlToCheck = mediator.UrlCurrent == null ? "" : mediator.UrlCurrent.ToLower().Replace("/", "");
			var urlToDrope = ("service/internetplanchanger").Replace("/", "");
			var urlToDropeLogout = ("account/logout").Replace("/", "");
			bool urltoRedirect = urlToCheck.IndexOf(urlToDrope) == -1 && urlToCheck.IndexOf(urlToDropeLogout) == -1;

			if (client != null && urltoRedirect && currentRedirectFromCurrentController) {
				// получение сведения об изменении тарифов 
				foreach (var changer in session.Query<PlanChangerData>().ToList()) {
					// поиск целевого тарифа, фильтрация url с разрешенными тарифными планами 
					if (changer.TargetPlan.Id == client.PhysicalClient.Plan.Id && urltoRedirect) {
						// если услуга существует,проверка, не подошел ли срок отключения клиента.
						if (client.ClientServices.Any(s => s.Service.Name == "PlanChanger")
						    && client.PhysicalClient.LastTimePlanChanged != Convert.ToDateTime("01.01.0001")
						    && (client.PhysicalClient.LastTimePlanChanged.AddDays(changer.Timeout) < Convert.ToDateTime(SystemTime.Now()))) {
							// если срок подошел редиректим на страницу смены тарифа 
							mediator.UrlRedirectAction = "InternetPlanChanger";
							mediator.UrlRedirectController = "Service";
						}
					}
				}
			}
		}

		public static bool IsPlanchangerTimeOff(ISession session, Client client)
		{
			if (client != null && client.PhysicalClient != null) {
				// получение сведения об изменении тарифов 
				foreach (var changer in session.Query<PlanChangerData>().ToList()) {
					// поиск целевого тарифа, фильтрация url с разрешенными тарифными планами 
					if (changer.TargetPlan.Id == client.PhysicalClient.Plan.Id) {
						// если услуга существует,проверка, не подошел ли срок отключения клиента.
						if (client.ClientServices.Any(s => s.Service.Name == "PlanChanger")
						    && client.PhysicalClient.LastTimePlanChanged != Convert.ToDateTime("01.01.0001")
						    && (client.PhysicalClient.LastTimePlanChanged.AddDays(changer.Timeout) < Convert.ToDateTime(SystemTime.Now()))) {
							// если срок подошел возвращаем результат проверки
							return true;
						}
					}
				}
			}
			// если срок подошел возвращаем результат проверки
			return false;
		}
	}
}