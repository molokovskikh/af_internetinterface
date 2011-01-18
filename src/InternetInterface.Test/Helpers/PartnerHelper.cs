﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InternetInterface.Models;

namespace InternetInterface.Test.Helpers
{
	class PartnerHelper
	{
		public static Partner CreatePartner()
		{
			return new Partner
			       	{
			       		Adress = "testAdress",
						Email = "test@mail.ru",
						Login = "testLogin",
						Name = "megaTESTpartner",
						TelNum = "8-111-111-11-11"
			       	};
		}

		public static List<PartnerAccessSet> CreatePartnerAccessSet(List<int> rights, Partner partner)
		{
			var resulr = new List<PartnerAccessSet>();
			foreach (var right in rights)
			{
				if ((right == 4) && (!rights.Contains(1)))
				{
					rights.Add(1);
				}
				var newAccess = new PartnerAccessSet
				{
					AccessCat = AccessCategories.Find(right),
					PartnerId = partner
				};
				resulr.Add(newAccess);
				//newAccess.SaveAndFlush();
			}
			return resulr;
		}
	}
}
