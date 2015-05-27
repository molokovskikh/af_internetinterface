using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using Common.Tools;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers;
using InternetInterface.Models;
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
		public DataAudit()
		{
		}

		public DataAudit(ISession session)
			: base(session)
		{
		}

		protected override void Process()
		{
			var settings = Session.Query<InternetSettings>().First();
			var mailhelper = new Mailer();

			//Если не прошло дня то выходим, так как аудит должен случаться раз в день, а запускается процесс каждые пол часа
			if (settings.NextDataAuditDate != null && settings.NextDataAuditDate.Value < SystemTime.Now())
				return;

			//Формируем следующую дату прохода
			var date = SystemTime.Now();
			date = date.AddDays(1);
			settings.NextDataAuditDate = new DateTime(date.Year, date.Month, date.Day, 15, 0, 0);
			Session.Save(settings);
			
			//Обработка отсутствия значения ratedPeriodDate у пользователей
			var sb = new StringBuilder();
			var status = Session.Load<Status>((uint)StatusType.Worked);
			var clients = Session.Query<Client>().Where(i => i.PhysicalClient != null && i.Disabled == false && i.Status == status && i.RatedPeriodDate == null).ToList();
			sb.AppendLine(string.Format("Найдено {0} подозрительных клиентов. Cейчас {1}, следующаяя дата: {2}", clients.Count, SystemTime.Now(), settings.NextDataAuditDate));
			foreach (var client in clients) {
				//Формируем сообщения для пользователей
				var url = "http://stat.ivrn.net/ii/UserInfo/ShowPhysicalClient?filter.ClientCode=" + client.Id;
				sb.Append("<br/><a href='" + url + "'>");
				sb.Append(client.Id);
				sb.Append("</a>");
				var ips = Session.Query<StaticIp>().Where(i => client.Endpoints.Contains(i.EndPoint)).ToList();
				if (ips.Count > 0) {
					client.RatedPeriodDate = SystemTime.Now();
					Session.Save(client);
					sb.Append(" (Дата расчетного периода проставлена автоматически, так как клиент скорее всего является антенщиком)");
				}
			}

			//@todo подумать над структурой данного класса и над адресами посылки почты - не правильно держать их как литеральные строки
			mailhelper.SendText("service@analit.net", "service@analit.net", "Подозрительные клиенты в InternetInterface: " + clients.Count, sb.ToString());
			Session.Flush();
		}
	}
}
