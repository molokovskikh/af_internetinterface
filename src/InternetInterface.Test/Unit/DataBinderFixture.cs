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

		[Test, Ignore("Чинить")]
		public void CanBindToGenericList()
		{
			var args = new NameValueCollection
			           	{
			           		{"lawyerPerson.Tariff", "sdfsd"},
			           		{"lawyerPerson.Name", "Test"},
			           		{"lawyerPerson.ShortName", "Test"},
			           		{"lawyerPerson.Telephone", "8-900-900-90-90"}
			           	};
			var instance = binder.BindObject(typeof (LawyerPerson), "lawyerPerson", builder.BuildSourceNode(args));
			Assert.IsTrue(binder.GetValidationSummary(instance).HasError);
			args["lawyerPerson.Tariff"] = "600";
			instance = binder.BindObject(typeof (LawyerPerson), "lawyerPerson", builder.BuildSourceNode(args));
			Assert.IsFalse(binder.GetValidationSummary(instance).HasError);
		}
	}
}
