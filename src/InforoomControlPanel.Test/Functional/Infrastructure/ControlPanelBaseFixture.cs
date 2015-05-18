using System;
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
		public void Setup()
		{
			var adminName = Environment.UserName;
			var employee = DbSession.Query<Employee>().First(i => i.Login == adminName);
			Employee = employee;
			DefaultEmployeePassword =  ConfigurationManager.AppSettings["DefaultEmployeePassword"];
		}
	}
}
