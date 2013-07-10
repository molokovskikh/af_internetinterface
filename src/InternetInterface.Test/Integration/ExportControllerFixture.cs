using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Castle.MonoRail.TestSupport;
using ExcelLibrary.SpreadSheet;
using InternetInterface.Controllers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Queries;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;

namespace InternetInterface.Test.Integration
{
	[TestFixture]
	public class ExportControllerFixture : ControllerFixture
	{
		protected ExportController Controller;

		[SetUp]
		public void Setup()
		{
			Controller = new ExportController();
			PrepareController(Controller);
		}

		[Test]
		public void GetClientsInExcelTest()
		{
			Response.Output = new StringWriter(new StringBuilder());
			Controller.GetClientsInExcel(new SeachFilter());
			Assert.That(Response.OutputStream.Length, Is.GreaterThan(0));
			Response.OutputStream.Seek(0, SeekOrigin.Begin);
			var wb = Workbook.Load(Response.OutputStream);
			Assert.That(wb.Worksheets[0].Name, Is.StringContaining("Статистика по клиентам"));
		}
	}
}
