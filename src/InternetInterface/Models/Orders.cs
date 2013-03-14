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
	[ActiveRecord(Schema = "Internet", Lazy = true)]
	public class Orders
	{
		public Orders()
		{
			BeginDate = SystemTime.Now();
			EndDate = SystemTime.Now();
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, Description("Номер заказа")]
		public virtual uint Number { get; set; }

		[Property]
		public virtual DateTime? BeginDate { get; set; }

		[Property]
		public virtual DateTime? EndDate { get; set; }

		[BelongsTo]
		public virtual ClientEndpoint EndPoint { get; set; }

		[HasMany(ColumnKey = "OrderId", Lazy = true)]
		public virtual IList<OrderService> OrderServices { get; set; }

		[BelongsTo(Column = "ClientId")]
		public virtual Client Client { get; set; }

		[Property]
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

		public static uint GetNextNumber(ISession session)
		{
			var orders = session.Query<Orders>().ToList();
			uint number = 1;
			if(orders.Count > 0)
				number = orders.Max(o => o.Number) + 1;
			return number;
		}
	}
}