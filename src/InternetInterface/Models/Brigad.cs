using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;
using Castle.Components.Validator;
using Common.Web.Ui.ActiveRecordExtentions;
using InternetInterface.Models.Universal;
using System.Linq;

namespace InternetInterface.Models
{
	[ActiveRecord("ConnectBrigads", Schema = "Internet", Lazy = true)]
	public class Brigad : ValidActiveRecordLinqBase<Brigad>
	{
		public Brigad()
		{
		}

		public Brigad(string name)
		{
			Name = name;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, ValidateNonEmpty("Введите имя бригады")]
		public virtual string Name { get; set; }

		[HasMany(ColumnKey = "Brigad", Lazy = true)]
		public virtual IList<ConnectGraph> Graphs { get; set; }

		public virtual Client GetOneGraph(int intervalNum, DateTime selectDate)
		{
			var graphs =
				ConnectGraph.Queryable.Where(c => c.Brigad == this && c.Day.Date == selectDate.Date && c.IntervalId == (uint)intervalNum).ToList();
			return graphs.Count != 0 ? graphs.First().Client : null;
		}

		public static List<Brigad> All()
		{
			return (List<Brigad>)ArHelper.WithSession(s => s.QueryOver<Brigad>().List());
		}
	}
}