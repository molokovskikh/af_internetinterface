using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using Inforoom2.Intefaces;
using Inforoom2.Models;
using NHibernate;
using NHibernate.Engine;
using NHibernate.Event;
using NHibernate.Mapping.ByCode;
using NHibernate.Persister.Entity;

namespace Inforoom2.Helpers
{
	public class ModelCrudListener : IPreUpdateEventListener, IPostInsertEventListener, IPreDeleteEventListener
	{
		[Description("Текущий пользователь")]
		public static Employee CurrentEmployee { get; private set; }

		/// <summary>
		/// Указание текущего пользователя, для отражения его действий в логах
		/// </summary>
		public static void SetEmployee(Employee currentUser)
		{
			CurrentEmployee = currentUser;
		}

		/// <summary>
		/// Обработка события перед обновлением модели
		/// </summary>
		/// <param name="preUpdate">результат события</param>
		/// <returns>veto</returns>
		public bool OnPreUpdate(PreUpdateEvent preUpdate)
		{
			CreatePreUpdateLog(preUpdate);
			CreatePreUpdateAppeal(preUpdate);
			return false;
		}

		/// <summary>
		/// Обработка события после добавления модели
		/// </summary>
		/// <param name="postInsert">результат события</param>
		/// <returns></returns>
		public void OnPostInsert(PostInsertEvent postInsert)
		{
			CreatePostInsertLog(postInsert);
		}

		/// <summary>
		/// Обработка события перед удалением  модели
		/// </summary>
		/// <param name="preDelete">результат события</param>
		/// <returns>veto</returns>
		public bool OnPreDelete(PreDeleteEvent preDelete)
		{
			CreatePreDeleteLog(preDelete);
			return false;
		}

		//////////////////////////| Реализация логики обработки событий|////////////////////////////

		/// <summary>
		/// Создание лога перед изменением модели (реализующей интерфейс ILogAppeal), 
		/// регистрация разницы прежнего и текущего состояний
		/// </summary>
		/// <param name="postUpdate">результат события</param>
		private void CreatePreUpdateAppeal(PreUpdateEvent postUpdate)
		{
			if ((postUpdate.Entity as Log != null) || (postUpdate.Entity as Appeal != null))
			{
				return;
			} 
			if (postUpdate.Entity.GetType().GetInterfaces().Any(s => s == typeof(ILogAppeal))) {
				//получаем необходимые данные о событии
				var currentObj = ((ILogAppeal)postUpdate.Entity);
				var session = postUpdate.Session;
				var oldState = postUpdate.OldState;
				var currentState = postUpdate.State;
				// объявляем нужные переменные
				Appeal appeal = null;
				var appealBase = currentObj.GetAppealClient(session);
				// реализуем логику обработки события
				if (appealBase != null) {
					string message = "";
					// получаем логируемые поля модели 
					var appealFields = currentObj.GetAppealFields();
					// получаем поля обновленной модели
					var propNames = postUpdate.Persister.PropertyNames;
					// фиксируем изменения
					for (int i = 0; i < propNames.Length; i++) {
						if (appealFields.Contains(propNames[i])) {
							appealFields.Remove(propNames[i]);
							if ((!(oldState.GetValue(i) == null && currentState.GetValue(i) == null) && (oldState.GetValue(i) == null || currentState.GetValue(i) == null))
							    || (!(oldState.GetValue(i) == null && currentState.GetValue(i) == null) && (!oldState.GetValue(i).Equals(currentState[i])))) {
								if (currentState.GetValue(i) != null && (currentState.GetValue(i) as BaseModel) != null) {
									// обработка полей, являющихся наследниками BaseModel,
									// чьи логируемые значения формируются внутри модели свои собственным методом 
									string changesText = currentObj.GetAdditionalAppealInfo(propNames[i], oldState.GetValue(i),session);
									if (changesText == "") {
										changesText = propNames.GetValue(i) + " было: " + (oldState.GetValue(i) == null ? "значение отсуствует" : oldState[i]).ToString() + " <br/> "
										              + propNames.GetValue(i) + " стало: " + (currentState.GetValue(i) == null ? "значение отсуствует" : currentState[i]).ToString() + " <br/> ";
									}
									message += changesText;
								}
								else {
									if ((currentState.GetValue(i) as IList) != null) {
										// обработка обычных полей
										message += propNames.GetValue(i) + " (список) было: " + (oldState.GetValue(i) == null ? "значение отсуствует" : 
											(oldState.GetValue(i) as IList).Count.ToString()).ToString() + " <br/> "
										           + propNames.GetValue(i) + " стало: " + (currentState.GetValue(i) == null ? "значение отсуствует" : 
												   (currentState.GetValue(i) as IList).Count.ToString()).ToString() + " <br/> ";
									}
									else {
										// обработка обычных полей
										message += propNames.GetValue(i) + " было: " + (oldState.GetValue(i) == null ? "значение отсуствует" : oldState[i]).ToString() + " <br/> "
										           + propNames.GetValue(i) + " стало: " + (currentState.GetValue(i) == null ? "значение отсуствует" : currentState[i]).ToString() + " <br/> ";
									}
								}
							}
						}
					}
					// если логируемые остались в списке, значит в обновленной модели их нет,
					// а значит, при заполнении логируемых полей, допустили ошибку.
					if (appealFields.Count > 0) {
						throw new Exception("У модели отсуствуют следующие поля: " + string.Join(",", appealFields));
					}
					// если существует логируемая разница между старой и новой моделями, созраняем ее в лог 
					if (message != "") {
						message = "Модель " + postUpdate.Entity.GetType().Name + (postUpdate.Entity.GetType().BaseType == typeof(BaseModel)
							? ", Id : " + ((BaseModel)postUpdate.Entity).Id : "") + " <br/> " + message;
						appeal = new Appeal() {
							Message = message,
							Client = appealBase,
							AppealType = AppealType.System,
							Employee = CurrentEmployee
						};
					}
				}
				// если лог сформирован, сохраняем его в БД
				if (appeal != null) {
					session.Save(appeal);
				}
			}
		}

