using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;


namespace InforoomInternet.Models
{
	/*[ActiveRecord(Schema = "Internet")]
	public class Lease : ActiveRecordLinqBase<Lease>
	{
		private const string IPRegExp =
	@"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b";

		[PrimaryKey]
		public uint Id { get; set; }

		[Property("`ip`")]
		public uint Ip { get; set; }

		[BelongsTo]
		public ClientEndpoint Endpoint { get; set; }

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

	[ActiveRecord(Schema = "Internet")]
	public class ClientEndpoint
	{
		[PrimaryKey]
		public uint Id { get; set; }

		[Property]
		public bool Monitoring { get; set; }

		[Property]
		public int? PackageId { get; set; }

		[BelongsTo]
		public Client Client { get; set; }
	}

	[ActiveRecord(Schema = "Internet")]
	public class Client
	{
		[PrimaryKey]
		public uint Id { get; set; }

		[BelongsTo]
		public PhysicalClient PhisicalClient { get; set; }

		[Property]
		public bool Disabled { get; set; }

	}
	
	[ActiveRecord(Schema = "Internet")]
	public class PhysicalClient : ActiveRecordLinqBase<PhysicalClient>
	{
		[PrimaryKey]
		public uint Id { get; set; }

		[Property]
		public decimal Balance { get; set; }

		[BelongsTo]
		public Tariff Tariff { get; set; }

		[Property]
		public string Password { get; set; }

		public decimal ToPay()
		{
			return Math.Abs(Balance) + Tariff.Price;
		}

		public static Lease FindByIP(string ip)
		{
			var addressValue = BigEndianConverter.ToInt32(IPAddress.Parse(ip).GetAddressBytes());
			return Lease.Queryable.FirstOrDefault(l => l.Ip == addressValue);
		}
	}*/
}