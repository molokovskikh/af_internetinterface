using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Common.Tools;
using Common.Web.Ui.Models.Audit;
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
	[ActiveRecord(Schema = "Internet", Table = "Orders", Lazy = true), Auditable]
	public class Order
	{
		public Order()
		{
			BeginDate = SystemTime.Now();
			EndDate = SystemTime.Now();
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, Description("Номер заказа"), Auditable("Номер заказа")]
		public virtual uint Number { get; set; }

		[Property, Auditable("Дата активации заказа")]
		public virtual DateTime? BeginDate { get; set; }

		[Property, Auditable("Дата окончания заказа")]
		public virtual DateTime? EndDate { get; set; }

		[BelongsTo]
		public virtual ClientEndpoint EndPoint { get; set; }

		[HasMany(ColumnKey = "OrderId", Lazy = true, Cascade = ManyRelationCascadeEnum.SaveUpdate)]
		public virtual IList<OrderService> OrderServices { get; set; }

		[BelongsTo(Column = "ClientId")]
		public virtual Client Client { get; set; }

		[Property, Auditable("Деактивирован")]
		public virtual bool Disabled { get; set; }

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
					if (EndDate == null || EndDate.Value.Date >= SystemTime.Now().Date) {
						return OrderStatus.Enabled;
					}
					return OrderStatus.Disabled;
				}
				return OrderStatus.New;
			}
		}

		public virtual bool CanEdit()
		{
			var nextMonth = BeginDate.Value.AddMonths(1).Month;
			var controlDate = new DateTime(BeginDate.Value.Year, nextMonth, 5);
			if (SystemTime.Now().Date <= controlDate)
				return true;
			return false;
		}

		public static uint GetNextNumber(ISession session, uint clientId)
		{
			var order = 0;
			var client = session.Get<Client>(clientId);
			if (client != null && client.Orders.Count > 0)
				order = client.Orders.Max(o => (int)o.Number);
			uint number = 1;
			if (order > 0)
				number = (uint)order + 1;
			return number;
		}
	}
}