using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;
using System.Web;
using System.Web.Mvc;
using Inforoom2.Helpers;
using Inforoom2.Models;
using InforoomControlPanel.Controllers;
using NHibernate;
using NHibernate.Linq;

namespace InforoomControlPanel.Helpers
{
	public static class EmployeePermissionViewHelper
	{
		public static List<Tuple<string, string>> GetAllFormPermissions()
		{
			var pub = typeof (EmployeePermissionViewHelper.FormPermissions).GetFields().ToList();
			return
				pub.Where(s => s.Name.IndexOf("Block_") != -1)
					.Select(v => new Tuple<string, string>(v.Name, v.GetCustomAttributesData().FirstOrDefault().ConstructorArguments.FirstOrDefault().ToString().Replace("\"","")))
					.ToList();
		}

		public enum FormPermissions
		{
			[Description("Ссылка - Клиент")] Block_000000,
			[Description("Ссылка - Клиент - Список клиентов")] Block_000001,
			[Description("Ссылка - Клиент - Онлайн клиенты")] Block_000002,
			[Description("Ссылка - Клиент -  Обращения")] Block_000003,
			[Description("Ссылка - Клиент - Зарегистрировать физ. лицо")] Block_000004,
			[Description("Ссылка - Клиент - Зарегистрировать физ. лицо")] Block_000005,
			[Description("Ссылка - Клиент - Создать заявку на подключение")] Block_000006,
			[Description("Ссылка - Клиент - Заявки на подключение")] Block_000007,
			[Description("Ссылка - Клиент - Агенты")] Block_000008,
			[Description("Ссылка - Сервисные бригады")] Block_001000,
			[Description("Ссылка - Сервисные бригады - Сервисные инженеры")] Block_001001,
			[Description("Ссылка - Сервисные бригады - Сервисные заявки")] Block_001002,
			[Description("Ссылка - Сервисные бригады - График назначенных заявок")] Block_001003,
			[Description("Ссылка - Сервисные бригады - Подключения клиентов")] Block_001004,
			[Description("Ссылка - Контент")] Block_002000,
			[Description("Ссылка - Контент - Вопросы и ответы")] Block_002001,
			[Description("Ссылка - Контент - Новости")] Block_002002,
			[Description("Ссылка - Контент - Слайды")] Block_002003,
			[Description("Ссылка - Контент - Баннеры")] Block_002004,
			[Description("Ссылка - Контент - Прайс-листы")] Block_002005,
			[Description("Ссылка - Сотрудники")] Block_003000,
			[Description("Ссылка - Сотрудники - Сотрудники")] Block_003001,
			[Description("Ссылка - Сотрудники - Роли")] Block_003002,
			[Description("Ссылка - Сотрудники - Права")] Block_003003,
			[Description("Ссылка - Сотрудники - Работа сотрудников")]Block_003004,
			[Description("Ссылка - Сотрудники - Настройки работы сотрудников")]Block_003005, 
            [Description("Ссылка - Запросы пользователей")] Block_004000,
			[Description("Ссылка - Запросы пользователей - Запросы в техподдержку")] Block_004001,
			[Description("Ссылка - Запросы пользователей - Заявки на обратный звонок")] Block_004002,
			[Description("Ссылка - Тарифы")] Block_005000,
			[Description("Ссылка - Тарифы - Тарифы")] Block_005001,
			[Description("Ссылка - Тарифы - Тарифы HTML")] Block_005002,
			[Description("Ссылка - Тарифы - Смена по истечению срока")] Block_005003,
			[Description("Ссылка - Тарифы - ТВ каналы")] Block_005004,
			[Description("Ссылка - Тарифы - Группы ТВ каналов")] Block_005005,
			[Description("Ссылка - Тарифы - Протоколы для ТВ")] Block_005006,
			[Description("Ссылка - Тарифы - Скорость")] Block_005007,
			[Description("Ссылка - Тарифы - Настройки скидок")]Block_005008,
			[Description("Ссылка - Тарифы - Стоимость подключения фикс. Ip")]Block_005009,
            [Description("Ссылка - Коммутаторы")] Block_006000,
			[Description("Ссылка - Коммутаторы - Коммутаторы")] Block_006001,
			[Description("Ссылка - Коммутаторы - Узлы связи")] Block_006002,
			[Description("Ссылка - Коммутаторы - IP-пулы регионов")]Block_006003,
			[Description("Ссылка - Адреса")] Block_007000,
			[Description("Ссылка - Адреса - Адреса коммутаторов")] Block_007001,
			[Description("Ссылка - Адреса - Города")] Block_007002,
			[Description("Ссылка - Адреса - Регионы")] Block_007003,
			[Description("Ссылка - Адреса - Улицы")] Block_007004,
			[Description("Ссылка - Адреса - Дома")] Block_007005,
			[Description("Ссылка - Адреса - Подключенные Дома")] Block_007006,
			[Description("Ссылка - Адреса - Подключенные Улицы")] Block_007007,
			[Description("Ссылка - Оборудование")] Block_008000,
			[Description("Ссылка - Оборудование - Список оборудования")] Block_008001,
			[Description("Ссылка - Оборудование - Список клиентов")] Block_008002,
			[Description("Ссылка - Отчеты")] Block_009000,
			[Description("Ссылка - Отчеты - По клиентам")] Block_009001,
			[Description("Ссылка - Отчеты - По списаниям")] Block_009002,
			[Description("Ссылка - Отчеты - По платежам")] Block_009003,
			[Description("Ссылка - Отчеты - По банковским выпискам")]Block_009004,
			[Description("Ссылка - Отчеты - Платежи по регистраторам")]Block_009005,
			[Description("Ссылка - Отчеты - Логирование")]Block_009006,
			[Description("Блок - Клиент - Физ.лицо - Меню")] Block_010000,
			[Description("Блок - Клиент - Физ.лицо - Личная информация")] Block_010001,
			[Description("Блок - Клиент - Физ.лицо - Паспортные данные")] Block_010002,
			[Description("Блок - Клиент - Физ.лицо - Информация по подключению")] Block_010003,
			[Description("Блок - Клиент - Физ.лицо - Платежи")] Block_010004,
			[Description("Блок - Клиент - Физ.лицо - Абонентская плата")] Block_010005,
			[Description("Блок - Клиент - Физ.лицо - Контакты")] Block_010006,
			[Description("Блок - Клиент - Физ.лицо - Обращения клиента")] Block_010007,
			[Description("Блок - Клиент - Физ.лицо - Неопознанные звонки")] Block_010008,
			[Description("Блок - Клиент - Юр.лицо - Меню")] Block_020000,
			[Description("Блок - Клиент - Юр.лицо - Личная информация")] Block_020001,
			[Description("Блок - Клиент - Юр.лицо - ")] Block_020002,
			[Description("Блок - Клиент - Юр.лицо - Информация по заказам")] Block_020003,
			[Description("Блок - Клиент - Юр.лицо - Платежи")] Block_020004,
			[Description("Блок - Клиент - Юр.лицо - Абонентская плата")] Block_020005,
			[Description("Блок - Клиент - Юр.лицо - Контакты")] Block_020006,
			[Description("Блок - Клиент - Юр.лицо - Обращения клиента")] Block_020007,
			[Description("Блок - Клиент - Юр.лицо - Неопознанные звонки")] Block_020008,
			[Description("Настройки - Платежи - Полный интерфейс")] Block_900001
		}

