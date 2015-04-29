using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NHibernate;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	[Class(0, Table = "Status", NameType = typeof(Status))]
	public class Status : BaseModel
	{
		public Status()
		{
		}

		public Status(StatusType status)
		{
			ShortName = status.ToString();
			Name = status.GetDescription();
		}

		[Property]
		public virtual string Name { get; set; }

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

		[Description("Получение статуса клиента из БД по перечислению")]
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
				if (ShortName == "BlockedForRepair")
					return StatusType.BlockedForRepair;
				return 0;
			}
		}


		[Bag(0, Table = "StatusCorrelation")]
		[Key(1, Column = "StatusId", NotNull = false)]
		[ManyToMany(2, Column = "AdditionalStatusId", ClassType = typeof(AdditionalStatus))]
		public virtual IList<AdditionalStatus> Additional { get; set; }

		public override string ToString()
		{
			return Name;
		}
	}
}