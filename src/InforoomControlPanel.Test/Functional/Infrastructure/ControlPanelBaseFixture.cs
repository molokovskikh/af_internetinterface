﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Inforoom2.Models;
using Inforoom2.Test.Infrastructure;
using InternetInterface.Models;
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
			var adminName = Environment.UserName;
			var employee = DbSession.Query<Employee>().First(i => i.Login == adminName);
			Employee = employee;
			DefaultEmployeePassword =  ConfigurationManager.AppSettings["DefaultEmployeePassword"];
			LoginForAdmin();
		}

		[TearDown]
		public void ControlPanelTearDown()
		{
			Css(".entypo-logout.right").Click();
		}
		public void LoginForAdmin()
		{
			Css("#username").SendKeys(Employee.Login);
			Css("#password").SendKeys("1234");
			Css(".btn-login").Click();
			AssertText(Employee.Name);
		}
	}
}
