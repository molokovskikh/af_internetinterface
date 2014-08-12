using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.Components.Validator;
using Common.Tools;

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

		public static SaleSettings Defaults()
		{
			return new SaleSettings {
				MaxSale = 15,
				MinSale = 3,
				PeriodCount = 3,
				SaleStep = 1,
				FreeDaysVoluntaryBlocking = 28,
				DaysForRepair = 3
			};
		}

		public virtual bool IsRepairExpaired(Client client)
		{
			return client.Status.Type == StatusType.BlockedForRepair
				&& (SystemTime.Now() - client.StatusChangedOn).GetValueOrDefault().TotalDays > DaysForRepair;
		}
	}
}