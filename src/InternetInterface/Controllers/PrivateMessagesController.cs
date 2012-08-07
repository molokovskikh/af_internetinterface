using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.Components.Binder;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Tools;
using Common.Web.Ui.Controllers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using NHibernate.Criterion;
using NHibernate.Linq;
using NHibernate.SqlCommand;

namespace InternetInterface.Controllers
{
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class PrivateMessagesController : BaseController
	{
		public PrivateMessagesController()
		{
			SetBinder(new ARDataBinder());
		}

		public void ForClient(uint clientId)
		{
			var client = DbSession.Load<Client>(clientId);
			var message = DbSession.Query<MessageForClient>().FirstOrDefault(m => m.Client.Id == clientId);
			message = message ?? new MessageForClient();
			PropertyBag["PrivateMessage"] = message;
			PropertyBag["client"] = client;
			if (IsPost) { 
				BindObjectInstance(message, "PrivateMessage", AutoLoadBehavior.NewInstanceIfInvalidKey);
				if (IsValid(message)) {
					message.Registrator = InitializeContent.Partner;
					DbSession.Save(message);
					Notify("Сохранено");
				}
			}
		}

		public void ForSwitch(uint switchId)
		{
			var @switch = DbSession.Load<NetworkSwitches>(switchId);
			PropertyBag["switch"] = @switch;
			var messages = ClientEndpoint.FindAll(DetachedCriteria.For(typeof(ClientEndpoint))
				.CreateAlias("Client", "c", JoinType.InnerJoin)
				.CreateAlias("c.Message", "m", JoinType.InnerJoin)
				.Add(Expression.Eq("Switch", @switch))).Select(e => e.Client.Message).ToList();
			var message = new MessageForClient();
			if (messages.Count > 0)
				message = messages
					.GroupBy(k => k.Text)
					.Select(g => new { count = g.Count(), message = g.FirstOrDefault()})
					.OrderByDescending(o => o.count)
					.Select(s => s.message)
					.FirstOrDefault();
			PropertyBag["PrivateMessage"] = message;
			if (IsPost) {
				var clients = ClientEndpoint.Queryable.Where(e => e.Switch == @switch).Select(e => e.Client).ToList();
				var applyCount = 0;
				var errorClients = new List<uint>();
				foreach (var client in clients) {
					var toSave = client.Message;
					toSave = toSave ?? new MessageForClient {Client = client};
					toSave.Registrator = InitializeContent.Partner;
					BindObjectInstance(toSave, "PrivateMessage", AutoLoadBehavior.OnlyNested);
					if (IsValid(message)) {
						DbSession.Save(toSave);
						applyCount ++;
					}
					else {
						errorClients.Add(client.Id);
					}
				}
				if (clients.Count != applyCount) {
					PropertyBag["Message"] = Message.Error(string.Format("Сообщения для клиентов {0} не были сохранены! в них содержатся ошибки", errorClients.Implode()));
				}
				else {
					PropertyBag["Message"] = Message.Notify("Сохранено");
				}
			}
		}
	}
}