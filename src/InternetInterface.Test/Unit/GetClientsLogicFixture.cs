﻿using System;
using System.Collections.Generic;
using System.Linq;
using InternetInterface.AllLogic;
using InternetInterface.Models;
using InternetInterfaceFixture.Helpers;
using NUnit.Framework;


namespace InternetInterfaceFixture.Unit
{
	[TestFixture]
	class GetClientsLogicFixture : WatinFixture
	{
		private static bool ResultAssert(Func<IList<PhisicalClients>> result)
		{
			var _result = result();
			if (result != null)
			{
				if (_result.Count != 0)
				{
					if (!_result.First().Connected)
						return true;
					if (result.Method.Name == "GetClientsTest")
						return true;
				}
			}
			throw new ArgumentException("Не найден клиент");
		}


		[Test]
		public void GetClientsForCloseDemandTest()
		{
			ResultAssert(GetClientsLogic.GetClientsForCloseDemand);
		}

		[Test]
		public void GetClientsTest()
		{
			ResultAssert(() => GetClientsLogic.GetClients(UserSearchPropertiesHelper.CreateUserSearchProperties(),
			                                                           ConnectedTypePropertiesHelper.CreateUserSearchProperties(), 0,
			                                                           0,
			                                                           string.Empty, 0));
		}

		[Test]
		public void GetWhereTest()
		{
			var query = GetClientsLogic.GetWhere(UserSearchPropertiesHelper.CreateUserSearchProperties(),
			                                     ConnectedTypePropertiesHelper.CreateUserSearchProperties(),
			                                     0, 0, "Test", 0);
			Assert.That(query, Is.StringContaining(@"WHERE LOWER(P.Name) like :SearchText or LOWER(P.Surname) like :SearchText
or LOWER(P.Patronymic) like :SearchText or LOWER(P.City) like :SearchText 
or LOWER(P.PassportSeries) like :SearchText
or LOWER(P.PassportNumber) like :SearchText or LOWER(P.WhoGivePassport) like :SearchText
or LOWER(P.RegistrationAdress) like :SearchText or LOWER(P.Login) like :SearchText"));
		}
	}
}
