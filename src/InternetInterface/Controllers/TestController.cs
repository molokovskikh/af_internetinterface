using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Net;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using Castle.Core.Internal;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Tools;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.MonoRailExtentions;
using Common.Web.Ui.NHibernateExtentions;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using InternetInterface.Models.Services;
using InternetInterface.Queries;
using InternetInterface.Services;
using NHibernate;
using NHibernate.Linq;
using NHibernate.SqlCommand;
using NHibernate.Transform;
using NPOI.HPSF;
using NPOI.SS.UserModel;
using Contact = InternetInterface.Models.Contact;
using ContactType = InternetInterface.Models.ContactType;
using TextHelper = InternetInterface.Helpers.TextHelper;

namespace InternetInterface.Controllers
{
	/// <summary>
	/// Контроллер для проведения тестов.
	/// </summary>
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class TestController : InternetInterfaceController
	{
		public TestController()
		{
			BeforeAction += (action, context1, controller, controllerContext) => {
				var type = typeof(TestController);
				var actions = type.GetMethods().Where(i => i.IsPublic && i.DeclaringType.Name == type.Name).Select(i => i.Name).ToList();
				PropertyBag["actions"] = actions;
				PropertyBag["content"] = "";
				//controllerContext.PropertyBag["MapPartner"] = InitializeContent.Partner;
			};
			AfterAction += (action, context1, controller, controllerContext) => { RenderView("index"); };
		}

		/// <summary>
		/// Проверка обработки списаний с юридических лиц
		/// </summary>
		/// <param name="id">Id Клиента</param>
		/// <param name="days">Сколько дней отнять от даты (если недавно случилось)</param>
		/// <param name="date">Дата по которой должны проводиться списания (если случилось давно)</param>
		public void WriteOffLawyerTest(uint id = 0, int days = 0, DateTime? date = null)
		{
#if DEBUG
			if (id == 0)
				return;
			var time = date ?? SystemTime.Now();
			time = time.AddDays(days);
			var client = DbSession.Query<Client>().FirstOrDefault(i => i.Id == id);
			var writeoffs = client.LawyerPerson.Calculate(time, DbSession);
			DbSession.Clear();
			DbSession.Transaction.Rollback();
			PropertyBag["content"] = "Количество списаний:" + writeoffs.Count;
#endif
		}

		/// <summary>
		/// Проверка сможет ли биллиг заблокировать клиента
		/// </summary>
		/// <param name="id">Id Клиента</param>
		public void CanBlockTest(uint id = 0)
		{
#if DEBUG
			if (id == 0)
				return;
			var client = DbSession.Query<Client>().FirstOrDefault(i => i.Id == id);
			var block = client.CanBlock();
			PropertyBag["content"] = "Клиента можно заблокировать:" + block;
#endif
		}

		// "Проверка вывода суммы для разблокировки в ЛК у заблокированного клиента"
		public void PlanChangerBinder()
		{
			var allClients = DbSession.Query<Client>().Where(s => s.PhysicalClient != null
			                                                      && !s.ClientServices.Any(d => d.Service is PlanChanger)).ToList();
			var planChangerService = DbSession.Query<Service>().FirstOrDefault(s => s.HumanName == "PlanChanger");

			foreach (var client in allClients) {
				// создание сервиса PlanChanger для текущего клиента
				var clientService = new ClientService() {
					Client = client,
					Service = planChangerService,
					BeginWorkDate = SystemTime.Now(),
					IsActivated = true
				};
				client.ClientServices.Add(clientService);
				DbSession.Update(client);
			}
			DbSession.Flush();
		}
	}
}