using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;
using Castle.Components.Validator;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.MonoRailExtentions;
using InternetInterface.Helpers;
using InternetInterface.Models.Universal;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Models
{
	public enum SwitchType
	{
		[Description("Неизвестный")]
		Unknown = 0,
		[Description("Каталист")]
		Catalyst = 1,
		[Description("Линксис")]
		Linksys = 2,
		[Description("Д-линк")]
		Dlink = 3,
		[Description("Хуафей")]
		Huawei = 4
	}

	[ActiveRecord("NetworkSwitches", Schema = "Internet", Lazy = true)]
	public class NetworkSwitch
	{
		public NetworkSwitch()
		{
		}

		public NetworkSwitch(string name, Zone zone)
		{
			Name = name;
			Zone = zone;
		}

		public const string IPRegExp =
			@"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b";

		private const string MACRegExp = @"^([0-9a-fA-F][0-9a-fA-F]-){5}([0-9a-fA-F][0-9a-fA-F])$";

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, ValidateRegExp(MACRegExp, "Ошибка ввода MAC (**-**-**-**-**)"), ValidateIsUnique("Такой МАС уже существует")]
		public virtual string Mac { get; set; }

		[Property(ColumnType = "InternetInterface.Helpers.IPUserType, InternetInterface"),
			ValidateRegExp(IPRegExp, "Ошибка формата IP адреса (max 255.255.255.255))")]
		public virtual IPAddress IP { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[BelongsTo, ValidateNonEmpty]
		public virtual Zone Zone { get; set; }

		[Property]
		public virtual int PortCount { get; set; }

		[Property]
		public virtual string Comment { get; set; }

		[HasMany]
		public virtual IList<ClientEndpoint> Endpoints { get; set; }

		[Property, ValidateInteger("Введите число"), ValidateGreaterThanZero]
		public virtual int TotalPorts { get; set; }

		[Property]
		public virtual SwitchType Type { get; set; }

		public override string ToString()
		{
			return Name + String.Format(" ({0})", IP);
		}

		public virtual string GetCommentForWeb()
		{
			return AppealHelper.GetTransformedAppeal(Comment);
		}

		public static List<NetworkSwitch> All()
		{
			return ArHelper.WithSession(s => All(s));
		}

		public static List<NetworkSwitch> All(ISession session, RegionHouse region = null)
		{
			var query = session.Query<NetworkSwitch>().Where(s => s.Name != null && s.Name != "");
			if (region != null) {
				query = query.Where(s => s.Zone.Region == region);
			}
			return query.OrderBy(s => s.Name).ToList();
		}
	}
}