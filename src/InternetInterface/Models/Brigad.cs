using System;
using System.Collections.Generic;
using System.ComponentModel;
using Boo.Lang.Compiler.TypeSystem;
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
		public static TimeSpan DefaultWorkBegin = new TimeSpan(9, 0, 0);
		public static TimeSpan DefaultWorkEnd = new TimeSpan(18, 0, 0);
		public static TimeSpan DefaultWorkStep = new TimeSpan(0, 30, 0);

		public Brigad()
		{
			WorkBegin = DefaultWorkBegin;
			WorkEnd = DefaultWorkEnd;
			WorkStep = DefaultWorkStep;
		}

		public Brigad(string name) : this()
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

		[Property, Description("Время начала работ по подключению")]
		public virtual TimeSpan? WorkBegin { get; set; }

		[Property, Description("Время окончания работ по подключению")]
		public virtual TimeSpan? WorkEnd { get; set; }

		[Property, Description("Интервал между подключениями")]
		public virtual TimeSpan? WorkStep { get; set; }

		[HasMany(ColumnKey = "Brigad", Lazy = true)]
		public virtual IList<ConnectGraph> Graphs { get; set; }

		public virtual Client GetOneGraph(int intervalNum, DateTime selectDate)
		{
			var timeInterval = TimeInterval.FromRequests(selectDate, this, Graphs.ToList())[intervalNum];
			return Graphs.Where(c => c.DateAndTime.Date == selectDate.Date
				&& c.DateAndTime.TimeOfDay >= timeInterval.Begin
				&& c.DateAndTime.TimeOfDay < timeInterval.End)
				.Select(g => g.Client)
				.FirstOrDefault();
		}

		public virtual List<TimeInterval> GetIntervals(ISession session, DateTime? date = null)
		{
			if (WorkBegin == null || WorkEnd == null || WorkStep == null) 
				return null;
			var intervals = new List<TimeInterval>();
			if (date == null)
				return null;
			var begin = date.Value;
			var end = date.Value.AddDays(1);
			var requests = session.Query<ConnectGraph>().Where(r => r.DateAndTime >= begin
				&& r.DateAndTime < end
				&& r.Brigad == this)
				.ToList();
			return TimeInterval.FromRequests(date.Value, this, requests);
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