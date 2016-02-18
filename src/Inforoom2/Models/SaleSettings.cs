using Common.Tools;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "SaleSettings", NameType = typeof(SaleSettings))]
	public class SaleSettings : BaseModel
	{
		[Property, NotNull(Message = "Поле обязательно для заполнения")]
		public virtual int PeriodCount { get; set; }

		[Property, NotNull(Message = "Поле обязательно для заполнения")]
		public virtual int MinSale { get; set; }

		[Property, NotNull(Message = "Поле обязательно для заполнения")]
		public virtual int MaxSale { get; set; }

		[Property, NotNull(Message = "Поле обязательно для заполнения")]
		public virtual decimal SaleStep { get; set; }

		[Property, NotNull(Message = "Поле обязательно для заполнения")]
		public virtual int DaysForRepair { get; set; }

		[Property, NotNull(Message = "Поле обязательно для заполнения")]
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