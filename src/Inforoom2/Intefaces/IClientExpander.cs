using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inforoom2.Models;

namespace Inforoom2.Intefaces
{
	public interface IClientExpander
	{
		/// <summary>
		/// Получить расширенного клиента ( Физ. / Юр. лицо )
		/// </summary>
		/// <returns>расширенный клиент ( Физ. / Юр. лицо ) </returns>
		object GetExtendedClient { get; }

		/// <summary>
		/// Получить все контакты клиента
		/// </summary>
		/// <returns>контакты клиента</returns>
		IList<Contact> GetContacts();

		/// <summary>
		/// Получить адрес подключения клиента
		/// </summary>
		/// <returns>адрес подключения клиента</returns>
		string GetConnetionAddress();

		/// <summary>
		/// Получить ФИО клиента / сокращенное наименование организации
		/// </summary>
		/// <returns>ФИО клиента / сокращенное наименование организации</returns>
		string GetName();
		
		/// <summary>
		/// Получить дату регистрации договора с клиентом
		/// </summary>
		/// <returns>дата регистрации договора с клиентом</returns>
		DateTime? GetRegistrationDate();

		/// <summary>
		/// Получить дату расторжения договора с клиентом
		/// </summary>
		/// <returns>дата расторжения договора с клиентом</returns>
		DateTime? GetDissolveDate();

		/// <summary>
		/// Получить тариф клиента
		/// </summary>
		/// <returns>тариф клиента</returns>
		string GetPlan();

		/// <summary>
		/// Получить текущий баланс лицевого счета клиента
		/// </summary>
		/// <returns>баланс лицевого счета клиента</returns>
		decimal GetBalance();

		/// <summary>
		/// Получить текущий статус клиента
		/// </summary>
		/// <returns></returns>
		StatusType GetStatus();
	}
}