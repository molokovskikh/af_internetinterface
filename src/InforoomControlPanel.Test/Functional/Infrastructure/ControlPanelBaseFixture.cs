using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Common.Tools;
using Inforoom2.Models;
using Inforoom2.Test.Infrastructure;
using NHibernate.Linq;
using NUnit.Framework;

namespace InforoomControlPanel.Test.Functional.infrastructure
{
	public class ControlPanelBaseFixture : BaseFixture
	{
		protected Employee Employee;
		protected string DefaultEmployeePassword;

		[SetUp]
		public void ControlPanelSetUp()
		{
			SystemTime.Now = () => DateTime.Now;
			Employee = DbSession.Query<Employee>().First(i => i.Login == Environment.UserName);
			//Добавление прав
			foreach (var item in DbSession.Query<Permission>().ToList())
				Employee.Permissions.Add(item);
			DbSession.Save(Employee);
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