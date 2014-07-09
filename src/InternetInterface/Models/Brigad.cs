using System;
using System.Collections.Generic;
using System.ComponentModel;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;
using Castle.Components.Validator;
using Common.Web.Ui.ActiveRecordExtentions;
using InternetInterface.Models.Universal;
using System.Linq;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Models
{
	[ActiveRecord("ConnectBrigads", Schema = "Internet", Lazy = true)]
	public class Brigad
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

		[Property, ValidateNonEmpty("Введите имя бригады"), Description("Название")]
		public virtual string Name { get; set; }

		[Property, Description("Отключен")]
		public virtual bool IsDisabled { get; set; }

		[BelongsTo("RegionId"), Description("Регион")]
		public virtual RegionHouse Region { get; set; }

		[HasMany(ColumnKey = "Brigad", Lazy = true)]
		public virtual IList<ConnectGraph> Graphs { get; set; }

		public virtual Client GetOneGraph(int intervalNum, DateTime selectDate)
		{
			return Graphs.Where(c => c.Day.Date == selectDate.Date && c.IntervalId == (uint)intervalNum)
				.Select(g => g.Client)
				.FirstOrDefault();
		}

		public static List<Brigad> All()
		{
			return ArHelper.WithSession(s => All(s));
		}

		public static List<Brigad> All(ISession session, RegionHouse region = null)
		{
			var query = session.Query<Brigad>().Where(b => !b.IsDisabled);
			if (region != null)
				query = query.Where(b => b.Region == region || b.Region == null);
			return query.OrderBy(b => b.Name).ToList();
		}
	}
}