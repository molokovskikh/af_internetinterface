using System;
using System.Linq;
using Common.Tools;
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
			var rootPath = ConfigHelper.GetParam("inforoom2Url");
			if (client != null && (mediator.UrlCurrent == null || mediator.UrlCurrent.IndexOf(rootPath + "Service/InternetPlanChanger") == -1)) {
				// получение сведения об изменении тарифов 
				foreach (var changer in session.Query<PlanChangerData>().ToList()) {
					// поиск целевого тарифа, фильтрация url с разрешенными тарифными планами 
					if (changer.TargetPlan.Id == client.PhysicalClient.Plan.Id &&
					    mediator.UrlCurrent.IndexOf(rootPath + "Personal/InternetChangePlan") == -1) {
						// если услуга существует,проверка, не подошел ли срок отключения клиента.
							if (client.ClientServices.Any(s => s.Service.Name == "PlanChanger")
						    && client.WorkingStartDate.HasValue
						    && (client.WorkingStartDate.Value.AddDays(changer.Timeout) < Convert.ToDateTime(SystemTime.Now()))) {
							// если срок подошел редиректим на страницу смены тарифа 
							mediator.UrlRedirectAction = "InternetPlanChanger";
							mediator.UrlRedirectController = "Service";
						}
					}
				}
			}
		}
	}
}