using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom2.Models;
using Inforoom2.Test.Infrastructure;
using InternetInterface.Models;
using NHibernate.Linq;
using NUnit.Framework;
using InforoomControlPanel;
using InternetInterface.Helpers;

namespace InforoomControlPanel.Test.Functional.infrastructure
{
	public class ControlPanelBaseFixture : BaseFixture
	{
		protected Employee Employee;
		protected string DefaultEmployeePassword;

		[SetUp]
		public void ControlPanelSetUp()
		{
			SystemTime.Now = ()=> DateTime.Now;
			var adminName = Environment.UserName;
			var employee = DbSession.Query<Employee>().First(i => i.Login == adminName);
			Employee = employee;
			//Добавление прав
			Call(BuildTestUrl("AdminOpen/RenewActionPermissionsJs"));
			var permissions = DbSession.Query<Permission>().ToList();
			foreach (var item in permissions) {
				employee.Permissions.Add(item);
			}
			DbSession.Save(employee);
			DbSession.Flush();
			//Авторизация
			DefaultEmployeePassword = ConfigurationManager.AppSettings["DefaultEmployeePassword"];
			LoginForAdmin();
		}

		[TearDown]
		public void ControlPanelTearDown()
		{
			WaitForVisibleCss("#logoutLink");
			Css("#logoutLink").Click();
			CloseAllTabsButOne();
		}

		public void LoginForAdmin()
		{
			Open();
			WaitForVisibleCss("#username", 60);
			Css("#username").SendKeys(Employee.Login);
			Css("#password").SendKeys("1234");
			Css(".btn-login").Click();
			AssertText(Employee.Name);
        }
        protected void UpdateDriverSideSystemTime()
        {
            var time = SystemTime.Now();
            Open($"AdminOpen/SetDebugTime?time={time}");
            WaitForText($"Время установлено {time}", 20);
        }
    }
}