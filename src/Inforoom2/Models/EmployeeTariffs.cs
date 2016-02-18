using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{    /// <summary>
		/// Модель агента
		/// </summary>
	[Class(0, Table = "AgentTariffs", NameType = typeof(EmployeeTariff))]
	public class EmployeeTariff : BaseModel
	{
		[Property]
		public virtual string ActionName { get; set; }

		[Property]
		public virtual decimal Sum { get; set; }

		[Property]
		public virtual string Description { get; set; }

	}
}