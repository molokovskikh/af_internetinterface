using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Web;

namespace InternetInterface.Helpers
{
	public class EmailHelper
	{
		public static void Send(string to, string subject, string message)
		{
			var mailer = new SmtpClient("box.analit.net");
#if DEBUG
			to = "kvasovtest@analit.net";
#endif
			mailer.Send("internet@ivrn.net", to, subject, message);
		}
	}
}