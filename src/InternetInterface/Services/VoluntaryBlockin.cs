using System;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Common.Tools;
using InternetInterface.Models;

namespace InternetInterface.Services
{
	[ActiveRecord(DiscriminatorValue = "VoluntaryBlockin")]
	public class VoluntaryBlockin : Service
	{
		public override string GetParameters()
		{
			var builder = new StringBuilder();
			builder.Append("<tr>");
			builder.Append(
				string.Format(
					"<td><label for=\"endDate\" id=\"endDateLabel\"> Заблокировать до  </label><input type=text  name=\"endDate\" value=\"{0}\"  id=\"endDate\" class=\"date-pick dp-applied\"></td>",
					DateTime.Now.AddDays(1).ToShortDateString()));
			builder.Append("</tr>");
			return builder.ToString();
		}

		private static bool InternalCanActivate(Client client)
		{
			var result = client.PhysicalClient != null
				&& client.PhysicalClient.Balance >= 0
				&& !client.Disabled
				&& !client.HaveActiveService<DebtWork>()
				&& client.StartWork();
			if (client.FreeBlockDays <= 0) {
				result = result && client.Balance >= 50 + client.GetSumForRegularWriteOff();
			}
			return result;
		}

		public override bool CanActivate(Client client)
		{
			return InternalCanActivate(client) && !client.HaveService<VoluntaryBlockin>();
		}

		//будь бдителен два вызова CanActivate похожи но не идентичные
		//CanActivate(Client client) - происходит если нужно узнать может ли услуга быть активирована
		//CanActivate(ClientService assignedService) - происходит когда услуга активируется
		//разница в проверке дублей когда услуга активируется она уже будет в списке ClientService
		public override bool CanActivate(ClientService assignedService)
		{
			var result = SystemTime.Now() >= assignedService.BeginWorkDate.Value
				&& InternalCanActivate(assignedService.Client)
				&& !assignedService.Client.ClientServices
					.Except(new[] {assignedService})
					.Any(s => s.IsService(assignedService.Service));
			if (assignedService.Client.FreeBlockDays <= 0) {
				result = result && assignedService.Client.Balance >= 50 + assignedService.Client.GetSumForRegularWriteOff();
			} else {
				result = result && assignedService.Client.Balance >= assignedService.Client.GetSumForRegularWriteOff();
			}
			return result;
		}

		public override void Activate(ClientService assignedService)
		{
			if (CanActivate(assignedService) && !assignedService.IsActivated) {
				var client = assignedService.Client;

				client.RatedPeriodDate = DateTime.Now;

				var now = SystemTime.Now();
				//если сегодня у пользователя нет списаний с соответствующего типа
				if (assignedService.BeginWorkDate.Value.Date == now.Date) {
					if (!client.UserWriteOffs.Any(s => s.Date.Date == now.Date && s.Type == UserWriteOffType.ClientVoluntaryBlock)) {
						//производится списание абон платы
						var comment = string.Format("Абонентская плата за {0} из-за добровольной блокировки клиента",
							now.ToShortDateString());
						var writeOff = new UserWriteOff {
							Client = client,
							Type = UserWriteOffType.ClientVoluntaryBlock,
							Date = now,
							Sum = client.GetSumForRegularWriteOff(),
							Comment = comment,
							Ignore = true
						};
						ActiveRecordMediator.Save(writeOff);
					}
				}

				client.SetStatus(Status.Find((uint)StatusType.VoluntaryBlocking));
				client.Update();

				assignedService.IsActivated = true;
				assignedService.EndWorkDate = assignedService.EndWorkDate.Value.Date;
				ActiveRecordMediator.Save(assignedService);

				if (client.FreeBlockDays <= 0) {
					var comment = string.Format("Платеж за активацию услуги добровольная блокировка с {0} по {1}",
						assignedService.BeginWorkDate.Value.ToShortDateString(), assignedService.EndWorkDate.Value.ToShortDateString());
					var writeOff = new UserWriteOff {
						Client = client,
						Sum = 50m,
						Date = DateTime.Now,
						Comment = comment
					};
					ActiveRecordMediator.Save(writeOff);
				}
			}
		}

		public override void ForceDeactivate(ClientService assignedService)
		{
			var client = assignedService.Client;
			client.DebtDays = 0;
			client.ShowBalanceWarningPage = client.PhysicalClient.Balance < 0;
			client.SetStatus(client.Balance > 0
				? Status.Find((uint)StatusType.Worked)
				: Status.Find((uint)StatusType.NoWorked));
			client.RatedPeriodDate = DateTime.Now;
			client.CreareAppeal("Услуга 'Добровольная блокировка' отключена.");
			assignedService.IsActivated = false;
			ActiveRecordMediator.Update(assignedService);
			ActiveRecordMediator.Update(client);
		}

		public override bool CanDeactivate(ClientService assignedService)
		{
			return assignedService.Client.PhysicalClient.Balance < 0
				|| (assignedService.EndWorkDate != null && SystemTime.Now().Date >= assignedService.EndWorkDate.Value);
		}

		public override decimal GetPrice(ClientService assignedService)
		{
			if (assignedService.BeginWorkDate == null || assignedService.BeginWorkDate.Value.Date == SystemTime.Now().Date)
				return 0;

			if (assignedService.Client.RatedPeriodDate == null)
				return 0;

			if (assignedService.Client.FreeBlockDays > 0)
				return 0;

			if (assignedService.Client.PhysicalClient.Balance < 0)
				return 0;

			return assignedService.Client.GetInterval() * 3m;
		}

		public override void WriteOff(ClientService assignedService)
		{
			var client = assignedService.Client;
			if (assignedService.IsActivated
				&& client.FreeBlockDays > 0
				&& assignedService.BeginWorkDate.Value.Date != SystemTime.Today()
				&& assignedService.EndWorkDate.Value.Date != SystemTime.Today()) {
				client.FreeBlockDays--;
				client.Update();
			}
			if (assignedService.IsActivated && client.FreeBlockDays == 0) {
				var userWriteOffs = client.UserWriteOffs.Where(s=>!s.Ignore).ToList()
					.Where(uw => uw.Comment.Contains("Платеж за активацию услуги добровольная блокировка")).ToList();
				var lastUserWriteOff = userWriteOffs.OrderByDescending(uw => uw.Date).ToList().FirstOrDefault();
				if (lastUserWriteOff == null || lastUserWriteOff.Date < assignedService.BeginWorkDate) {
					var writeOffComment = "Разовое списание за пользование услугой добровольная блокировка";
					var writeOffs = client.WriteOffs.ToList()
						.Where(uw => uw.Comment != null && uw.Comment.Contains(writeOffComment)).ToList();
					var lastWriteOff = writeOffs.OrderByDescending(w => w.WriteOffDate).ToList().FirstOrDefault();
					if (lastWriteOff == null || lastWriteOff.WriteOffDate < assignedService.BeginWorkDate) {
						var newWriteOff = client.PhysicalClient.WriteOff(50m);
						newWriteOff.Comment = writeOffComment;
						newWriteOff.Save();
					}
				}
			}
		}
	}
}
