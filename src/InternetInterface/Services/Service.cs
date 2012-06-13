using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using InternetInterface.Models;
using InternetInterface.Models.Universal;

namespace InternetInterface.Services
{
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

		[Property]
		public virtual bool InterfaceControl { get; set; }

		//говорит о том что нужно вызывать GetPrice, даже если активирована блокирующая услуга
		//"добровольная блокировка"
		public virtual bool ProcessEvenInBlock
		{
			get
			{
				return false;
			}
		}

		public virtual bool SupportUserActivation
		{
			get
			{
				return false;
			}
		}

		public static Service GetByType(Type type)
		{
			return (Service)ActiveRecordMediator.FindFirst(type);
		}

		public static Service Type<T>() where T : Service
		{
			return ActiveRecordMediator<T>.FindFirst();
		}

		public virtual void Activate(ClientService assignedService)
		{
			if (SupportUserActivation
				&& assignedService.ActivatedByUser
				&& !assignedService.Client.Disabled)
				assignedService.Activated = true;
			assignedService.Activated = true;
		}

		public virtual bool Deactivate(ClientService assignedService)
		{
			if (SupportUserActivation
				&& (!assignedService.ActivatedByUser
				|| assignedService.Client.Disabled))
				assignedService.Activated = false;
			return false;
		}

		public virtual void CompulsoryDeactivate(ClientService CService)
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

		public virtual decimal GetPrice(ClientService assignedService)
		{
			return Price;
		}

		public virtual bool CanActivate(Client client)
		{
			return true;
		}

		public virtual bool CanActivate(ClientService cService)
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
				var cs = client.ClientServices.FirstOrDefault(c => c.Service == this && c.Activated);
				if (cs != null)
					return true;
			}
			return false;
		}

		public virtual void WriteOff(ClientService assignedService)
		{}
	}
}