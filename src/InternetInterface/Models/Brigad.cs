using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;
using Castle.Components.Validator;
using InternetInterface.Models.Universal;

namespace InternetInterface.Models
{
	[ActiveRecord("ConnectBrigads", Schema = "Internet", Lazy = true)]
	public class Brigad : ValidActiveRecordLinqBase<Brigad>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, ValidateNonEmpty("Введите имя бригады")]
		public virtual string Name { get; set; }

		[HasMany(ColumnKey = "Brigad")]
		public virtual IList<ConnectGraph> Graphs { get; set; }
	}

}
