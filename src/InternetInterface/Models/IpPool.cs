using System;
using Castle.ActiveRecord;

namespace InternetInterface.Models
{
	[ActiveRecord("ippools", Schema = "internet", Lazy = true)]
	public class IpPool : ChildActiveRecordLinqBase<IpPool>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		//будь бдителен адреса хранятся в bigendian формате
		[Property]
		public virtual uint Begin { get; set; }

		//будь бдителен адреса хранятся в bigendian формате
		[Property]
		public virtual uint End { get; set; }

		[Property]
		public virtual int LeaseTime { get; set; }

		[Property]
		public virtual bool IsGray { get; set; }

		/// <summary>
		/// Возвращает IP-адрес в стандартном формате
		/// </summary>
		/// <param name="addrNum">Числовое значение IP-адреса</param>
		/// <returns>Строка в формате "NNN.NNN.NNN.NNN"</returns>
		public static string GetIpAddressFromNumber(uint addrNum)
		{
			var fstBase = Math.Pow(2, 24);				// 16777216
			var sndBase = Math.Pow(2, 16);				// 65536
			var trdBase = Math.Pow(2, 8);					// 256
			return Math.Floor(addrNum / fstBase).ToString("0") + "."
			       + Math.Floor(addrNum % fstBase / sndBase).ToString("0") + "."
			       + Math.Floor(addrNum % sndBase / trdBase).ToString("0") + "."
			       + Math.Floor(addrNum % trdBase).ToString("0");
		}

		/// <summary>
		/// Возвращает начальный IP-адрес пула в стандартном формате
		/// </summary>
		public virtual string GetBeginIp()
		{
			return GetIpAddressFromNumber(Begin);
		}

		/// <summary>
		/// Возвращает конечный IP-адрес пула в стандартном формате
		/// </summary>
		public virtual string GetEndIp()
		{
			return GetIpAddressFromNumber(End);
		}
	}
}