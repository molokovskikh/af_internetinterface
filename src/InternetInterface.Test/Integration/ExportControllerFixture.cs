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
using NHibernate.Linq;
using NUnit.Framework;

namespace InternetInterface.Test.Integration
{
	[TestFixture]
	public class ExportControllerFixture : SessionControllerFixture
	{
		protected ExportController Controller;

		[SetUp]
		public void Setup()
		{
			Controller = new ExportController();
			PrepareController(Controller);
			var partner = session.Query<Partner>().First();
			partner.AccesedPartner = new List<string>();
			InitializeContent.GetAdministrator = () => partner;
		}


		[Test]
		public void GetClientsInExcelTest()
		{
			Controller.GetClientsInExcel(new SeachFilter());
			Assert.That(Response.OutputStream.Length, Is.GreaterThan(0));
			Response.OutputStream.Seek(0, SeekOrigin.Begin);
			var wb = Workbook.Load(Response.OutputStream);
			Assert.That(wb.Worksheets[0].Name, Is.StringContaining("Статистика по клиентам"));
		}
	}
}