		/// <summary>
		/// логирование модели перед обновлением
		/// </summary>
		/// <param name="postUpdate">результат события</param>
		private void CreatePreUpdateLog(PreUpdateEvent postUpdate)
		{
			if ((postUpdate.Entity as Log != null) || (postUpdate.Entity as Appeal != null))
			{
				return;
			}
			//получаем необходимые данные о событии 
			var currentObj = postUpdate.Entity;
			var session = postUpdate.Session;
			var oldState = postUpdate.OldState;
			var currentState = postUpdate.State;
			// объявляем нужные переменные
			Log appeal = null;
			// реализуем логику обработки события 
			string message = "";
			// получаем поля обновленной модели
			var propNames = postUpdate.Persister.PropertyNames;
			// фиксируем изменения
			for (int i = 0; i < propNames.Length; i++) {
				if ((!(oldState.GetValue(i) == null && currentState.GetValue(i) == null) && (oldState.GetValue(i) == null || currentState.GetValue(i) == null))
				    || (!(oldState.GetValue(i) == null && currentState.GetValue(i) == null) && (!oldState.GetValue(i).Equals(currentState[i])))) {
						if ((currentState.GetValue(i) as IList) != null)
						{
							// обработка обычных полей
							message += propNames.GetValue(i) + " (список) было: " + (oldState.GetValue(i) == null ? "значение отсуствует" :
								(oldState.GetValue(i) as IList).Count.ToString()).ToString() + " <br/> "
									   + propNames.GetValue(i) + " стало: " + (currentState.GetValue(i) == null ? "значение отсуствует" :
									   (currentState.GetValue(i) as IList).Count.ToString()).ToString() + " <br/> ";
						}
						else {
							// формируем значение моделей
							var pastTempState = oldState.GetValue(i) == null ? "значение отсуствует"
								: ((oldState.GetValue(i) as BaseModel) == null ? oldState[i] : (oldState.GetValue(i) as BaseModel).Id);
							var currentTempState = currentState.GetValue(i) == null ? "значение отсуствует"
								: ((currentState.GetValue(i) as BaseModel) == null ? currentState[i] : (currentState.GetValue(i) as BaseModel).Id);
							// формируем сообщение на основе значений моделей
							message += propNames.GetValue(i) + " было: " + (pastTempState).ToString() + " <br/> "
							           + propNames.GetValue(i) + " стало: " + (currentTempState).ToString() + " <br/> ";
						}
					
				}
			}
			// если существует логируемая разница между старой и новой моделями, созраняем ее в лог 
			if (message != "") {
				message = "Модель <strong>" + postUpdate.Entity.GetType().Name + (postUpdate.Entity.GetType().BaseType == typeof(BaseModel)
					? "</strong>, Id : <strong>" + ((BaseModel)postUpdate.Entity).Id : "") + "</strong> <br/> " + message;
				appeal = new Log() {
					Message = message,
					Type = LogEventType.Update,
					Employee = CurrentEmployee
				};
			}
			// если лог сформирован, сохраняем его в БД
			if (appeal != null) {
				session.Save(appeal);
			}
		}

