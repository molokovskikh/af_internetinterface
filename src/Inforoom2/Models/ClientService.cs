using System;
using Common.Tools;
using Inforoom2.Models.Services;
using NHibernate;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	[Class(0, Table = "ClientServices", NameType = typeof(ClientService))]
	public class ClientService : BaseModel
	{
		[ManyToOne(Column = "client", NotNull = true)]
		public virtual Client Client { get; set; }

		[ManyToOne(Column = "service", NotNull = true)]
		public virtual Service Service { get; set; }

		[Property(Column = "BeginWorkDate")]
		public virtual DateTime? BeginDate { get; set; }

		[Property(Column = "EndWorkDate")]
		public virtual DateTime? EndDate { get; set; }

		[Property(Column = "Activated")]
		public virtual bool IsActivated { get; set; }

		[Property(Column = "Diactivated")]
		public virtual bool IsDeactivated { get; set; }

		[Property]
		public virtual bool ActivatedByUser { get; set; }

		[Property]
		public virtual bool IsFree { get; set; }

		/// <summary>
		/// Текстовое сообщение о причине невозможности активировать данную услугу
		/// </summary>
		public virtual string CannotActivateMsg { get; set; }

		/// <summary>
		/// Активировать услугу для клиента currentClient
		/// </summary>
		public virtual string ActivateFor(Client currentClient, ISession session)
		{
			string message;
			if (Service.CanActivate(this)) {
				Service.Activate(this, session);
				IsActivated = true;                       // IsActivated внутри Service.Activate() почему-то не срабатывает
				currentClient.ClientServices.Add(this);   // Важно добавлять услугу после активации, чтобы она не влияла на списания
				currentClient.IsNeedRecofiguration = Service.GetType() == typeof (DeferredPayment);

				message = string.Format("Услуга \"{0}\" активирована на период с {1} по {2}",
					Service.Name,
					BeginDate != null ? BeginDate.Value.ToShortDateString() : SystemTime.Now().ToShortDateString(),
					EndDate != null ? EndDate.Value.ToShortDateString() : string.Empty);
			}
			else {
				message = CannotActivateMsg;
			}
			return message;
		}

		/// <summary>
		/// Деактивировать услугу для клиента currentClient
		/// </summary>
		public virtual string DeActivateFor(Client currentClient, ISession session)
		{
			Service.Deactivate(this, session);
			currentClient.IsNeedRecofiguration = Service.GetType() == typeof(BlockAccountService);
			return string.Format("Услуга \"{0}\" деактивирована", Service.Name);
		}

		public virtual bool IsService(Service service)
		{
			return NHibernateUtil.GetClass(Service) == NHibernateUtil.GetClass(service);
		}

		public virtual decimal GetPrice()
		{
			if (IsFree)
				return 0;
			// GetPrice() возвращает цену, отличную от цены услуги, если у конкретной услуги эта ф-ция перегружена
			return Service.GetPrice(this);
		}
	}
}
