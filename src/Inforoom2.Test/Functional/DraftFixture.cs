using Inforoom2.Helpers;
using Inforoom2.Models;
using NUnit.Framework;

namespace Inforoom2.Test.Functional
{
	[TestFixture]
	public class DraftFixture : BaseFixture
	{
		[SetUp]
		public void FixtureSetup()
		{
			Employee emp = new Employee();
			emp.Username = "test";
			var pass = PasswordHasher.Hash("test");
			emp.Username = pass.Hash;
			emp.Salt = pass.Salt;
			session.Save(emp);
			session.Flush();
		}

		[Test]
		public void DraftTest()
		{	
			Open();
			AssertText("Ваш город");
		}
	}
}