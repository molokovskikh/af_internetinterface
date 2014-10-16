using System;
using NHibernate.Mapping.Attributes;


namespace Inforoom2.Models
{
	/// <summary>
	/// Модель пользователя
	/// </summary>
	[Class(0, Table = "client", NameType = typeof (Client))]
	public class Client : BaseModel
	{
	
		[Property]
		public virtual string Username { get; set; }

		[Property]
		public virtual string Password { get; set; }

		[Property]
		public virtual string Salt { get; set; }
		
		[Property]
		public virtual string City { get; set; }

		[Property]
		public virtual string Email { get; set; }
	}
}