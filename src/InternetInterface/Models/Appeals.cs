using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models.Universal;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Models
{
	public enum AppealType
	{
		[Description("Все")] All = 0,
		[Description("Пользовательские")] User = 1,
		[Description("Системные")] System = 3,
		[Description("Внешние")] FeedBack = 5,
		[Description("Статистические")] Statistic = 7
	}

	public enum UniversalAppealType
	{
		Appeal,
		Service
	}

	public class UniversalAppeal
	{
		public uint Id;
		public string Text;
		public DateTime Date;
		public string Partner;
		public List<UniversalAppeal> SubFields;
		public UniversalAppealType Type;
		public AppealType AppealType;

		public string Color
		{
			get
			{
				if (Type == UniversalAppealType.Service)
					return "#C8E8CB";
				if (AppealType == AppealType.User)
					return "#f5efdf";
				if (AppealType == AppealType.System)
					return "#e3e7f7";
				if (AppealType == AppealType.FeedBack)
					return "#FCD9D9";
				return string.Empty;
			}
		}
	}

	[ActiveRecord(Schema = "internet", Lazy = true)]
	public class Appeals : ValidActiveRecordLinqBase<Appeals>
	{
		public Appeals()
		{
			Date = DateTime.Now;
		}

		public Appeals(string appeal, Client client, AppealType appealType, bool usePartner) : this()
		{
			Appeal = appeal;
			Client = client;
			AppealType = appealType;
			if (usePartner)
				Partner = InitializeContent.Partner;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Appeal { get; set; }

		[Property]
		public virtual DateTime Date { get; set; }

		[BelongsTo("Partner")]
		public virtual Partner Partner { get; set; }

		[BelongsTo("Client")]
		public virtual Client Client { get; set; }

		[Property]
		public virtual AppealType AppealType { get; set; }

		public virtual string GetTransformedAppeal()
		{
			return AppealHelper.GetTransformedAppeal(Appeal);
		}

		public static List<UniversalAppeal> GetAllAppeal(ISession session, Client client, AppealType Type)
		{
			Expression<Func<Appeals, bool>> predicate;
			if (Type == AppealType.All)
				predicate = a => a.Client == client;
			else {
				predicate = a => a.Client == client && a.AppealType == Type;
			}
			var appeals = Queryable.Where(predicate).ToList().Select(a => new UniversalAppeal {
				Date = a.Date,
				Partner = a.Partner != null ? a.Partner.Name : string.Empty,
				Text = a.GetTransformedAppeal(),
				Type = UniversalAppealType.Appeal,
				AppealType = a.AppealType
			}).ToList();
			if (Type == AppealType.All) {
				var serviceRequests = session.Query<ServiceRequest>().Where(s => s.Client == client);
				var service = serviceRequests.ToList().Select(s => new UniversalAppeal {
					Id = s.Id,
					Date = s.RegDate,
					Partner = s.Registrator != null ? s.Registrator.Name : string.Empty,
					Text = s.GetDescription(),
					Type = UniversalAppealType.Service,
					SubFields = s.Iterations.Count > 0 ?
						new List<UniversalAppeal>(s.Iterations.Select(i => new UniversalAppeal {
							Id = s.Id,
							Date = i.RegDate,
							Partner = i.Performer != null ? i.Performer.Name : string.Empty,
							Text = i.GetDescription(),
							Type = UniversalAppealType.Service
						})) : null
				}).ToList();
				appeals.AddRange(service);
			}
			return appeals.OrderByDescending(a => a.Date).ToList();
		}

		public virtual string Type()
		{
			var type = string.Empty;
			if (AppealType == AppealType.User)
				type = "Пользовательское";
			if (AppealType == AppealType.System)
				type = "Системное";
			return type;
		}
	}
}