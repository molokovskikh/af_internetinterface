using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using Castle.ActiveRecord;
using Common.Tools;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models.Universal;

namespace InternetInterface.Models
{
	/*public class ServiceNames
	{
		public static string DebtWork
		{
			get { return "DebtWork"; }
		}

		public static string VoluntaryBlockin
		{
			get { return "VoluntaryBlockin"; }
		}
	}*/

	[ActiveRecord("Services", Schema = "Internet", Lazy = true, DiscriminatorColumn = "Name",
		DiscriminatorType = "String", DiscriminatorValue = "service")]
	public class Service : ValidActiveRecordLinqBase<Service>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string HumanName { get; set; }

		[Property]
		public virtual decimal Price { get; set; }

		[Property]
		public virtual bool BlockingAll { get; set; }

			/*public static T GetByType<T>() where T : class 
		{
			return ActiveRecordMediator<T>.FindFirst();
		}*/

		public static Service GetByType(Type type)
		{
			return (Service)ActiveRecordMediator.FindFirst(type);
		}


		public virtual void Activate(ClientService CService)
		{}

		public virtual bool Diactivate(ClientService CService)
		{
			return false;
		}

		public virtual void CompulsoryDiactivate(ClientService CService)
		{}

		public virtual void EditClient(ClientService CService)
		{}

		public virtual void PaymentClient(ClientService CService)
		{}

		public virtual string GetParameters()
		{
			return string.Empty;
		}

		public virtual bool CanDelete(ClientService CService)
		{
			return true;
		}

		public virtual bool CanBlock(ClientService CService)
		{
			return true;
		}

		public virtual decimal GetPrice(ClientService CService)
		{
			return Price;
		}

		public virtual bool CanActivate(Client client)
		{
			return true;
		}

		public virtual bool CanActivate(ClientService CService)
		{
			return true;
		}

		public virtual bool CanActivateInWeb(Client client)
		{
			return CanActivate(client) && !client.ClientServices.Select(c => c.Service.Id).Contains(Id);
		}


		public virtual bool ActivatedForClient(Client client)
		{
			if (client.ClientServices != null)
			{
				var cs = client.ClientServices.Where(c => c.Service == this && c.Activated && !c.Diactivated).FirstOrDefault();
				if (cs != null)
					return true;
			}
			return false;
		}
	}



	[ActiveRecord(DiscriminatorValue = "DebtWork")]
	public class DebtWork : Service
	{
		public override string GetParameters()
		{
			var builder = new StringBuilder();
			builder.Append("<tr>");
			builder.Append(
				string.Format(
					"<td><label for=\"endDate\"> Конец периода </label><input type=text  name=\"endDate\" id=\"endDate\" value=\"{0}\" class=\"date-pick dp-applied\"></td>",
					DateTime.Now.AddDays(1).ToShortDateString()));
			builder.Append("</tr>");
			return builder.ToString();
		}

		public override bool CanActivate(Client client)
		{
			if (client.PhysicalClient != null)
			{
				var balance = client.PhysicalClient.Balance < 0;
				var conVol =
					!client.ClientServices.Select(c => c.Service).Contains(GetByType(typeof (VoluntaryBlockin)));
				return balance && conVol && client.StartWork();
			}
			return false;
		}

		public override bool CanActivate(ClientService CService)
		{
			var client = CService.Client;
			var payTar = client.PaymentForTariff();
			if (CService.Activator != null)
				payTar = true;
			return payTar && CanActivate(client);
		}

		public override void PaymentClient(ClientService CService)
		{
			if (CService.Client.PhysicalClient.Balance > 0)
				CService.Diactivate();
		}

		public override bool CanBlock(ClientService CService)
		{
			if (CService.EndWorkDate == null)
				return false;
			return CService.EndWorkDate.Value < SystemTime.Now();
		}

		public override bool CanDelete(ClientService CService)
		{
			if (CService.Activator != null)
				return true;

			var lastPayments =
				Payment.Queryable.Where(
					p => p.Client == CService.Client && CService.BeginWorkDate.Value < p.PaidOn).
					ToList().Sum(p => p.Sum);
			var balance = CService.Client.PhysicalClient.Balance;
			if (balance > 0 &&
				balance - lastPayments <= 0)
				return true;
			return false;
		}

		public override void CompulsoryDiactivate(ClientService CService)
		{
			var client = CService.Client;
			client.Disabled = true;
			client.AutoUnblocked = true;
			client.Status = Status.Find((uint)StatusType.NoWorked);
			client.Update();
			CService.Activated = false;
			CService.Diactivated = true;
			CService.Update();
		}

		public override bool Diactivate(ClientService CService)
		{
			if (CService.Activated && CService.EndWorkDate.Value < SystemTime.Now())
			{
				CompulsoryDiactivate(CService);
				return true;
			}
			return false;
		}

		public override void Activate(ClientService CService)
		{
			if ((!CService.Activated && CanActivate(CService)))
			{
				var client = CService.Client;
				client.Disabled = false;
				client.RatedPeriodDate = SystemTime.Now();
				client.Status = Status.Find((uint) StatusType.Worked);
				client.Update();
				CService.Activated = true;
				CService.Update();
			}
		}
	}

	[ActiveRecord(DiscriminatorValue = "VoluntaryBlockin")]
	public class VoluntaryBlockin : Service
	{
		public override string GetParameters()
		{
			var builder = new StringBuilder();
			builder.Append("<tr>");
			builder.Append(
				string.Format(
					"<td><label for=\"startDate\">Начала периода </label><input type=text value=\"{0}\" name=\"startDate\" id=\"startDate\" class=\"date-pick dp-applied\"> </td>",
					DateTime.Now.ToShortDateString()));
			builder.Append(
				string.Format(
					"<td><label for=\"endDate\"> Конец периода </label><input type=text  name=\"endDate\" value=\"{0}\"  id=\"endDate\" class=\"date-pick dp-applied\"></td>",
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
				client.Disabled = true;
				client.AutoUnblocked = false;
				client.DebtDays = 0;
				client.Status = Status.Find((uint)StatusType.VoluntaryBlocking);
				client.Update();
				CService.Activated = true;
				CService.Update();
			}
		}

		public override void CompulsoryDiactivate(ClientService CService)
		{
			var client = CService.Client;
			client.DebtDays = 0;
			client.RatedPeriodDate = DateTime.Now;
			client.Disabled = CService.Client.PhysicalClient.Balance < 0;
			client.AutoUnblocked = true;
			if (CService.Client.PhysicalClient.Balance > 0)
			{
				client.Disabled = false;
				client.Status = Status.Find((uint)StatusType.Worked);
			}
			client.Update();
			CService.Diactivated = true;
			CService.Update();
		}

		public override bool Diactivate(ClientService CService)
		{
			if ((CService.EndWorkDate == null && CService.Client.PhysicalClient.Balance < 0) ||
				(CService.EndWorkDate != null && (SystemTime.Now() > CService.EndWorkDate.Value)))
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

			if ((SystemTime.Now().Date < CService.BeginWorkDate.Value.AddMonths(1).Date))
				return 0;
			return CService.Client.GetInterval()*2m;
		}
	}
}