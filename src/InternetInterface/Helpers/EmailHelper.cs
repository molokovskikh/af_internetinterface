using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Web;
using InternetInterface.Models;

namespace InternetInterface.Helpers
{
	public class EmailHelper
	{
		public static void Send(string to, string subject, string message)
		{
			var mailer = new Mailer();
		#if DEBUG
			message = to + "\n" + message;
			to = ConfigurationManager.AppSettings["DebugMail"];
			if(to == null)
				throw new Exception("Параметр приложения DebugMail должен быть задан в config");
		#endif
			mailer.SendText("internet@ivrn.net", to, subject, message);
		}
	}
}