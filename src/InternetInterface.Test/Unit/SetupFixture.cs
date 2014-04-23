using System.Collections.Generic;
using Common.Web.Ui.NHibernateExtentions;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using NUnit.Framework;

namespace InternetInterface.Test.Unit
{
	[SetUpFixture]
	public class SetupFixture
	{
		[SetUp]
		public void Setup()
		{
			NHibernateHelper.InitProxy();
			InitializeContent.GetPartner = () => new Partner { AccesedPartner = new List<string>() };
		}
	}
}