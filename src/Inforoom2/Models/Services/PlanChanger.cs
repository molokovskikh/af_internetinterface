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
	[Subclass(0, ExtendsType = typeof (Service), DiscriminatorValue = "PlanChanger")]
	public class PlanChanger : Service
	{
		public const string MessagePatternDaysRemained = "Акционный период на тарифе 'Народный (300)' заканчивается {0}";
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
					!(currentController == "home" || currentController == "faq" || currentController == "about" ||
						currentController == "bussiness")
						&& !((currentController == "account" && currentAction == "login")
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
							&& (client.PhysicalClient.LastTimePlanChanged.AddDays(changer.Timeout).Date <= Convert.ToDateTime(SystemTime.Now()))) {
							// если срок подошел редиректим на страницу смены тарифа 
							mediator.UrlRedirectAction = "InternetPlanChanger";
							mediator.UrlRedirectController = "Service";
						}
					}
				}
			}
		}

		/// <summary>
		/// Дата завершения работы по акционному тарифу (когда клиенту начнет отображаться варнинг)
		/// </summary>
		/// <param name="client"></param>
		/// <returns></returns>
		public static DateTime? PlanchangerTimeOffDate(Client client)
		{
			if (client != null && client.PhysicalClient != null) {
				if (client.PhysicalClient.Plan.PlanChangerData != null) {
					if (client.ClientServices.Any(s => s.Service.Name == "PlanChanger")) {
						// если срок подошел возвращаем результат проверки
						if (client.PhysicalClient.LastTimePlanChanged == DateTime.MinValue) {
							return null;
						}
						return client.PhysicalClient.LastTimePlanChanged.AddDays(client.PhysicalClient.Plan.PlanChangerData.Timeout);
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Звершена ли работа по акционному тарифу
		/// </summary>
		/// <param name="client"></param>
		/// <returns></returns>
		public static bool IsPlanchangerTimeOff(Client client)
		{
			var dateOff = PlanchangerTimeOffDate(client);
			return dateOff.HasValue && dateOff.Value.Date <= Convert.ToDateTime(SystemTime.Now());
		}
	}
}