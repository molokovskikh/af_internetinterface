﻿using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "dealers", NameType = typeof(Dealer))]
	public class Dealer : BaseModel
	{
		[Property, NotEmpty(Message = "Введите имя клиента")]
		public virtual string Name { get; set; }

		[Property]
		public virtual bool Active { get; set; }
	}
}