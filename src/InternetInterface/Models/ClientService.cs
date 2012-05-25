using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using Castle.ActiveRecord;
using InternetInterface.Models.Universal;
using InternetInterface.Services;
using NHibernate;

namespace InternetInterface.Models
{
	[ActiveRecord("ClientServices", Schema = "Internet", Lazy = true)]
	public class ClientService : ValidActiveRecordLinqBase<ClientService>
	{
		public ClientService()
		{}

		public ClientService(Client client, Service service)
		{
			Client = client;
			Service = service;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo]
		public virtual Client Client { get; set; }

		[BelongsTo]
		public virtual Service Service { get; set; }

		[Property]
		public virtual DateTime? BeginWorkDate { get; set; }

		[Property]
		public virtual DateTime? EndWorkDate { get; set; }

		[Property]
		public virtual bool Activated { get; set; }

		[BelongsTo]
		public virtual Partner Activator { get; set; }

		public virtual void DeleteFromClient()
		{
			if (Service.CanDelete(this))
			{
				//сторока ниже не работает, в тестt ServiceFixture.ActiveDeactive хотя должна, какой то бред
				//Client.ClientServices.Remove(this);
				Client.ClientServices.Remove(Client.ClientServices.First(c => c.Id == Id));
				Client.Save();
			}
		}

		public override void Save()
		{
			if (Client.ClientServices != null)
				if (Client.ClientServices.Select(c => c.Service).Contains(Service))
				{
					LogComment = "Невозможно использовать данную услугу";
					return;
				}
			base.Save();
			Client.Refresh();
		}

		public virtual void Activate()
		{
			Service.Activate(this);
		}

		public virtual void Diactivate()
		{
			if (Service.Diactivate(this))
				DeleteFromClient();
		}

		public virtual void CompulsoryDiactivate()
		{
			Service.CompulsoryDiactivate(this);
			DeleteFromClient();
		}

		public virtual decimal GetPrice()
		{
			return Service.GetPrice(this);
		}

		public virtual void PaymentClient()
		{
			Service.PaymentClient(this);
		}

		public virtual void WriteOff()
		{
			Service.WriteOff(this);
		}
	}
}