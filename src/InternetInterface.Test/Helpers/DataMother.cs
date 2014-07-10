using InternetInterface.Models;
using NHibernate;

namespace InternetInterface.Test.Helpers
{
	public class DataMother
	{
		private ISession session;

		public DataMother(ISession session)
		{
			this.session = session;
		}

		public Client PhysicalClient()
		{
			return ClientHelper.Client(session);
		}
	}
}