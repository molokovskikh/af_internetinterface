using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml.Linq;
using System.Xml.XPath;
using Common.Tools;
using Inforoom2.validators;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Hql.Ast;
using NHibernate.Linq;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель агента
	/// </summary> 
	[Class(0, Table = "ignoredinns", NameType = typeof(IgnoredInn), Schema = "Billing")]
	public class IgnoredInn : BaseModel
	{
		[Property]
		public virtual string Inn { get; set; }
	}
}