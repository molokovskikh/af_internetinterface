using System;
using System.Collections.Generic;
using System.Linq;
using InternetInterface.AllLogic;
using InternetInterface.Controllers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Queries;
using InternetInterface.Test.Helpers;
using NUnit.Framework;

namespace InternetInterface.Test.Unit
{
	[TestFixture]
	public class GetClientsLogicFixture
	{
		private SeachFilter _filter;
		private Partner _thisPartner;

		[SetUp]
		public void Setup()
		{
			_thisPartner = new Partner {
				Categorie = new UserCategorie {
					ReductionName = "Office"
				}
			};
			InitializeContent.GetAdministrator = () => _thisPartner;
			_filter = new SeachFilter {
				ClientTypeFilter = new ClientTypeProperties { Type = ForSearchClientType.AllClients },
				SearchProperties = new UserSearchProperties { SearchBy = SearchUserBy.Auto },
				EnabledTypeProperties = new EnabledTypeProperties { Type = EndbledType.All }
			};
		}

		[Test]
		public void Get_where_diller_id()
		{
			_thisPartner.Categorie.ReductionName = "Diller";
			_filter.SearchText = "5";
			var result = GetClientsLogic.GetWhere(_filter);
			Assert.That(result, Is.EqualTo("WHERE (c.Id = 5) and (C.PhysicalClient is not null)"));
		}

		[Test]
		public void Get_where_diller_name()
		{
			_thisPartner.Categorie.ReductionName = "Diller";
			_filter.SearchText = "testText";
			var result = GetClientsLogic.GetWhere(_filter);
			Assert.That(result, Is.EqualTo("WHERE (LOWER(C.Name) like :SearchText) and (C.PhysicalClient is not null)"));
		}

		[Test]
		public void Get_where_office_base()
		{
			var result = GetClientsLogic.GetWhere(_filter);
			Assert.That(result, Is.Empty);
		}

		[Test]
		public void Get_where_office_text()
		{
			_filter.SearchText = "testText";
			var result = GetClientsLogic.GetWhere(_filter);
			Assert.That(result, Is.EqualTo(@"
	WHERE
	(LOWER(C.Name) like :SearchText or
	C.id like :SearchText or
	LOWER(co.Contact) like :SearchText or
	LOWER(h.Street) like :SearchText or
	LOWER(l.ActualAdress) like :SearchText )"), result);
		}

		[Test]
		public void Get_where_office_statusType()
		{
			_filter.StatusType = 1;
			var result = GetClientsLogic.GetWhere(_filter);
			Assert.That(result, Is.EqualTo("WHERE  S.Id = :statusType"));
		}

		[Test]
		public void Get_where_office_clientTypeFilter_Physical()
		{
			_filter.ClientTypeFilter.Type = ForSearchClientType.Physical;
			var result = GetClientsLogic.GetWhere(_filter);
			Assert.That(result, Is.EqualTo("WHERE  C.PhysicalClient is not null"));
		}

		[Test]
		public void Get_where_office_clientTypeFilter_Lawyer()
		{
			_filter.ClientTypeFilter.Type = ForSearchClientType.Lawyer;
			var result = GetClientsLogic.GetWhere(_filter);
			Assert.That(result, Is.EqualTo("WHERE  C.LawyerPerson is not null"));
		}

		[Test]
		public void Get_where_office_EnabledTypeProperties_enabled()
		{
			_filter.EnabledTypeProperties.Type = EndbledType.Enabled;
			var result = GetClientsLogic.GetWhere(_filter);
			Assert.That(result, Is.EqualTo("WHERE  c.Disabled = false"));
		}

		[Test]
		public void Get_where_office_EnabledTypeProperties_disabled()
		{
			_filter.EnabledTypeProperties.Type = EndbledType.Disabled;
			var result = GetClientsLogic.GetWhere(_filter);
			Assert.That(result, Is.EqualTo("WHERE  c.Disabled"));
		}

		[Test]
		public void Get_where_office_searchProperties_IsSearchAccount()
		{
			_filter.SearchText = "5";
			_filter.SearchProperties.SearchBy = SearchUserBy.SearchAccount;
			var result = GetClientsLogic.GetWhere(_filter);
			Assert.That(result, Is.EqualTo("where C.id = 5"));
		}

		[Test]
		public void Get_where_office_searchProperties_IsSearchByFio()
		{
			_filter.SearchText = "5";
			_filter.SearchProperties.SearchBy = SearchUserBy.ByFio;
			var result = GetClientsLogic.GetWhere(_filter);
			Assert.That(result, Is.EqualTo(@"
	WHERE (LOWER(C.Name) like :SearchText )"));
		}

		[Test]
		public void Get_where_office_searchProperties_IsSearchTelephone()
		{
			_filter.SearchText = "5";
			_filter.SearchProperties.SearchBy = SearchUserBy.TelNum;
			var result = GetClientsLogic.GetWhere(_filter);
			Assert.That(result, Is.EqualTo("WHERE (LOWER(co.Contact) like :SearchText)"));
		}

		[Test]
		public void Get_where_office_searchProperties_IsSearchByAddress()
		{
			_filter.SearchText = "5";
			_filter.SearchProperties.SearchBy = SearchUserBy.ByAddress;
			var result = GetClientsLogic.GetWhere(_filter);
			Assert.That(result, Is.EqualTo(String.Format(@"
	WHERE (LOWER(h.Street) like {0} or
	LOWER(l.ActualAdress) like {0})", ":SearchText")));
		}
	}
}