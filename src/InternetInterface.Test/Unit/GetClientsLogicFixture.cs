﻿using System;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Queries;
using NUnit.Framework;

namespace InternetInterface.Test.Unit
{
	[TestFixture]
	public class GetClientsLogicFixture
	{
		private SearchFilter _filter;
		private Partner _thisPartner;

		[SetUp]
		public void Setup()
		{
			_thisPartner = new Partner {
				Role = new UserRole {
					ReductionName = "Office"
				}
			};
			InitializeContent.GetPartner = () => _thisPartner;
			_filter = new SearchFilter {
				ClientTypeFilter = ForSearchClientType.AllClients,
				SearchProperties = SearchUserBy.Auto,
				EnabledTypeProperties = EndbledType.All
			};
		}

		[Test]
		public void Get_where_diller_id()
		{
			_thisPartner.Role.ReductionName = "Diller";
			_filter.SearchText = "5";
			var result = _filter.GetWhere();
			Assert.That(result, Is.EqualTo("WHERE (c.Id = 5) and (C.PhysicalClient is not null)"));
		}

		[Test]
		public void Get_where_diller_name()
		{
			//Эти тесты надо выкинуть, потому что это бред
			//Проверять надо результат работы объектов а не их отдельных функций
			//Мешает модификации
			_thisPartner.Role.ReductionName = "Diller";
			_filter.SearchText = "testText";
			var result = _filter.GetWhere();
			Assert.That(result, Is.EqualTo("WHERE (C.Name REGEXP :SearchText) and (C.PhysicalClient is not null)"));
		}

		[Test]
		public void Get_where_office_base()
		{
			var result = _filter.GetWhere();
			Assert.That(result, Is.Empty);
		}

