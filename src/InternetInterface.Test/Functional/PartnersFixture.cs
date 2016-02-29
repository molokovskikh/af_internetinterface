using System;
using System.Linq;
using InternetInterface.Models;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium.Support.UI;
using Test.Support.Selenium;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class PartnersFixture : SeleniumFixture
	{
		[Test, Ignore("Функционал перенесен в новую админку.")]
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

		[Test(Description = "Проверка правильности сортировки списка агентов в Платежах")]
		public void CheckAgentsListSorting()
		{
			// Создание 2-х партнеров-дилеров для отображения в списке регистраторов
			var role = session.Get<UserRole>((uint)1);
			if (role == null) {
				role = new UserRole {
					ReductionName = "Diller"
				};
				session.Save(role);
			}
			var partner1 = new Partner("agent1", role) {
				Name = "Агент_первый",
			};
			session.Save(partner1);
			var partner2 = new Partner("agent2", role) {
				Name = "Агент_второй",
			};
			session.Save(partner2);

			Open("Payers/AgentFilter");
			var agentsSelect = Css("#filter_Agent_Id") as SelectElement;

			// Проверка наличия списка "agentsSelect" на странице
			Assert.IsNotNull(agentsSelect);

			var item = agentsSelect.Options.First(pr => pr.Text == partner1.Name);
			var partner1Id = agentsSelect.Options.IndexOf(item);
			item = agentsSelect.Options.First(pr => pr.Text == partner2.Name);
			var partner2Id = agentsSelect.Options.IndexOf(item);

			// Проверка того, что в списке "agentsSelect" partner1 находится позже partner2
			Assert.IsTrue(partner1Id > partner2Id);
		}
	}
}