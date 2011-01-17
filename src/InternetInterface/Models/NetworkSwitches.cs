using System;
using System.Text.RegularExpressions;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;
using Castle.Components.Validator;
using InternetInterface.Models.Universal;

namespace InternetInterface.Models
{
	[ActiveRecord("NetworkSwitches", Schema = "Internet", Lazy = true)]
	public class NetworkSwitches : ValidActiveRecordLinqBase<NetworkSwitches>
	{
		private const string IPRegExp =
			@"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b";

		private const string MACRegExp = @"^([0-9a-fA-F][0-9a-fA-F]-){5}([0-9a-fA-F][0-9a-fA-F])$";

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, ValidateRegExp(MACRegExp, "Ошибка ввода MAC (**-**-**-**-**)")]
		public virtual string Mac { get; set; }

		[Property, ValidateRegExp(IPRegExp, "Ошибка фотмата IP адреса (max 255.255.255.255))")]
		public virtual string IP { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[BelongsTo("Zone")]
		public virtual Zone Zone { get; set; }


		public virtual string GetNormalIp()
		{
			if (!string.IsNullOrEmpty(IP))
			{
				var splited = IP.Split('.');
				var valid = new Regex(IPRegExp);
				if ((valid.IsMatch(IP)) || (splited.Length == 1))
				{
					var normalip = BitConverter.GetBytes(Convert.ToInt64(IP));
					return string.Format("{0}.{1}.{2}.{3}", normalip[3], normalip[2], normalip[1],
					                     normalip[0]);
				}
			}
			return string.Empty;
		}

		public static string SetProgramIp(string ip)
		{
			var valid = new Regex(IPRegExp);
			if (valid.IsMatch(ip))
			{
				var splited = ip.Split('.');
				var fg = new byte[8];
				fg[0] = Convert.ToByte(splited[3]);
				fg[1] = Convert.ToByte(splited[2]);
				fg[2] = Convert.ToByte(splited[1]);
				fg[3] = Convert.ToByte(splited[0]);
				return BitConverter.ToInt64(fg, 0).ToString();
			}
			return string.Empty;
		}
	}
}