using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using Inforoom2.Models;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;

namespace Inforoom2.Test.Functional.infrastructure.Helpers
{
	/// <summary>
	/// Разбивает список Клиентов (строк) на количество частей указанных в ClientProppertyType; 
	/// Количество элементов списока всегда должен быть достаточным. (чтобы кто-то не остался без ФИО)
	/// </summary>
	public class ClientCreateHelper
	{
		/// <summary>
		/// список клиентов
		/// </summary>
		private List<string> clients = new List<string>();

		private int currentIndex = 0;

		/// <summary>
		/// метки для разделения ФИО (возможно в перспективе и другой инф.)
		/// </summary> 
		private enum ClientProppertyType
		{
			name,
			surname,
			patronymic
		}

		/// <summary>
		/// Перечисление с метками, использующееся для определения ролей клиентов в тесте.
		/// Пример получения Описания метки:
		/// var clientMark = ;
		/// </summary>
		public enum ClientMark
		{
			[Description("нормальный клиент")] normalClient,

			[Description("без паспортных данных")] nopassportClient,

			[Description("заблокированный клиент")] disabledClient,

			[Description("неподключенный клиент")] unpluggedClient,

			[Description("клиент с низким балансом")] lowBalanceClient,

			[Description("клиент заблокированный по сервисной заявке")] servicedClient,

			[Description("клиент с услугой добровольной блокировки")] frozenClient,

			[Description("клиент c тарифом, игнорирующим скидку")] ignoreDiscountClient,

			[Description("клиент с тарифным планом, который закреплен за регионом")] clientWithRegionalPlan ,

			[Description("новый подключенный клиент,с недавней датой регистрации")] recentClient ,
		}

		public int Index
		{
			get { return currentIndex; }
			private set { currentIndex = value; }
		}

		public string Name
		{
			get { return clients[Index].Split(',')[(int)ClientProppertyType.name]; }
		}

		public string Surname
		{
			get { return clients[Index].Split(',')[(int)ClientProppertyType.surname]; }
		}

		public string Patronymic
		{
			get { return clients[Index].Split(',')[(int)ClientProppertyType.patronymic]; }
		}

		/// <summary>
		///  добавление ФИО (возможно в перспективе и другой инф.) в список clients
		/// </summary>
		public ClientCreateHelper()
		{
			clients.Add("Николай,Третьяков,Иванович");
			clients.Add("Владислав,Савинов,Петрович");
			clients.Add("Алексей,Дулин,Иванов");
			clients.Add("Николай,Серов,Викторович");
			clients.Add("Виктор,Трешкин,Дмитриевич");
			clients.Add("Андрей,Евтеев,Владимирович");
			clients.Add("Владимир,Тоцкий,Дмитриевич");
			clients.Add("Петр,Свиблов,Львович");
			clients.Add("Валентин,Гречкин,Константинович");
			clients.Add("Дмитрий,Иванов,Алексеевич");
		}

		/// <summary>
		/// получение следующего клиента
		/// </summary>
		/// <returns>false по выходу индекса за пределы массива</returns>
		public bool GetNextClient()
		{
			Index++;
			if (clients.Count > Index) {
				return true;
			}
			Index = 0;
			return false;
		}

		/// <summary>
		///  Маркеровка клиентов
		/// </summary>
		/// <param name="client">Маркеруемый клиент</param>
		/// <param name="mark">Маркер</param>
		public void MarkClient(Client client, ClientMark mark)
		{
			client.Comment = mark.GetDescription();
			client.PhysicalClient.Patronymic = mark.GetDescription();
		}
	}
}