using System;
using System.Reflection;
using System.Text;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using System.Configuration;
using InternetInterface.Controllers;
using InternetInterface.Models;
using log4net.Config;
using NUnit.Framework;
using WatiN.Core;


namespace InternetInterfaceFixtute.RegisterTest
{
	[TestFixture]
	public class SearchFixture : SearchController
	{

	/*	[]
		public void Setup()
		{

		}*/
		[TestFixtureSetUp]
		public void Setup()
		{
			XmlConfigurator.Configure();
			if (!ActiveRecordStarter.IsInitialized)
				ActiveRecordStarter.Initialize(new[] {
					Assembly.Load("InternetInterface"),
					Assembly.Load("InternetInterface.Test"),
				}, ActiveRecordSectionHandler.Instance);
		}

		public static string BuildTestUrl(string urlPart)
		{
			return String.Format("http://localhost:{0}/{1}",
								 ConfigurationManager.AppSettings["webPort"],
								 urlPart);
		}

		protected IE Open(string uri)
		{
			return new IE(BuildTestUrl(uri));
		}

		[Test]
		public void SearchTets()
		{
			//GetClients(new UserSearchProperties { SearchBy = SearchUserBy.Auto }, 4, 1, "");
			//var browser = Open()
		}


		public static string ToBinaryNumberString(ulong value)
		{
			int i = 8 >> 2;
            int bit_count = sizeof(ulong) ;
            StringBuilder sb = new StringBuilder(bit_count);
            for (int bit = bit_count - 1; bit >= 0; --bit)
            {
                switch ((value >> bit) & 1UL)
                {
                    case 0:
                        sb.Append('0');
                        break;
                    case 1:
                        sb.Append('1');
                        break;
                }
               
            }
            return sb.ToString();
        }
 

		[Test]
		public void BinaryTest()
		{
			string value = "123";
			string binary = ToBinaryNumberString(Convert.ToUInt64(value));
			char[] strValue = binary.ToCharArray();
			for (int i = 1; i < strValue.Length - 1; i++)
			{
				if (strValue[i - 1] == '0' && strValue[i + 1] == '0')
				{
					strValue[i] = '0';
				}
				if (strValue[i - 1] == '1' && strValue[i + 1] == '1')
				{
					strValue[i] = '1';
				}
			}
		}
	}
}