		[Test]
		public void Get_where_office_text()
		{
			//Эти тесты надо выкинуть, потому что это бред
			//Проверять надо результат работы объектов а не их отдельных функций
			//Мешает модификации
			_filter.SearchText = "testText";
			var result = _filter.GetWhere();
			Assert.That(result, Is.EqualTo(@"
	WHERE
	(C.Name REGEXP :SearchText or
	C.id like :SearchText or
	p.ExternalClientId like :SearchText or
	co.Contact like :SearchText or
	h.Street like :SearchText or
	l.ActualAdress like :SearchText)"),
				result);
		}

		[Test]
		public void Get_where_office_statusType()
		{
			_filter.StatusType = 1;
			var result = _filter.GetWhere();
			Assert.That(result, Is.EqualTo("WHERE  S.Id = :statusType"));
		}

		[Test]
		public void Get_where_office_clientTypeFilter_Physical()
		{
			_filter.ClientTypeFilter = ForSearchClientType.Physical;
			var result = _filter.GetWhere();
			Assert.That(result, Is.EqualTo("WHERE  C.PhysicalClient is not null"));
		}

		[Test]
		public void Get_where_office_clientTypeFilter_Lawyer()
		{
			_filter.ClientTypeFilter = ForSearchClientType.Lawyer;
			var result = _filter.GetWhere();
			Assert.That(result, Is.EqualTo("WHERE  C.LawyerPerson is not null"));
		}

		[Test]
		public void Get_where_office_EnabledTypeProperties_enabled()
		{
			_filter.EnabledTypeProperties = EndbledType.Enabled;
			var result = _filter.GetWhere();
			Assert.That(result, Is.EqualTo("WHERE  c.Disabled = false"));
		}

		[Test]
		public void Get_where_office_EnabledTypeProperties_disabled()
		{
			_filter.EnabledTypeProperties = EndbledType.Disabled;
			var result = _filter.GetWhere();
			Assert.That(result, Is.EqualTo("WHERE  c.Disabled"));
		}

		[Test]
		public void Get_where_office_searchProperties_IsSearchAccount()
		{
			_filter.SearchText = "5";
			_filter.SearchProperties = SearchUserBy.SearchAccount;
			var result = _filter.GetWhere();
			Assert.That(result, Is.EqualTo("where C.id = 5"));
		}

		[Test]
		public void Get_where_office_searchProperties_IsSearchByFio()
		{
			//Эти тесты надо выкинуть, потому что это бред
			//Проверять надо результат работы объектов а не их отдельных функций
			//Мешает модификации
			_filter.SearchText = "5";
			_filter.SearchProperties = SearchUserBy.ByFio;
			var result = _filter.GetWhere();
			Assert.That(result, Is.EqualTo(@"WHERE (C.Name REGEXP :SearchText)"));
		}

		[Test]
		public void Get_where_office_searchProperties_IsSearchTelephone()
		{
			_filter.SearchText = "5";
			_filter.SearchProperties = SearchUserBy.TelNum;
			var result = _filter.GetWhere();
			Assert.That(result, Is.EqualTo("WHERE (co.Contact like :SearchText)"));
		}

		[Test]
		public void Get_where_office_searchProperties_IsSearchByAddress()
		{
			_filter.SearchText = "5";
			_filter.SearchProperties = SearchUserBy.ByPassport;
			var result = _filter.GetWhere();
			Assert.That(result, Is.EqualTo(String.Format(@"
	WHERE (p.PassportSeries like {0} or p.PassportNumber like {0} or l.ActualAdress like {0})", ":SearchText")));
		}

		[Test]
		public void Get_where_if_adress_find_city()
		{
			_filter.SearchProperties = SearchUserBy.Address;
			_filter.City = "testCity";
			var where = _filter.GetWhere();
			Assert.AreEqual(where, @"where(p.City like :City or l.ActualAdress like :City)");
		}

		[Test]
		public void Get_where_if_adress_find_street()
		{
			_filter.SearchProperties = SearchUserBy.Address;
			_filter.Street = "Street";
			var where = _filter.GetWhere();
			Assert.AreEqual(where, @"where(h.Street like :Street or l.ActualAdress like :Street or p.Street like :Street)");
		}

		[Test]
		public void Get_where_if_adress_find_house()
		{
			_filter.SearchProperties = SearchUserBy.Address;
			_filter.House = "5";
			var where = _filter.GetWhere();
			Assert.AreEqual(where, @"where(p.House = :House)");
		}

		[Test]
		public void Get_where_if_adress_find_case_house()
		{
			_filter.SearchProperties = SearchUserBy.Address;
			_filter.CaseHouse = "testCase";
			var where = _filter.GetWhere();
			Assert.AreEqual(where, @"where(p.CaseHouse like :CaseHouse or l.ActualAdress like :CaseHouse)");
		}

		[Test]
		public void Get_where_if_adress_find_apartment()
		{
			_filter.SearchProperties = SearchUserBy.Address;
			_filter.Apartment = "5";
			var where = _filter.GetWhere();
			Assert.AreEqual(where, @"where(p.Apartment = :Apartment)");
		}

		[Test]
		public void Get_where_if_adress_find_all()
		{
			_filter.SearchProperties = SearchUserBy.Address;
			_filter.City = "testCity";
			_filter.Street = "testStreet";
			_filter.House = "testHouse";
			_filter.CaseHouse = "testCaseCouse";
			_filter.Apartment = "restApartment";
			var where = _filter.GetWhere();
			Assert.AreEqual(where, @"where(p.City like :City or l.ActualAdress like :City) and (h.Street like :Street or l.ActualAdress like :Street or p.Street like :Street) and (p.House = :House) and (p.CaseHouse like :CaseHouse or l.ActualAdress like :CaseHouse) and (p.Apartment = :Apartment)");
		}

		[Test]
		public void GetQueryStringTest()
		{
			_filter.ClientTypeFilter = ForSearchClientType.Lawyer;
			_filter.SearchProperties = SearchUserBy.SearchAccount;
			_filter.EnabledTypeProperties = EndbledType.Enabled;

			var query = _filter.GetQueryString();
			Assert.That(query["filter.ClientTypeFilter"], Is.EqualTo(_filter.ClientTypeFilter));
			Assert.That(query["filter.SearchProperties"], Is.EqualTo(_filter.SearchProperties));
			Assert.That(query["filter.EnabledTypeProperties"], Is.EqualTo(_filter.EnabledTypeProperties));
		}
	}
}