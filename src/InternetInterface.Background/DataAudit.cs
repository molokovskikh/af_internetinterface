using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using Common.Tools;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers;
using InternetInterface.Helpers;
using InternetInterface.Models;
using InternetInterface.Services;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping;

namespace InternetInterface.Background
{
	/// <summary>
	/// Аудит данных и восстановление их, в случае, если они неверны.
	/// Также отсылка сообщений на почту, в случае, если данные неправильные, но мы не уверены, почему
	/// </summary>
	public class DataAudit : Task
	{
		protected Mailer Mailer;
		public NameValueCollection Reports;

		//todo Исправить идиотизм. В реальности создается таск без сесии, но в тестах правильнее передавать сессию. Приходится дублировать конструктор
		public DataAudit()
		{
			Mailer = new Mailer();
			Reports = new NameValueCollection();
		}

		public DataAudit(ISession session)
			: base(session)
		{
			Mailer = new Mailer();
			Reports = new NameValueCollection();
		}

		protected override void Process()
		{
			var settings = Session.Query<InternetSettings>().First();

			//Если не прошло дня то выходим, так как аудит должен случаться раз в день, а запускается процесс каждые пол часа
			if (settings.NextDataAuditDate != null && settings.NextDataAuditDate.Value > SystemTime.Now())
				return;

			//Формируем следующую дату прохода
			var date = SystemTime.Now();
			date = date.AddDays(1);
			settings.NextDataAuditDate = new DateTime(date.Year, date.Month, date.Day, 23, 50, 0);
			Session.Save(settings);

			CheckForSuspiciousClient();
			CheckForHouseObjAbsence();
			CheckForDefferedPaymentFailure();
			Session.Flush();
		}

		/// <summary>
		/// Поиск физиков без параметра HouseObj
		/// </summary>
		/// <returns>Содержание сообщения</returns>
		public void CheckForHouseObjAbsence()
		{
			var clients = Session.Query<Client>().Where(s => s.PhysicalClient != null && s.PhysicalClient.HouseObj == null && s.Status.Id != 10 && s.Status.Id != 3 && s.Status.Id != 1).Select(s => s.Id).ToList();
			if (clients.Count == 0)
				return; 
			for (int i = 0; i < clients.Count; i++)
				UpdateOldAddressHelper.UpdateOldAddressOfPhysicByClientId(Convert.ToInt32(clients[i]), Session); 
		}

		/// <summary>
		///  Обработка отсутствия значения ratedPeriodDate у пользователей
		/// </summary>
		public void CheckForSuspiciousClient()
		{
			var sb = new StringBuilder();
			var status = Session.Load<Status>((uint)StatusType.Worked);
			var clients = Session.Query<Client>().Where(i => i.PhysicalClient != null && i.Disabled == false && i.Status == status && i.RatedPeriodDate == null).ToList();
			if (clients.Count == 0) {
				return;
			}
			foreach (var client in clients) {
				//Формируем сообщения для пользователей
				var url = $"http://stat.ivrn.net/cp/Client/{(client.PhysicalClient!=null ? "InfoPhysical" : "InfoLegal")}/" + client.Id;
				sb.Append("<br/>\n<a href='" + url + "'>");
				sb.Append(client.Id);
				sb.Append("</a>");
				var ips = Session.Query<StaticIp>().Where(i => client.Endpoints.Contains(i.EndPoint)).ToList();
				if (ips.Count > 0) {
					client.RatedPeriodDate = SystemTime.Now();
					Session.Save(client);
					sb.Append(" (Дата расчетного периода проставлена автоматически, так как клиент скорее всего является антенщиком)");
				}
			}
			SendReport("Подозрительные клиенты в InternetInterface: " + clients.Count, sb.ToString());
		}

		/// <summary>
		/// Поиск клиентов, у которых проблема с подключением услуги обещанный платеж
		/// </summary>
		public void CheckForDefferedPaymentFailure()
		{
			var services = Session.Query<ClientService>().Where(i => i.Service.HumanName.Contains("Обещанный") && i.EndWorkDate > SystemTime.Now()).ToList();
			var sb = new StringBuilder();
			var send = false;
			foreach (var service in services) {
				var client = service.Client;
				var active = !service.IsDeactivated;
				if (client.Status.Type != StatusType.Worked) {
					send = true;
					var msg = String.Format("Клиент {0} с услугой обещанный платеж заблокирован. Сервис активен:{1}, должен быть заблокирован: {2}", client.Id, active, client.CanBlock());
					sb.AppendLine(msg);
				}
			}
			sb.AppendLine("Всего клиентов c услугой: " + services.Count);
			if(send)
				SendReport("Обнаружены проблемы с услугой обещанный платеж", sb.ToString());
		}

		/// <summary>
		/// Отправляет отчет об аудите данных.
		/// </summary>
		/// <param name="title">Название отчета (заголовок)</param>
		/// <param name="body">Тело отчета</param>
		private void SendReport(string title, string body)
		{
			Reports.Add(title, body);
			Mailer.SendText("service@analit.net", "service@analit.net", title, body);
		}
	}
}