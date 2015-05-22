using System;
using System.Text.RegularExpressions;
using InternetInterface.Models;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{

	[Class(0, Table = "PackageSpeed", NameType = typeof(PackageSpeed))]
	public class PackageSpeed : BaseModel
	{

		[Property]
		public virtual int PackageId { get; set; }

		[Property]
		public virtual int Speed { get; set; }

		[Property]
		public virtual bool IsPhysic { get; set; }

		/// <summary>
		/// Получение скорости в пакета в Мегабитах
		/// </summary>
		public virtual float GetSpeed()
		{
			//Приводим к флоту, так как изначально скорость записывается в байтах
			//После деления, у всех скоростей, что меньше 1 мегабита, будут 0 отображаться.
			var sp = (float)Speed;
			float val = sp == 0 ? 0 : sp / 1000000;
			return val;
		}
	}
}