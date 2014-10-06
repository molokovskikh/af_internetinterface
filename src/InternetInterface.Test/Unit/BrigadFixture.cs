using System;
using System.Linq;
using InternetInterface.Models;
using NUnit.Framework;

namespace InternetInterface.Test.Unit
{
	[TestFixture]
	public class BrigadFixture
	{
		[Test]
		public void CheckStandartConstructor()
		{
			var brigad = new Brigad();

			Assert.AreEqual(new TimeSpan(9, 0, 0), brigad.WorkBegin);
			Assert.AreEqual(new TimeSpan(18, 0, 0), brigad.WorkEnd);
			Assert.AreEqual(new TimeSpan(0, 30, 0), brigad.WorkStep);
		}

		[Test]
		public void CheckConstructorWhithName()
		{
			var brigad = new Brigad("123");

			Assert.AreEqual(new TimeSpan(9, 0, 0), brigad.WorkBegin);
			Assert.AreEqual(new TimeSpan(18, 0, 0), brigad.WorkEnd);
			Assert.AreEqual(new TimeSpan(0, 30, 0), brigad.WorkStep);
		}
	}
}