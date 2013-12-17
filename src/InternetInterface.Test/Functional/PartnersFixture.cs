using System;
using System.Linq;
using InternetInterface.Models;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.Selenium;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class PartnersFixture : SeleniumFixture
	{
		[Test]
		public void Create_service_engineer()
		{
			Open("Map/SiteMap");
			Click("Администрирование");
			Click("Сервисные инженеры");
			Click("Зарегистрировать партнера");
			AssertText("Регистрация партнера");
			Css("#Partner_Name").SendKeys(Guid.NewGuid().ToString());
			Css("#Partner_Login").SendKeys(Guid.NewGuid().ToString());
			Click("Сохранить");
			AssertText("Информация для партнера");
		}

		[Test]
		public void Edit_work_hours()
		{
			var role = session.Query<UserRole>().First(r => r.ReductionName == "Service");
			var partner = new Partner(role);
			partner.Name = Guid.NewGuid().ToString();
			partner.Login = Guid.NewGuid().ToString();
			session.Save(partner);

			Open("Partners/Edit?id={0}", partner.Id);
			Css("#Partner_WorkBegin").Clear();
			Css("#Partner_WorkBegin").SendKeys("11:00");
			Click("Сохранить");
			AssertText("Сохранено");

			session.Refresh(partner);
			Assert.AreEqual(new TimeSpan(11, 00, 00), partner.WorkBegin);
		}
	}
}