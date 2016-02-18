using System;
using NHibernate.Cfg;

namespace Inforoom2.Helpers
{
	public class TableNamingStrategy : INamingStrategy
	{
		public string ClassToTableName(string className)
		{
			return className;
		}

		public string PropertyToColumnName(string propertyName)
		{
			return propertyName;
		}

		public string TableName(string tableName)
		{
			tableName = tableName.ToLower();
			if (tableName == "PhysicalClients".ToLower()
			    || tableName == "Appeals".ToLower()
			    || tableName == "Tariffs".ToLower()
			    || tableName == "Regions".ToLower()
			    || tableName == "Services".ToLower()
			    || tableName == "ClientServices".ToLower()
			    || tableName == "Clients".ToLower()
			    || tableName == "UserWriteOffs".ToLower()
			    || tableName == "StatusCorrelation".ToLower()
			    || tableName == "Status".ToLower()
			    || tableName == "AdditionalStatus".ToLower()
			    || tableName == "LawyerPerson".ToLower()
			    || tableName == "InternetSettings".ToLower()
			    || tableName == "Leases".ToLower()
			    || tableName == "SaleSettings".ToLower()
			    || tableName == "ClientEndpoints".ToLower()
			    || tableName == "NetworkSwitches".ToLower()
			    || tableName == "PaymentForConnect".ToLower()
			    || tableName == "StaticIps".ToLower()
			    || tableName == "WriteOff".ToLower()
			    || tableName == "Requests".ToLower()
			    || tableName == "Partners".ToLower()
			    || tableName == "Payments".ToLower()
			    || tableName == "ServiceRequest".ToLower()
			    || tableName == "ConnectBrigads".ToLower()
			    || tableName == "Contacts".ToLower()
				|| tableName == "IpPoolRegions".ToLower()
				|| tableName == "ippools".ToLower()
				|| tableName == "PackageSpeed".ToLower()
			    || tableName == "MessagesForClients".ToLower()
			    || tableName == "issues".ToLower()
			    || tableName == "serviceiterations".ToLower()
			    || tableName == "Orders".ToLower()
				|| tableName == "Internetsessionslogs".ToLower()
				|| tableName == "clientendpointinternetlogs".ToLower()
				|| tableName == "OrderServices".ToLower()
				|| tableName == "UnresolvedPhone".ToLower()
				|| tableName == "bankpayments".ToLower()
				|| tableName == "NetworkZones".ToLower()
				|| tableName == "Recipients".ToLower()
				|| tableName == "PaymentsForAgent".ToLower()
				|| tableName == "AgentTariffs".ToLower())
			{
				
                return tableName;
			}
			return "inforoom2_" + tableName;
		}

		public string ColumnName(string columnName)
		{
			return columnName;
		}

		public string PropertyToTableName(string className, string propertyName)
		{
			throw new NotImplementedException();
		}

		public string LogicalColumnName(string columnName, string propertyName)
		{
			throw new NotImplementedException();
		}
	}
}