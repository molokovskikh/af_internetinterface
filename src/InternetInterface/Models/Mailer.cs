using Castle.MonoRail.Framework;
using Common.Web.Ui.MonoRailExtentions;

namespace InternetInterface.Models
{
	public static class MailerExtension
	{
		public static Mailer Mailer(this SmartDispatcherController controller)
		{
			return controller.Mailer<Mailer>();
		}
	}

	public class Mailer : BaseMailer
	{
		public void Invoice(Invoice invoice)
		{
			throw new System.NotImplementedException();
		}
	}
}