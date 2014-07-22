using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Common.Tools;
using Common.Web.Ui.Models.Audit;
using InternetInterface.Helpers;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Models
{
	public enum OrderStatus
	{
		[Description("Новый")] New = 0,
		[Description("Активный")] Enabled = 1,
		[Description("Неактивный")] Disabled = 2
	}
	/// <summary>
	/// Заказ
	/// </summary>
	[ActiveRecord(Schema = "Internet", Table = "Orders", Lazy = true), Auditable, LogInsert(typeof(LogOrderInsert))]
	public class Order
	{
		public Order(LawyerPerson client)
			: this()
		{
			Client = client.client;
		}

		public Order()
		{
			Number = 1;
			BeginDate = SystemTime.Now();
			OrderServices = new List<OrderService>();
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, Description("Номер заказа"), Auditable("Номер заказа"), SendEmailAboutOrder]
		public virtual uint Number { get; set; }

		[Property, Auditable("Дата активации заказа"), Description("Дата активации заказа"), SendEmailAboutOrder]
		public virtual DateTime? BeginDate { get; set; }

		[Property, Auditable("Дата окончания заказа"), Description("Дата окончания заказа"), SendEmailAboutOrder]
		public virtual DateTime? EndDate { get; set; }

		[BelongsTo]
		public virtual ClientEndpoint EndPoint { get; set; }

		[HasMany(ColumnKey = "OrderId", Lazy = true, Cascade = ManyRelationCascadeEnum.SaveUpdate)]
		public virtual IList<OrderService> OrderServices { get; set; }

		[BelongsTo(Column = "ClientId")]
		public virtual Client Client { get; set; }

		//состояние заказа, выставленное пользователем
		[Property]
		public virtual bool Disabled { get; set; }

		//обработана ли активация заказ, устанавливает биллинг нужен для учета списаний не периодических услуг
		[Property]
		public virtual bool IsActivated { get; set; }

		//обработана ли деактивация заказа, устанавливает биллинг нужен для учета списаний периодических услуг
		[Property]
		public virtual bool IsDeactivated { get; set; }

		/// <summary>
		/// Статус заказа
		/// </summary>
		public virtual OrderStatus OrderStatus
		{
			get
			{
				if(Disabled)
					return OrderStatus.Disabled;
				if (BeginDate.Value.Date <= SystemTime.Now().Date) {
					if (EndDate == null || EndDate.Value.Date > SystemTime.Now().Date) {
						return OrderStatus.Enabled;
					}
					return OrderStatus.Disabled;
				}
				return OrderStatus.New;
			}
		}

		public virtual string Description
		{
			get
			{
				return string.Format("{0}, услуги {1}", Id, OrderServices.Implode());
			}
		}

		public virtual bool CanEdit()
		{
			return !IsActivated;
		}

		public static uint GetNextNumber(ISession session, uint clientId)
		{
			var client = session.Get<Client>(clientId);
			if (client != null && client.Orders.Count > 0)
				return client.Orders.Max(o => o.Number) + 1;
			return 1;
		}
	}
}