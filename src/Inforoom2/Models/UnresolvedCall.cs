using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель многопарника
	/// </summary>
	[Class(0, Table = "UnresolvedPhone", NameType = typeof (UnresolvedCall), Schema = "telephony")]
	public class UnresolvedCall : BaseModel
	{

		[Property]
		public virtual string Phone { get; set; }
	}
}