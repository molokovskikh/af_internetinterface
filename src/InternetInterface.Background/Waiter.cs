using System;
using Castle.ActiveRecord;
using Common.Tools;
using Common.Web.Ui.Helpers;
using InternetInterface.Helpers;

namespace InternetInterface.Background
{
	public class Waiter : RepeatableCommand
	{
		public Waiter()
		{
			ActiveRecordStarter.EventListenerComponentRegistrationHook += AuditListener.RemoveAuditListener;
			StandaloneInitializer.Init(typeof(Waiter).Assembly);

			var tasks = new Task[] {
				new DeleteFixIpIfClientLongDisable(),
				new SendNullTariffLawyerPerson(),
				new SendUnknowEndPoint(),
				new SendSmsNotification()
			};
			tasks.Each(t => t.Token = Cancellation);

			Delay = TimeSpan.FromHours(1);
			Action = () => tasks.Each(t => t.Execute());
		}
	}
}