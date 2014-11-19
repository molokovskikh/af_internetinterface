using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using Test.Support.Selenium;

namespace InternetInterface.Test.Functional
{
	public class RoleBasedFixture : SeleniumFixture
	{
		protected string InitialPartnerLogin;
		protected Partner CurrentPartner;

		virtual protected int GetRoleId()
		{
			return 3;
		}
	
		[SetUp]
		public void SetUp()
		{
			InitialPartnerLogin = Environment.UserName;
			var initialPartner = Partner.GetPartnerForLogin(InitialPartnerLogin);
			CurrentPartner = PartnerHelper.CreatePartnerByRole(GetRoleId(), session);
			//проверяем есть ли необходимость в смене пользователя
			if (initialPartner.Role.Id != CurrentPartner.Role.Id)
				SwitchUser(CurrentPartner.Login);
		}

		protected void SwitchUser(string login)
		{
			Open(String.Format("Login/ChangeLoggedInPartner?login={0}&redirect={1}", login, "~/Map/SiteMap"));
		}

		[TearDown]
		public new void TearDown()
		{
			//проверяем есть ли необходимость в смене пользователя
			if(CurrentPartner.Login != InitialPartnerLogin)
				SwitchUser(InitialPartnerLogin);
		}
	}
}