using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;
using Castle.Components.Validator;
using Common.Web.Ui.Helpers;
using InternetInterface.Helpers;
using InternetInterface.Models.Universal;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Models
{
	[ActiveRecord("NetworkSwitches", Schema = "Internet", Lazy = true)]
	public class NetworkSwitches	
	{
		public const string IPRegExp =
			@"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b";

		private const string MACRegExp = @"^([0-9a-fA-F][0-9a-fA-F]-){5}([0-9a-fA-F][0-9a-fA-F])$";

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, ValidateRegExp(MACRegExp, "Ошибка ввода MAC (**-**-**-**-**)"), ValidateIsUnique("Такой МАС уже существует")]
		public virtual string Mac { get; set; }

		[Property, ValidateRegExp(IPRegExp, "Ошибка формата IP адреса (max 255.255.255.255))")]
		public virtual string IP { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[BelongsTo, ValidateNonEmpty]
		public virtual Zone Zone { get; set; }

		[Property]
		public virtual int PortCount { get; set; }

		[Property]
		public virtual string Comment { get; set; }

		public override string ToString()
		{
			return Name + String.Format(" ({0})", GetNormalIp());
		}

		public virtual string GetCommentForWeb()
		{
			return AppealHelper.GetTransformedAppeal(Comment);
		}

		public virtual string GetNormalIp()
		{
			return IpHelper.GetNormalIp(IP);
		}

		public static string GetNormalIp(uint ip)
		{
			return IpHelper.GetNormalIp(ip.ToString());
		}

		public static string SetProgramIp(string ip)
		{
			var valid = new Regex(IPRegExp);
			if (!String.IsNullOrEmpty(ip) && valid.IsMatch(ip))
			{
				var splited = ip.Split('.');
				var fg = new byte[8];
				fg[0] = Convert.ToByte(splited[3]);
				fg[1] = Convert.ToByte(splited[2]);
				fg[2] = Convert.ToByte(splited[1]);
				fg[3] = Convert.ToByte(splited[0]);
				return BitConverter.ToInt64(fg, 0).ToString();
			}
			return String.Empty;
		}

		public static List<NetworkSwitches> All(ISession session)
		{
			return session.Query<NetworkSwitches>().Where(s => s.Name != null && s.Name != "").OrderBy(s => s.Name).ToList();
		}
	}
}