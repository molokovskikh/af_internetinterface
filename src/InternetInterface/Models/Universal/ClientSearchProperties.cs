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
	}

	public enum TypeChangeBalance
	{
		[Description("Согласно тарифу")]
		ForTariff,
		[Description("Другая сумма")]   
		OtherSumm,
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

	}
}
