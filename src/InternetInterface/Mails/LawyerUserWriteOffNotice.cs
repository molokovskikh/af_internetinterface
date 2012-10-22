﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Common.Web.Ui.Helpers;
using InternetInterface.Models;

namespace InternetInterface.Mails
{
	public class LawyerUserWriteOffNotice
	{
		public static void Send(UserWriteOff writeOff)
		{
			Mailer mailer;
			if (writeOff.Sender != null)
				mailer = new Mailer(writeOff.Sender);
			else
				mailer = new Mailer();
			var messageText = new StringBuilder();
			messageText.AppendLine("Зарегистрировано разовое списание для Юр.Лица.");
			messageText.AppendFormat("Клиент: {0} - {1} ({2})\r\n", writeOff.Client.Id.ToString("00000"), writeOff.Client.LawyerPerson.Name, writeOff.Client.Name);
			var registrator = writeOff.Registrator != null ? writeOff.Registrator.Name : string.Empty;
			messageText.AppendFormat("Списание: Сумма - {0} \r\nКомментарий: {1} \r\nОператор: {2}", writeOff.Sum.ToString("0.00"), writeOff.Comment, registrator);
			mailer.SendText("internet@ivrn.net", "InternetBilling@analit.net", "Списание для Юр.Лица.", messageText.ToString());
		}
	}
}