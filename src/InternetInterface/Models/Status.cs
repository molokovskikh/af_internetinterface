using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using InternetInterface.Models.Universal;

namespace InternetInterface.Models
{
	public enum StatusType
	{
		BlockedAndNoConnected = 1,
		BlockedAndConnected = 3,
		Worked = 5,
		NoWorked = 7,
        VoluntaryBlocking = 9
	};


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
        public virtual string ShortName { get; set; }

	    public virtual bool Visualisible()
		{
			if (Id == (uint)StatusType.BlockedAndConnected)
				return false;
			if (Id == (uint)StatusType.BlockedAndNoConnected)
				return false;
			return true;
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