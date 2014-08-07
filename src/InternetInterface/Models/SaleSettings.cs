using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.Components.Validator;

namespace InternetInterface.Models
{
	[ActiveRecord("SaleSettings", Schema = "internet")]
	public class SaleSettings
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, ValidateNonEmpty("Поле не должно быть пустым"), ValidateInteger("Должно быть введено число")]
		public virtual int PeriodCount { get; set; }

		[Property, ValidateNonEmpty("Поле не должно быть пустым"), ValidateInteger("Должно быть введено число")]
		public virtual int MinSale { get; set; }

		[Property, ValidateNonEmpty("Поле не должно быть пустым"), ValidateInteger("Должно быть введено число")]
		public virtual int MaxSale { get; set; }

		[Property, ValidateNonEmpty("Поле не должно быть пустым"), ValidateDecimal("Должно быть введено число"), ValidateRange("0.5", "5", errorMessage: "Значение выходит за разрешенный интервал")]
		public virtual decimal SaleStep { get; set; }

		[Property, Description("Количество дней через которое сбрасывается статус \"Заблокирован - Восстановление работы\"")]
		public virtual int DaysForRepair { get; set; }

		[Property, Description("Количество дней в год когда услуга добровольная блокировка бесплатная")]
		public virtual int FreeDaysVoluntaryBlocking { get; set; }
	}
}