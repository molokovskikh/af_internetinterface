using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using NHibernate.Engine;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "Requests", NameType = typeof(ClientRequest))]
	public class ClientRequest : BaseModel
	{
		[Property, NotNullNotEmpty(Message = "Введите ФИО")]
		public virtual string ApplicantName { get; set; }

		[Property,NotNullNotEmpty(Message = "Введите номер телефона"), Pattern(@"^(([0-9]{1})-([0-9]{3})-([0-9]{3})-([0-9]{2})-([0-9]{2}))",RegexOptions.Compiled, "Ошибка формата телефонного номера: мобильный телефон (8-***-***-**-**))")]
		public virtual string ApplicantPhoneNumber { get; set; }
		
		[Property(Column = "ApplicantEmail"), NotNullNotEmpty(Message = "Введите Email"), Pattern(@"^\S+@\S+$")]
		public virtual string Email { get; set; }

		[ManyToOne(Column = "_Address")]
		public virtual Address Address { get; set; }

		[ManyToOne(Column = "Tariff"), NotNull]
		public virtual Plan Plan { get; set; }

		[Property]
		public virtual bool SelfConnect { get; set; }



		//Поля старой модели заявки

		[Property, NotNullNotEmpty(Message = "Введите город")]
		public virtual string City { get; set; }
		
		[Property, NotNullNotEmpty(Message = "Введите улицу")]
		public virtual string Street { get; set; }

		[Property(Column = "House"), NotNull(Message = "Введите номер дома")]
		public virtual int? HouseNumber { get; set; }
		
		[Property(Column = "CaseHouse")]
		public virtual string Housing { get; set; }

		[Property,NotNull(Message = "Введите номер квартиры"), Digits(3,Message = "Здесь должно быть число")]
		public virtual int? Apartment { get; set; }
		
		[Property, Digits(3,Message = "Здесь должно быть число")]
		public virtual int? Entrance { get; set; }
	
		[Property, Digits(3,Message = "Здесь должно быть число")]
		public virtual int? Floor { get; set; }

		[Property]
		public virtual DateTime ActionDate { get; set; }
		
		[Property]
		public virtual DateTime RegDate { get; set; }

		public virtual bool IsContractAccepted { get; set; }
	}
}