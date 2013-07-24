using System;
using System.Collections.Generic;
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
			to = "kvasovtest@analit.net";
#endif
			mailer.SendText("internet@ivrn.net", to, subject, message);
		}
	}
}