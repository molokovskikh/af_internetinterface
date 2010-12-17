using System.ComponentModel;


namespace InternetInterface.Models
{
	public enum SearchUserBy
	{
		[Description("Автоматически")]
		Auto,
		[Description("ФИО")]
		ByFio,
		[Description("Паспортные данные")]
		ByPassportSet,
		[Description("Логин клиента")]
		ByLogin,
		[Description("Номер счета")]
		SearchAccount
	}

	public enum TypeChangeBalance
	{
		[Description("Согласно тарифу")]
		ForTariff,
		[Description("Другая сумма")]   
		OtherSumm,
	}

	public enum ConnectedType
	{
		[Description("Подключенных")]
		Connected,
		[Description("Не подключенных")]
		NoConnected,
		[Description("Всех")]
		AllConnected
	}

	public class ConnectedTypeProperties
	{
		public ConnectedType Type { get; set; }

		public bool IsConnected()
		{
			return Type == ConnectedType.Connected;
		}

		public bool IsNoConnected()
		{
			return Type == ConnectedType.NoConnected;
		}

		public bool IsAllConnected()
		{
			return Type == ConnectedType.AllConnected;
		}
	}

	public class ChangeBalaceProperties
	{
		public TypeChangeBalance ChangeType { get; set; }

		public bool IsForTariff()
		{
			return ChangeType == TypeChangeBalance.ForTariff;
		}

		public bool IsOtherSumm()
		{
			return ChangeType == TypeChangeBalance.OtherSumm;
		}
	}

	public class UserSearchProperties
	{
		public string SearchText;

		public SearchUserBy SearchBy { get; set; }

		public bool IsSearchAuto()
		{
			return SearchBy == SearchUserBy.Auto;  
		}

		public bool IsSearchByFio()
		{
			return SearchBy == SearchUserBy.ByFio;
		}

		public bool IsSearchByPassportSet()
		{
			return SearchBy == SearchUserBy.ByPassportSet;
		}

		public bool IsSearchByLogin()
		{
			return SearchBy == SearchUserBy.ByLogin;
		}

		public bool IsSearchAccount()
		{
			return SearchBy == SearchUserBy.SearchAccount;
		}
	}
}
