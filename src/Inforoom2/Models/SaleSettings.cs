using Common.Tools;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	[Class(0, Table = "SaleSettings", NameType = typeof(SaleSettings))]
	public class SaleSettings : BaseModel
	{
		[Property]
		public virtual int PeriodCount { get; set; }

		[Property]
		public virtual int MinSale { get; set; }

		[Property]
		public virtual int MaxSale { get; set; }

		[Property]
		public virtual decimal SaleStep { get; set; }

		[Property]
		public virtual int DaysForRepair { get; set; }

		[Property]
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