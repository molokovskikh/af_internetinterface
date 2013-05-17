using System.ComponentModel;

namespace InternetInterface.Models
{
	public enum SearchUserBy
	{
		[Description("Автоматически")] Auto,
		[Description("ФИО")] ByFio,
		[Description("Паспортные данные")] ByPassport,
		[Description("Номер счета")] SearchAccount,
		[Description("Внешний код клиента")] OuterClientCode,
		[Description("Номер телефона")] TelNum,
		[Description("Адрес")] Address
	}

	public enum TypeChangeBalance
	{
		[Description("Согласно тарифу")] ForTariff,
		[Description("Другая сумма")] OtherSumm,
	}

	public enum ForSearchClientType
	{
		[Description("Физические лица")] Physical,
		[Description("Юридические лица")] Lawyer,
		[Description("Все")] AllClients
	}

	public enum ConnectedType
	{
		[Description("Подключенных")] Connected,
		[Description("Не подключенных")] NoConnected,
		[Description("Всех")] AllConnected
	}

	public enum EndbledType
	{
		[Description("Подключенных")] Enabled,
		[Description("Не подключенных")] Disabled,
		[Description("Всех")] All
	}

	public class ClientTypeProperties
	{
		public ForSearchClientType Type { get; set; }

		public bool IsPhysical()
		{
			return Type == ForSearchClientType.Physical;
		}

		public bool IsLawyer()
		{
			return Type == ForSearchClientType.Lawyer;
		}

		public bool IsAll()
		{
			return Type == ForSearchClientType.AllClients;
		}
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

	public class EnabledTypeProperties
	{
		public EnabledTypeProperties()
		{
			Type = EndbledType.All;
		}

		public EndbledType Type { get; set; }

		public bool IsEnabled()
		{
			return Type == EndbledType.Enabled;
		}

		public bool IsDisabled()
		{
			return Type == EndbledType.Disabled;
		}

		public bool IsAll()
		{
			return Type == EndbledType.All;
		}
	}

	public class AppealTypeProperties
	{
		public AppealTypeProperties()
		{
			appealType = AppealType.All;
		}

		public AppealType appealType { get; set; }

		public bool IsUser()
		{
			return appealType == AppealType.User;
		}

		public bool IsSystem()
		{
			return appealType == AppealType.System;
		}

		public bool IsFeedBack()
		{
			return appealType == AppealType.FeedBack;
		}

		public bool IsAll()
		{
			return appealType == AppealType.All;
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

		public bool IsSearchByAddress()
		{
			return SearchBy == SearchUserBy.ByPassport;
		}

		public bool IsSearchAccount()
		{
			return SearchBy == SearchUserBy.SearchAccount;
		}

		public bool IsSearchOuterClientCode()
		{
			return SearchBy == SearchUserBy.OuterClientCode;
		}

		public bool IsSearchTelephone()
		{
			return SearchBy == SearchUserBy.TelNum;
		}
	}
}