		public static void GeneratePermissions(ISession dbSession,object currentController)
		{
			var controllers = currentController.GetType().Assembly.GetTypes().Where(i => i.IsSubclassOf(typeof(ControlPanelController))).ToList();
			foreach (var controller in controllers)
			{
				var methods = controller.GetMethods();
				var actions = methods.Where(i => i.ReturnType == typeof(ActionResult)).ToList();
				foreach (var action in actions)
				{
					var name = controller.Name + "_" + action.Name;
					var right = dbSession.Query<Permission>().FirstOrDefault(i => i.Name == name);
					if (right != null)
						continue;
					var newright = new Permission();
					newright.Name = name;
					var url = ((Controller)currentController).Url.Action(action.Name, controller.Name.Replace("Controller", ""));
					newright.Description = "Доступ к странице <a href='" + url + "'>" + name + "</a>";
					dbSession.Save(newright);
				}
				foreach (var item in EmployeePermissionViewHelper.GetAllFormPermissions())
				{
					var right = dbSession.Query<Permission>().FirstOrDefault(i => i.Name == item.Item1);
					if (right != null)
						continue;
					var newright = new Permission();
					newright.Name = item.Item1;
					newright.Description = item.Item2;
					dbSession.Save(newright);
				}
			}
		}
	}
}