using System;
using System.Linq;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class ClientFixture : HeadlessFixture
	{
		[SetUp]
		public void Setup()
		{
			var partner = session.Query<Partner>().First(p => p.Login == Environment.UserName);
			partner.ShowContractOfAgency = true;
		}

		[TearDown]
		public void TearDown()
		{
			var partner = session.Query<Partner>().First(p => p.Login == Environment.UserName);
			partner.ShowContractOfAgency = false;
		}

		[Test]
		public void Show_contract_of_agency()
		{
			var client = ClientHelper.Client(session);
			session.Save(client);
			Open(string.Format("UserInfo/SearchUserInfo?filter.ClientCode={0}", client.Id));
			AssertText("Информация по клиенту");
			Input("BalanceText", "500");
			ClickButton("Пополнить баланс");
			AssertText("ДОГОВОР ПОРУЧЕНИЯ");
		}
	}
}