using System.Linq;
using Inforoom2.Models;
using InforoomControlPanel.Test.Functional.infrastructure;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;

namespace InforoomControlPanel.Test.Functional.Admin
{
	class EditRoleFixture : AdminFixture
	{
		[Test, Description("Изменение роли")]
		public void EditRole()
		{
			Employee.Permissions.Clear();
			DbSession.SaveOrUpdate(Employee);
			Open("Admin/RoleList");
			var role = DbSession.Query<Role>().First(p => p.Name == "Admin");
			var targetRole = browser.FindElementByXPath("//td[contains(.,'" + role.Name + "')]");
			var row = targetRole.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn-success"));
			button.Click();
			//удаляем из выбранной роли право доступа к странице с адресами городов
			var roleEdit = DbSession.Query<Permission>().First(p => p.Name == "AddressController_CityList");
			var targetRoleEdit = browser.FindElementByXPath("//div[@class='col-sm-10 listItem gray'][contains(.,'" + roleEdit.Name + "')]");
			var rowPlanPrice = targetRoleEdit.FindElement(By.XPath(".."));
			var buttonPlanTransfers = rowPlanPrice.FindElement(By.CssSelector(".entypo-cancel-circled"));
			buttonPlanTransfers.Click();
			AssertText("Объект успешно изменен");
			browser.FindElementByCssSelector(".btn-green").Click();
			DbSession.Refresh(role);
			var permossionRole = role.Permissions.ToList().FirstOrDefault(i => i.Name == "AddressController_CityList");
			Assert.That(permossionRole, Is.Null, "Право у роли  должно удалиться и в базе данных");
			DbSession.Flush();
			ControlPanelTearDown();
			LoginForAdmin();   
			//заходим на страницу, которую удалили из прав доступа у роли
			Open("Address/CityList");
			AssertText("Вы попытались получить доступ к части системы, для которой у вас нет прав!");
			AssertText("Доступ запрещен");
		}
	}
}