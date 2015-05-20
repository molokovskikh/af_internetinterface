using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
			if (value != null && value is Inforoom2.Models.PhysicalClient) {
				var physic = (Inforoom2.Models.PhysicalClient)value;
				if (physic.Id == 0) {
					var dbSession = MvcApplication.SessionFactory.GetCurrentSession();
					if (dbSession.Query<Models.PhysicalClient>().Any(s => s.Name == physic.Name &&
					                                                      s.Surname == physic.Surname && s.Patronymic == physic.Patronymic)) {
						AddError("<p class='msg'><strong>Клиент с подобным ФИО уже зарегистрирован!</strong>" +
								 @"<div class='form-group'>
										<label class='col-sm-8 control-label c-pointer' for='scapeUserNameDoubling'>Разрешить дублирование ФИО:</label>
										<div class='col-sm-4'>
			<input id='scapeUserNameDoubling' class='c-pointer' data-val='true' data-val-required='Требуется поле Checked.' name='scapeUserNameDoubling' type='checkbox' value='true'> 
			<input name='scapeUserNameDoubling' type='hidden' value='false'>
										</div>
									</div>" +
						         "</p>");
					}
				}
			}
		}
	}
}