		/// <summary>
		/// логирование модели после ее добавления
		/// </summary>
		/// <param name="preInsert">результат события</param>
		private void CreatePostInsertLog(PostInsertEvent preInsert)
		{
			if ((preInsert.Entity as Log != null) || (preInsert.Entity as Appeal != null))
			{
				return;
			}
			//получаем необходимые данные о событии 
			var currentObj = preInsert.State;
			var session = preInsert.Session;
			// объявляем нужные переменные
			Log log = null;
			// реализуем логику обработки события 
			string message = "";
			// получаем поля обновленной модели
			var propNames = preInsert.Persister.PropertyNames;
			// фиксируем изменения
			for (int i = 0; i < propNames.Length; i++) {
				if ((currentObj.GetValue(i) as IList) != null)
				{
					message += propNames.GetValue(i) + " (список) : " + (currentObj.GetValue(i) == null ? "значение отсуствует" : (currentObj.GetValue(i) as IList).Count.ToString()) + " <br/> ";
				}
				else {
					message += propNames.GetValue(i) + " : " + (currentObj.GetValue(i) == null ? "значение отсуствует" :
					((currentObj.GetValue(i) as BaseModel) == null ? currentObj[i] : (currentObj.GetValue(i) as BaseModel).Id)).ToString() + " <br/> ";
				}
				
			}
			// созраняем сообщение в лог 
			if (message != "") {
				message = "Модель <strong>" + preInsert.Entity.GetType().Name + (preInsert.Entity.GetType().BaseType == typeof(BaseModel)
					? "</strong>, Id : <strong>" + ((BaseModel)preInsert.Entity).Id : "") + "</strong> добавлена :<br/> " + message;
				log = new Log() {
					Message = message,
					Type = LogEventType.Insert,
					Employee = CurrentEmployee
				};
			}
			// если лог сформирован, сохраняем его в БД
			if (log != null) {
				session.Save(log);
			}
		}

		/// <summary>
		/// логирование модели перед ее удалением
		/// </summary>
		/// <param name="preDelete">результат события</param>
		private void CreatePreDeleteLog(PreDeleteEvent preDelete)
		{
			if ((preDelete.Entity as Log != null) || (preDelete.Entity as Appeal != null))
			{
				return;
			}
			//получаем необходимые данные о событии 
			var currentObj = preDelete.DeletedState;
			var session = preDelete.Session;
			// объявляем нужные переменные
			Log log = null;
			// реализуем логику обработки события 
			string message = "";
			// получаем поля обновленной модели
			var propNames = preDelete.Persister.PropertyNames;
			// фиксируем изменения
			for (int i = 0; i < propNames.Length; i++)
			{
				if ((currentObj.GetValue(i) as IList) != null)
				{
					message += propNames.GetValue(i) + " (список) : " + (currentObj.GetValue(i) == null ? "значение отсуствует" : (currentObj.GetValue(i) as IList).Count.ToString()) + " <br/> ";
				}
				else {
					message += propNames.GetValue(i) + " : " + (currentObj.GetValue(i) == null ? "значение отсуствует" :
						((currentObj.GetValue(i) as BaseModel) == null ? currentObj[i] : (currentObj.GetValue(i) as BaseModel).Id)).ToString() + " <br/> ";
				}
			}
			// созраняем сообщение в лог 
			if (message != "") {
				message = "Модель <strong>" + preDelete.Entity.GetType().Name + (preDelete.Entity.GetType().BaseType == typeof(BaseModel)
					? "</strong>, Id : <strong>" + ((BaseModel)preDelete.Entity).Id : "") + "</strong> удалена :<br/> " + message;
				log = new Log() {
					Message = message,
					Type = LogEventType.Delete,
					Employee = CurrentEmployee
				};
			}
			// если лог сформирован, сохраняем его в БД
			if (log != null) {
				session.Save(log);
			}
		}
	}
}