using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Common.Tools;
using NHibernate;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models.Services
{
	[Subclass(0, ExtendsType = typeof(Service), DiscriminatorValue = "VoluntaryBlockin")]
	public class BlockAccountService : Service
	{
		public override bool CanActivate(ClientService assignedService)
		{
			var blockingTime = assignedService.EndDate - assignedService.BeginDate;
			if (blockingTime == null) {
				assignedService.CannotActivateMsg = "Невозможно определить срок добровольной блокировки. Активация отменена!";
				return false;
			}

			var client = assignedService.Client;
			var paidDays = (int)Math.Ceiling(blockingTime.Value.TotalDays) - client.FreeBlockDays;
			if (paidDays > 0) {
				var servicePay = 50m + (paidDays * 3m);
				if (servicePay > client.Balance) {
					assignedService.CannotActivateMsg = string.Format(
						"Недостаточно средств на счете для добровольной блокировки до {0}.</br>",
						assignedService.EndDate.Value.ToShortDateString());
					string plusMsg;
					if (client.FreeBlockDays > 0) {
						plusMsg = string.Format("Вы можете активировать услугу на бесплатные дни "
							+ "либо пополнить баланс и уже затем перейти к ее активации!");
					}
					else
						plusMsg = "Пополните баланс, чтобы затем перейти к активации услуги!";
					assignedService.CannotActivateMsg += plusMsg;
					return false;
				}
			}
			return true;
		}

		public override void Activate(ClientService assignedService, ISession session)
		{
			var client = assignedService.Client;
			client.RatedPeriodDate = SystemTime.Now();

			var now = SystemTime.Now();
			if (!client.PaidDay && now.Hour < 22 && assignedService.BeginDate.Value.Date == now.Date) {
				client.PaidDay = true;
				var comment = string.Format("Абонентская плата за {0} из-за добровольной блокировки клиента", now.ToShortDateString());
				var writeOff = new UserWriteOff {
					Client = client,
					Date = now,
					Sum = client.GetSumForRegularWriteOff(),
					Comment = comment
				};
				session.Save(writeOff);
			}

			client.SetStatus(Status.Get(StatusType.VoluntaryBlocking, session));
			session.Update(client);

			assignedService.IsActivated = true;
			assignedService.EndDate = assignedService.EndDate.Value.Date;
			session.Save(assignedService);

			if (client.FreeBlockDays <= 0) {
				var comment = string.Format("Платеж за активацию услуги добровольная блокировка с {0} по {1}",
					assignedService.BeginDate.Value.ToShortDateString(), assignedService.EndDate.Value.ToShortDateString());
				var writeOff = new UserWriteOff {
					Client = client,
					Sum = 50m,
					Date = now,
					Comment = comment
				};
				session.Save(writeOff);
			}
		}

		public override void Deactivate(ClientService assignedService, ISession session)
		{
			var client = assignedService.Client;
			client.DebtDays = 0;
			client.ShowBalanceWarningPage = client.PhysicalClient.Balance < 0;
			client.SetStatus(client.Balance > 0
				? Status.Get(StatusType.Worked, session)
				: Status.Get(StatusType.NoWorked, session));
			client.RatedPeriodDate = SystemTime.Now();

			if (!client.PaidDay && assignedService.IsActivated) {
				assignedService.IsActivated = false;

				var clientServicesToReActivate = client.ClientServices.Where(s => s.Service.Id != Id);
				foreach (
					var clientService in
						clientServicesToReActivate.Where(clientService => clientService.Service.IsActivableFor(client))) clientService.Service.Activate(clientService, session);

				var sum = client.GetSumForRegularWriteOff();
				if (sum > 0) {
					client.PaidDay = true;
					var comment = string.Format("Абонентская плата за {0} из-за добровольной разблокировки клиента",
						SystemTime.Now().ToShortDateString());

					session.Save(new UserWriteOff(client, sum, comment));
				}
			}

			client.ClientServices.Remove(assignedService);
			assignedService.IsActivated = false;
		}

		public override bool CanDeactivate(ClientService assignedService)
		{
			return assignedService.Client.PhysicalClient.Balance < 0
				|| (assignedService.EndDate != null && SystemTime.Now().Date >= assignedService.EndDate.Value);
		}

		public override bool IsActivableFor(Client client)
		{
			return client != null
			       && !client.Disabled
			       && client.IsWorkStarted()
			       && client.PhysicalClient.Balance >= 0
			       && !client.HasActiveService<DeferredPayment>()
			       && !client.HasActiveService<BlockAccountService>();
		}

		[DataType(DataType.Date)]
		[DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
		public virtual DateTime BlockingEndDate { get; set; }
	}
}