using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Castle.ActiveRecord;
using Common.Tools;
using InternetInterface.Models;

namespace InternetInterface.Services
{
	/*Если раскоментировать строчку, будет отображать ещё и дату активации*/
	[ActiveRecord(DiscriminatorValue = "VoluntaryBlockin")]
	public class VoluntaryBlockin : Service
	{
		public override string GetParameters()
		{
			var builder = new StringBuilder();
			builder.Append("<tr>");
			/*builder.Append(
				string.Format(
					"<td><label for=\"startDate\" >Активировать с </label><input type=text value=\"{0}\" name=\"startDate\" id=\"startDate\" class=\"date-pick dp-applied\"> </td>",
					DateTime.Now.ToShortDateString()));*/
			builder.Append(
				string.Format(
					"<td><label for=\"endDate\" id=\"endDateLabel\"> Заблокировать до  </label><input type=text  name=\"endDate\" value=\"{0}\"  id=\"endDate\" class=\"date-pick dp-applied\"></td>",
					DateTime.Now.AddDays(1).ToShortDateString()));
			builder.Append("</tr>");
			return builder.ToString();
		}

		public override bool CanActivate(Client client)
		{
			if (client.PhysicalClient != null)
			{
				var balance = client.PhysicalClient.Balance >= 0;
				var debtWork = !client.ClientServices.Select(c => c.Service).Contains(GetByType(typeof (DebtWork)));
				return balance && debtWork && client.StartWork();
			}
			return false;
		}

		public override bool CanActivate(ClientService CService)
		{
			var begin = SystemTime.Now() > CService.BeginWorkDate.Value;
			return  begin && CanActivate(CService.Client);
		}

		public override void Activate(ClientService CService)
		{
			if (CanActivate(CService) && !CService.Activated)
			{
				var client = CService.Client;

				client.RatedPeriodDate = DateTime.Now;

				//Это должно быть на этом месте, иначе возможно списывать неправильную сумму
				var now = SystemTime.Now();
				if (!client.PaidDay && now.Hour < 22 && CService.BeginWorkDate.Value.Date == now.Date) {
					client.PaidDay = true;
					var toDt = client.GetInterval();
					var price = client.GetPrice();
					new UserWriteOff {
						Client = client,
						Date = DateTime.Now,
						Sum = price/toDt,
						Comment = string.Format("Абоненская плата за {0} из-за добровольной блокировки клиента",
							DateTime.Now.ToShortDateString())
					}.Save();
				}

				client.Disabled = true;

				client.ShowBalanceWarningPage = true;

				client.AutoUnblocked = false;
				client.DebtDays = 0;
				client.Status = Status.Find((uint)StatusType.VoluntaryBlocking);
				client.Update();
				CService.Activated = true;
				CService.Diactivated = false;
				var evd = CService.EndWorkDate.Value;
				CService.EndWorkDate = new DateTime(evd.Year, evd.Month, evd.Day);
				CService.Update();

				if (client.FreeBlockDays <= 0)
				new UserWriteOff {
					Client = client,
					Sum = 50m,
					Date = DateTime.Now,
					Comment =
						string.Format("Платеж за активацию услуги добровольная блокировка с {0} по {1}",
						CService.BeginWorkDate.Value.ToShortDateString(), CService.EndWorkDate.Value.ToShortDateString())
				}.Save();
			}
		}

		public override void CompulsoryDiactivate(ClientService CService)
		{
			var client = CService.Client;
			client.DebtDays = 0;
			client.RatedPeriodDate = DateTime.Now;
			client.Disabled = CService.Client.PhysicalClient.Balance < 0;

			client.ShowBalanceWarningPage = CService.Client.PhysicalClient.Balance < 0;


			client.AutoUnblocked = true;
			if (CService.Client.PhysicalClient.Balance > 0)
			{
				client.Disabled = false;
				client.Status = Status.Find((uint)StatusType.Worked);
			}

			if (!client.PaidDay && CService.Activated) {
				CService.Activated = false;
				var toDt = client.GetInterval();
				var price = client.GetPrice();
				if (price / toDt > 0) {
					client.PaidDay = true;
					new UserWriteOff {
						Client = client,
						Date = DateTime.Now,
						Sum = price/toDt,
						Comment =
							string.Format("Абоненская плата за {0} из-за добровольной разблокировки клиента", DateTime.Now.ToShortDateString())
					}.Save();
				}
			}

			CService.Activated = false;
			CService.Diactivated = true;
			CService.Update();

			client.Update();
		}

		public override bool Diactivate(ClientService CService)
		{
			if ((CService.EndWorkDate == null && CService.Client.PhysicalClient.Balance < 0) ||
				(CService.EndWorkDate != null && (SystemTime.Now().Date >= CService.EndWorkDate.Value)))
			{
				CompulsoryDiactivate(CService);
				return true;
			}
			return false;
		}

		public override void PaymentClient(ClientService CService)
		{
			CompulsoryDiactivate(CService);
		}

		//Если раскоментировать этот кусочек, будет введено ограничение - использовать услугу можно будет только после истичения 45 дней с момента последней активации.
		/*public override bool CanDelete(ClientService CService)
		{
			if (CService.EndWorkDate == null)
				return true;
			return (SystemTime.Now().Date - CService.EndWorkDate.Value.Date).Days > 45;
		}*/

		public override decimal GetPrice(ClientService CService)
		{
			if (CService.BeginWorkDate == null)
				return 0;

			if (CService.Client.FreeBlockDays > 0)
				return 0;
			return CService.Client.GetInterval()*3m;
		}

		public override void WriteOff(ClientService cService)
		{
			var client = cService.Client;
			if (cService.Activated && client.FreeBlockDays > 0 &&
				cService.BeginWorkDate.Value.Date != SystemTime.Now().Date &&
				cService.EndWorkDate.Value.Date != SystemTime.Now().Date) {
				client.FreeBlockDays --;
				client.Update();
			}
		}
	}
}