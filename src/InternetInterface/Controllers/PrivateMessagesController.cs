using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Tools;
using Common.Web.Ui.Controllers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using NHibernate.Criterion;
using NHibernate.SqlCommand;

namespace InternetInterface.Controllers
{
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class PrivateMessagesController : BaseController
	{
		public void ForClient(uint clientId)
		{
			var client = ActiveRecordMediator<Client>.FindByPrimaryKey(clientId);
			var message = MessageForClient.Queryable.FirstOrDefault(m => m.Client.Id == clientId);
			message = message ?? new MessageForClient();
			PropertyBag["PrivateMessage"] = message;
			PropertyBag["client"] = client;
			if (IsPost) { 
				BindObjectInstance(message, "PrivateMessage", AutoLoadBehavior.NewInstanceIfInvalidKey);
				if (IsValid(message)) {
					message.Registrator = InitializeContent.Partner;
					message.Save();
					PropertyBag["Message"] = Message.Notify("Сохранено");
				}
			}
		}

		public void ForSwitch(uint switchId)
		{
			var _switch = ActiveRecordMediator<NetworkSwitches>.FindByPrimaryKey(switchId);
			PropertyBag["switch"] = _switch;
			var messages = ClientEndpoints.FindAll(DetachedCriteria.For(typeof(ClientEndpoints))
				.CreateAlias("Client", "c", JoinType.InnerJoin)
				.CreateAlias("c.Message", "m", JoinType.InnerJoin)
				.Add(Expression.Eq("Switch", _switch))).Select(e => e.Client.Message).ToList();
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
				var clients = ClientEndpoints.Queryable.Where(e => e.Switch == _switch).Select(e => e.Client).ToList();
				var applyCount = 0;
				var errorClients = new List<uint>();
				foreach (var client in clients) {
					var toSave = client.Message;
					toSave = toSave ?? new MessageForClient {Client = client};
					toSave.Registrator = InitializeContent.Partner;
					BindObjectInstance(toSave, "PrivateMessage", AutoLoadBehavior.OnlyNested);
					if (IsValid(message)) {
						toSave.Save();
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