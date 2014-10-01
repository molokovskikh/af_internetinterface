using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web;
using Common.Web.Ui.Helpers;
using InternetInterface.Helpers;
using InternetInterface.Models;

namespace InternetInterface.Mails
{
	public class LawyerUserWriteOffNotice : ILogInterface
	{
		public void Log(object entity)
		{
			var writeOff = (UserWriteOff)entity;
			if (writeOff.Client.IsPhysical())
				return;
			Mailer mailer;
			if (writeOff.Sender != null)
				mailer = new Mailer(writeOff.Sender);
			else
				mailer = new Mailer();
			var messageText = new StringBuilder();
			messageText.AppendLine("Зарегистрировано разовое списание для Юр.Лица.");
			messageText.AppendFormat("Клиент: {0} - {1} ({2})\r\n", writeOff.Client.Id.ToString("00000"),
				writeOff.Client.LawyerPerson.Name, writeOff.Client.Name);
			var registrator = writeOff.Registrator != null ? writeOff.Registrator.Name : string.Empty;
			messageText.AppendFormat("Списание: Сумма - {0} \r\nКомментарий: {1} \r\nОператор: {2}",
				writeOff.Sum.ToString("0.00"), writeOff.Comment, registrator);

			var str = ConfigurationManager.AppSettings["WriteOffNotificationMail"];
			if(str == null)
				throw new Exception("Параметр приложения WriteOffNotificationMail должен быть задан в config");
			var emails = str.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			mailer.SendText("internet@ivrn.net", emails,"Списание для Юр.Лица.", messageText.ToString());
		}
	}
}