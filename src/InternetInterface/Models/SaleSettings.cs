using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.Components.Validator;

namespace InternetInterface.Models
{
	[ActiveRecord("SaleSettings", Schema = "internet")]
	public class SaleSettings : ActiveRecordLinqBase<SaleSettings>
	{
		[PrimaryKey]
		public uint Id { get; set; }

		[Property, ValidateNonEmpty("Поле не должно быть пустым"), ValidateInteger("Должнно быть введено число")]
		public int PeriodCount { get; set; }

		[Property, ValidateNonEmpty("Поле не должно быть пустым"), ValidateInteger("Должнно быть введено число")]
		public int MinSale { get; set; }

		[Property, ValidateNonEmpty("Поле не должно быть пустым"), ValidateInteger("Должнно быть введено число")]
		public int MaxSale { get; set; }

		[Property, ValidateNonEmpty("Поле не должно быть пустым"), ValidateDecimal("Должнно быть введено число"), ValidateRange("0.5", "5", errorMessage: "Значение выходит за разрешенный интервал")]
		public decimal SaleStep { get; set; }
	}
}