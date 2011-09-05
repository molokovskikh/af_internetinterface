using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using Castle.ActiveRecord;
using InternetInterface.Models.Universal;

namespace InternetInterface.Models
{
	public class UniqueServiceException : Exception
	{
	}

	[ActiveRecord("ClientServices", Schema = "Internet", Lazy = true)]
	public class ClientService : ValidActiveRecordLinqBase<ClientService>
	{

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

		[Property]
		public virtual bool Diactivated { get; set; }

		public virtual void DeleteFromClient()
		{
			if (Service.CanDelete(this))
			{
				Client.ClientServices.Remove(this);
				base.Delete();
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
				//throw new UniqueServiceException();
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
			//Client.ClientServices
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
	}
}