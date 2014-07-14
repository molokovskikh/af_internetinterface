using System.ComponentModel;
using System.Linq;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using InternetInterface.Models.Universal;
using NHibernate;

namespace InternetInterface.Models
{
	public enum StatusType
	{
		[Description("Зарегистрирован")]
		BlockedAndNoConnected = 1,
		[Description("Не подключен")]
		BlockedAndConnected = 3,
		[Description("Подключен")]
		Worked = 5,
		[Description("Заблокирован")]
		NoWorked = 7,
		[Description("Добровольная блокировка")]
		VoluntaryBlocking = 9,
		[Description("Расторгнут")]
		Dissolved = 10
	}

	[ActiveRecord("Status", Schema = "internet", Lazy = true)]
	public class Status : ValidActiveRecordLinqBase<Status>
	{
		[PrimaryKey(PrimaryKeyType.Assigned)]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[Property]
		public virtual bool Blocked { get; set; }

		[Property]
		public virtual bool Connected { get; set; }

		[Property]
		public virtual bool ManualSet { get; set; }

		[Property]
		public virtual string ShortName { get; set; }

		public virtual bool Visualisible()
		{
			if (Id == (uint)StatusType.BlockedAndConnected)
				return false;
			if (Id == (uint)StatusType.BlockedAndNoConnected)
				return false;
			return true;
		}

		public static Status Get(StatusType type, ISession session)
		{
			return session.QueryOver<Status>().List().FirstOrDefault(status => status.Type == type);
		}

		public virtual StatusType Type
		{
			get
			{
				if (ShortName == "VoluntaryBlocking")
					return StatusType.VoluntaryBlocking;
				if (ShortName == "BlockedAndNoConnected")
					return StatusType.BlockedAndNoConnected;
				if (ShortName == "BlockedAndConnected")
					return StatusType.BlockedAndConnected;
				if (ShortName == "Worked")
					return StatusType.Worked;
				if (ShortName == "NoWorked")
					return StatusType.NoWorked;
				if (ShortName == "Dissolved")
					return StatusType.Dissolved;
				return 0;
			}
		}


		[HasAndBelongsToMany(typeof(AdditionalStatus),
			RelationType.Bag,
			Table = "StatusCorrelation",
			Schema = "internet",
			ColumnKey = "StatusId",
			ColumnRef = "AdditionalStatusId",
			Lazy = true)]
		public virtual IList<AdditionalStatus> Additional { get; set; }

		public override string ToString()
		{
			return Name;
		}
	}
}