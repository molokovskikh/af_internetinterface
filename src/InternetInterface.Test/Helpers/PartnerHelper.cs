using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InternetInterface.Models;
using InternetInterface.Test.Functional;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Test.Helpers
{
	internal class PartnerHelper
	{
		public static Partner CreatePartner()
		{
			return new Partner {
				Adress = "testAdress",
				Email = "test@mail.ru",
				Login = "testLogin",
				Name = "megaTESTpartner",
				TelNum = "8-111-111-11-11"
			};
		}

		public static List<CategorieAccessSet> CreatePartnerAccessSet(List<int> rights, Partner partner)
		{
			var resulr = new List<CategorieAccessSet>();
			foreach (var right in rights) {
				if ((right == 4) && (!rights.Contains(1))) {
					rights.Add(1);
				}
				var newAccess = new CategorieAccessSet {
					AccessCat = AccessCategories.Find(right),
					Categorie = partner.Role
				};
				resulr.Add(newAccess);
			}
			return resulr;
		}

		public static Partner CreatePartnerByRole(int roleId, ISession session)
		{
			var role = session.Query<UserRole>().First(r => r.Id ==  roleId);
			int random = new Random().Next(1, 10000);
			string name = DateTime.Now.ToShortDateString() + random;
			var	partner = new Partner(role){
				Name = name,
				Login = name
			};
			session.Save(partner);
			return partner;
		}
	}
}