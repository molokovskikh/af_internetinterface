﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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

		[Property, NotNullNotEmpty(Message = "Введите номер телефона"), Pattern(@"^(([0-9]{1})-([0-9]{3})-([0-9]{3})-([0-9]{2})-([0-9]{2}))", RegexOptions.Compiled, "Ошибка формата телефонного номера: мобильный телефон (8-***-***-**-**))")]
		public virtual string ApplicantPhoneNumber { get; set; }

		[Property(Column = "ApplicantEmail"), NotNullNotEmpty(Message = "Введите Email"), Pattern(@"^\S+@\S+$")]
		public virtual string Email { get; set; }

		[ManyToOne(Column = "_Address", Cascade = "save-update")]
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

		public virtual string YandexStreet { get; set; }

		public virtual string YandexHouse { get; set; }

		public virtual string AddressAsString { get; set; }

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
			if (!IsYandexAddressValid()) {
				return false;
			}
			var switchAddress = switchAddresses.FirstOrDefault((sa => (sa.House != null
			                                                           && sa.House.Street.Region.City.Name.Equals(YandexCity, StringComparison.InvariantCultureIgnoreCase)
			                                                           && sa.House.Street.Name.Equals(YandexStreet, StringComparison.InvariantCultureIgnoreCase)
			                                                           && sa.House.Number.Equals(YandexHouse, StringComparison.InvariantCultureIgnoreCase))
				//проверка частного сектора (частный сектор содержит только улицу) 
			                                                          ||
			                                                          (sa.Street != null) &&
			                                                          (sa.Street.Region.City.Name.Equals(YandexCity, StringComparison.InvariantCultureIgnoreCase)
			                                                           && sa.Street.Name.Equals(YandexStreet, StringComparison.InvariantCultureIgnoreCase))));
			return switchAddress != null;
		}
	}
}