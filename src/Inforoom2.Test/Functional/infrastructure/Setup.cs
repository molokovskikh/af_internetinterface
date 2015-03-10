using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using CassiniDev;
using Common.Web.Ui.NHibernateExtentions;
using Inforoom2.Components;
using Inforoom2.Helpers;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using InternetInterface.Helpers;
using MvcContrib.UI.InputBuilder.Conventions;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping.Attributes;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NUnit.Framework;
using Rhino.Mocks.Constraints;
using Test.Support.Selenium;
using Address = Inforoom2.Models.Address;
using Switch = Inforoom2.Models.Switch;

namespace Inforoom2.Test.Functional
{
	[SetUpFixture]
	public class Setup
	{
		private Server _webServer;

		[SetUp]
		public void SetupFixture()
		{
			//Все опасные функции, должны быть вызванны до этого момента, так как исключения в сетапе
			//оставляют невысвобожденные ресурсы браузера и веб сервера
			SeleniumFixture.GlobalSetup();
			_webServer = SeleniumFixture.StartServer();
			
		}
		[TearDown]
		public void TeardownFixture()
		{
			SeleniumFixture.GlobalTearDown();
			_webServer.ShutDown();
		}
	}
}