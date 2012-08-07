using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using NHibernate.ByteCode.Castle;
using NHibernate.Bytecode;
using NUnit.Framework;

namespace InternetInterface.Test.Unit
{
	[SetUpFixture]
	public class SetupFixture
	{
		[SetUp]
		public void Setup()
		{
			var provider = ((AbstractBytecodeProvider) NHibernate.Cfg.Environment.BytecodeProvider);
			provider.SetProxyFactoryFactory(typeof(ProxyFactoryFactory).AssemblyQualifiedName);

			InitializeContent.GetAdministrator = () => new Partner();
		}
	}
}