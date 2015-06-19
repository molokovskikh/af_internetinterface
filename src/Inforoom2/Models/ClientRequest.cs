﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Inforoom2.validators;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	public enum RequestType
	{
		[Display(Name = "от клиента")] FromClient = 1,
		[Display(Name = "от оператора")] FromOperator = 2,
		[Display(Name = "автоматическая")] Automatic = 3
	}

	[Class(0, Table = "Requests", NameType = typeof(ClientRequest))]
	public class ClientRequest : BaseModel
	{
		[Property(Column = "_Comment")]
		public virtual string Comment { get; set; }

		[Property, NotNullNotEmpty(Message = "Введите ФИО")]
		public virtual string ApplicantName { get; set; }
		 
		[Property, ApplicantPhoneValidator]
		public virtual string ApplicantPhoneNumber { get; set; }

		[Property(Column = "ApplicantEmail"), ValidatorEmail]
		public virtual string Email { get; set; }

		[ManyToOne(Column = "_Address", Cascade = "save-update")]
		public virtual Address Address { get; set; }

		[ManyToOne(Column = "Tariff"), NotNull]
		public virtual Plan Plan { get; set; }

		[Property]
		public virtual bool SelfConnect { get; set; }

		// TODO: сделать специальный тип моделей, содержащий метод
		[Property]
		public virtual int Label { get; set; }

		[ManyToOne(Column = "_ServiceMan")]
		public virtual ServiceMan ServiceMan { get; set; }

		[Property(Column = "_BeginTime")]
		public virtual DateTime BeginTime { get; set; }

		[Property(Column = "_EndTime")]
		public virtual DateTime EndTime { get; set; }

		[Property(Column = "_RequestSource"), Description("Источник заявки")]
		public virtual RequestType RequestSource { get; set; }

		[ManyToOne(Column = "Registrator"), Description("Автор регистрации заявки")]
		public virtual Employee RequestAuthor { get; set; }

		//Поля старой модели заявки

		// ===========================|~убрать|============================>>>
		[Property, NotNullNotEmpty(Message = "Введите город")]
		public virtual string City { get; set; }

		[Property, NotNullNotEmpty(Message = "Введите улицу")]
		public virtual string Street { get; set; }

		[Property(Column = "House"), NotNull(Message = "Введите номер дома")]
		public virtual int? HouseNumber { get; set; }

		[Property(Column = "CaseHouse")]
		public virtual string Housing { get; set; }

		// для привязки клиента к заявке (на нее есть ссылка в информации о клиенте)
		[ManyToOne(Column = "Client")]
		public virtual Client Client { get; set; }

		// <<<================================================================


		[Property, NotNull(Message = "Введите номер квартиры"), Digits(3, Message = "Здесь должно быть число")]
		public virtual int Apartment { get; set; }

		[Property, Digits(3, Message = "Здесь должно быть число")]
		public virtual int Entrance { get; set; }

		[Property, Digits(3, Message = "Здесь должно быть число")]
		public virtual int Floor { get; set; }

		[Property]
		public virtual DateTime ActionDate { get; set; }

		[Property]
		public virtual DateTime RegDate { get; set; }

		public virtual bool IsContractAccepted { get; set; }

		public virtual string YandexCity { get; set; }

		[Property]
		public virtual string YandexStreet { get; set; }

		[Property]
		public virtual string YandexHouse { get; set; }

		public virtual string AddressAsString { get; set; }

		[Property(Column = "Archive")]
		public virtual bool Archived { get; set; }

		public virtual string GetAddress()
		{
			var str = City + " " + Street + " " + HouseNumber + " " + Housing + " " + Apartment;
			if (Floor != 0)
				str += " (" + Floor + " этаж)";
			return str;
		}

		public virtual bool IsYandexAddressValid()
		{
			if (string.IsNullOrEmpty(YandexCity)
			    || string.IsNullOrEmpty(YandexStreet)
			    || string.IsNullOrEmpty(YandexHouse)
			    || YandexStreet == "undefined"
			    || YandexCity == "undefined"
			    || YandexHouse == "undefined") {
				return false;
			}
			return true;
		}

		public virtual bool IsAddressConnected(IList<SwitchAddress> switchAddresses)
		{
			if (!IsYandexAddressValid())
				return false;
			var switchAddress = switchAddresses.FirstOrDefault(sa => (sa.House != null
			                                                          && sa.House.Street.Region.City != null
			                                                          && sa.House.Street.Region.City.Name.Equals(YandexCity, StringComparison.InvariantCultureIgnoreCase)
			                                                          && sa.House.Street.Name.Equals(YandexStreet, StringComparison.InvariantCultureIgnoreCase)
			                                                          && sa.House.Number.ToLower().Equals(YandexHouse.ToLower(), StringComparison.InvariantCultureIgnoreCase)));
			return switchAddress != null;
		}
	}
}