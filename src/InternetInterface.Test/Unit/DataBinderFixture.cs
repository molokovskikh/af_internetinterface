using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Castle.Components.Binder;
using Castle.Components.Validator;
using InternetInterface.AllLogic;
using InternetInterface.Models;
using NUnit.Framework;

namespace InternetInterface.Test.Unit
{
	[TestFixture]
	public class DataBinderFixture
	{
		private IDataBinder binder;
		private TreeBuilder builder;

		[TestFixtureSetUp]
		public void Init()
		{
			builder = new TreeBuilder();
			binder = new DecimalValidateBinder();
			binder.Validator = new ValidatorRunner(new CachedValidationRegistry());
		}

		[Test]
		public void CanBindToGenericList()
		{
			var args = new NameValueCollection
			           	{
			           		{"lawyerPerson.Tariff", "dsfgsdfg"},
			           		{"lawyerPerson.Name", "Test"},
			           		{"lawyerPerson.ShortName", "Test"},
			           		{"lawyerPerson.Telephone", "8-900-900-90-90"}
			           	};
			//var paramsNode = GetParamsNode(expectedValue);
			//var myList = (LawyerPerson) binder.BindObject(typeof (LawyerPerson), "myList", paramsNode);
			var instance = binder.BindObject(typeof (LawyerPerson), "lawyerPerson", builder.BuildSourceNode(args));
			/*foreach(var e in binder.ErrorList)
				Console.WriteLine(e);*/
			Assert.IsFalse(binder.Validator.IsValid(instance));
			//Assert.AreEqual(expectedValue, myList);
		}
	}
}
