﻿using System;
using System.Collections.Generic;
using System.Linq;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;

namespace InternetInterface.Test.Functional
{
	public class SwitchLoginFixture : RoleBasedFixture
	{
		override protected int GetRoleId()
		{
			return 1;
		}
		
		[Test(Description = "Тестируется поведение базового класса и функции SwitchUser.")]
		public void SwitchUserTest()
		{
			var partner = PartnerHelper.CreatePartnerByRole(3, session);
			SwitchUser(partner.Login);
			AssertText("Администрирование");
			SwitchUser(CurrentPartner.Login);
			AssertNoText("Администрирование");
		}
	}
}