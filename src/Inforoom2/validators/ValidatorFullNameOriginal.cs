using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Inforoom2.Helpers;
using InternetInterface.Models;
using NHibernate.Linq;
using Contact = Inforoom2.Models.Contact;

namespace Inforoom2.validators
{
	//Валидация Физического клиента
	public class ValidatorPhysicalClient : CustomValidator
	{
		protected override void Run(object value)
		{
			//Проверка уже существующего ФИО при регистрации
			if (value != null && value is Inforoom2.Models.PhysicalClient)
			{
				var physic = (Inforoom2.Models.PhysicalClient)value;
				if (physic.Id == 0)
				{
					// формируем шаблон и часть ссылки
					string hrefItem = "<a href='{0}{1}' target= '_blank'>ЛС {1}</a>";
					string urlToClientInfo = ConfigHelper.GetParam("adminPanelOld") + "UserInfo/ShowPhysicalClient?filter.ClientCode=";
					// получаем текущую сессию
					var dbSession = MvcApplication.SessionFactory.GetCurrentSession();
					// поиск совпадений по ФИО, формирование списка ссылок по найденным совпадениям
					var doubleNameList = dbSession.Query<Models.PhysicalClient>().Where(s => s.Name.ToLower().Replace(" ", "") == physic.Name.ToLower().Replace(" ", "")
																							 && s.Surname.ToLower().Replace(" ", "") == physic.Surname.ToLower().Replace(" ", "")
																							 && s.Patronymic.ToLower().Replace(" ", "") == physic.Patronymic.ToLower().Replace(" ", ""))
						.Select(s => string.Format(hrefItem, urlToClientInfo, s.Client.Id)).ToList();
					// если найдены совпадения
					if (doubleNameList.Count > 0)
					{
						// добавляем checkbox в сообщение валидатора
						AddError(string.Format("<p class='msg'><strong>Клиент с подобным ФИО уже зарегистрирован!</strong><br>{0}" +
											   @"<div class='form-group'> 
										<label class='col-sm-8 control-label c-pointer' for='scapeUserNameDoubling'>Разрешить дублирование ФИО:</label>
										<div class='col-sm-4'>
			<input id='scapeUserNameDoubling' class='c-pointer' data-val='true' data-val-required='Требуется поле Checked.' name='scapeUserNameDoubling' type='checkbox' value='true'> 
			<input name='scapeUserNameDoubling' type='hidden' value='false'>
										</div> 
									</div>
								</p>", string.Join("<span style='color:black;'>, </span>", doubleNameList)));
					}
				}
			}
		}
	}
}