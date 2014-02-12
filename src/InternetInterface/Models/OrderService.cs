using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using Common.Tools;
using Common.Tools.Calendar;
using Common.Web.Ui.Models.Audit;
using Common.Web.Ui.MonoRailExtentions;
using Common.Web.Ui.NHibernateExtentions;
using InternetInterface.Helpers;
using NPOI.SS.Formula.Functions;

namespace InternetInterface.Models
{
	/// <summary>
	/// Услуга
	/// </summary>
	[ActiveRecord("OrderServices", Schema = "Internet", Lazy = true), Auditable, LogDelete(typeof(LogDeleteOrderService)), LogInsert(typeof(LogInsertOrderService))]
	public class OrderService
	{
		public OrderService()
		{
		}

		public OrderService(Order order, decimal cost, bool isPeriodic)
		{
			Order = order;
			IsPeriodic = isPeriodic;
			Cost = cost;
			if (IsPeriodic)
				Description = "Доступ к сети Интернет";
			else
				Description = "Подключение";
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, Description("Описание"), Auditable("Описание"), SendEmailAboutOrder]
		public virtual string Description { get; set; }

		[Property, Description("Стоимость"), Auditable("Стоимость услуги"), ValidateDecimal("Ошибка ввода суммы"), ValidateGreaterThanZero, SendEmailAboutOrder]
		public virtual decimal Cost { get; set; }

		[Property, Description("Услуга периодичная"), Auditable("Периодичность"), SendEmailAboutOrder]
		public virtual bool IsPeriodic { get; set; }

		[BelongsTo(Column = "OrderId")]
		public virtual Order Order { get; set; }

		public virtual decimal SumToWriteOff
		{
			get
			{
				if (Order == null
					|| Order.Client == null
					|| Order.Client.LawyerPerson == null)
					return 0;
				if (Order.OrderStatus == OrderStatus.New)
					return 0;
				if (Order.IsDeactivated)
					return 0;
				if (!IsPeriodic) {
					if (Order.IsActivated)
						return 0;
					else
						return Cost;
				}
				var currentPeriod = Order.Client.LawyerPerson.PeriodEnd;
				if (currentPeriod == DateTime.MinValue)
					return 0;
				var periodBegin = currentPeriod.FirstDayOfMonth();
				var periodEnd = currentPeriod.LastDayOfMonth();
				var actualPeriodBegin = Order.BeginDate.Value > periodBegin
					? Order.BeginDate.Value
					: periodBegin;
				var actualPeriodEnd = Order.EndDate != null && Order.EndDate.Value < periodEnd
					? Order.EndDate.Value
					: periodEnd;
				if (Order.Disabled)
					actualPeriodEnd = SystemTime.Today();
				var factor = (decimal)((actualPeriodEnd - actualPeriodBegin).Days + 1) / (decimal)((periodEnd - periodBegin).Days + 1);
				return Math.Round(factor * Cost, 2);
			}
		}

		public virtual string GetPeriodic()
		{
			if (IsPeriodic) {
				return "Периодичная";
			}
			return "Разовая";
		}

		public override string ToString()
		{
			return string.Format("[{0}] - '{1}'", Id, Description);
		}
	}
}