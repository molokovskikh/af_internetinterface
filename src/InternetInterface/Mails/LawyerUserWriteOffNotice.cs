using System;
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
			mailer.UserWriteOff(writeOff).Send();
		}
	}
}