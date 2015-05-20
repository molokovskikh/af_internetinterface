using System;
using System.Collections.Generic;
using System.Linq;
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
			//Если не прошло дня то выходим, так как аудит должен случаться раз в день, а запускается процесс каждые пол часа
			if (settings.NextDataAuditDate != null && settings.NextDataAuditDate.Value.AddDays(1) < SystemTime.Now())
				return;
			//Формируем следующую дату прохода
			var date = SystemTime.Now();
			date = date.AddDays(1);
			settings.NextDataAuditDate = new DateTime(date.Year, date.Month, date.Day, 15, 0, 0);
			Session.Save(settings);

			//Обработка отсутствия значения ratedPeriodDate у пользователей
			var mailhelper = new Mailer();
			var sb = new StringBuilder(1000);
			var clients = Session.Query<Client>().Where(i => i.PhysicalClient != null && i.Disabled == false && i.Status.Type == StatusType.Worked && i.RatedPeriodDate == null).ToList();
			foreach (var client in clients) {
				sb.AppendLine("Найден подозрительный без даты расчетного периода: " + client.Id);
				var ips = Session.Query<StaticIp>().Where(i => client.Endpoints.Contains(i.EndPoint)).ToList();
				if (ips.Count > 0) {
					client.RatedPeriodDate = SystemTime.Now();
					Session.Save(client);
					sb.AppendLine("<br/>Дата расчетного периода проставлена автоматически, так как клиент скорее всего является антенщиком");
				}
				mailhelper.SendText("debug-inforoom@analit.net", "service@analit.net", "Подозрительный клиент в InternetInterface", sb.ToString());
				sb.Clear();
			}
		}
		}
}
