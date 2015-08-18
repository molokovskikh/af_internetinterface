using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web;
using Inforoom2.Components;
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
		/// Формирование дополнительного сообщения об ошибке (подсказки)
		/// </summary>
		/// <param name="exception">Исключение</param>
		/// <returns></returns>
		private string GetAdditionMessage(Exception exception)
		{
			if (exception as NullReferenceException != null) {
				return "Вероятно, не были установлены связи с моделью. Исправлением ошибки может являтся добавление <strong>[EntityBinder]</strong> для модели в параметрах вызываемого Экшена.";
			}
			return "";
		}
		/// <summary>
		/// Отправка сообщения на почту о возникшем исключении
		/// </summary>
		/// <param name="exception">Исключение</param>
		/// <param name="errorMessage">Текст сообщения</param>
		private void SendMailOnException(Exception exception, string errorMessage)
		{
			string additionMessage = GetAdditionMessage(exception);
			additionMessage = string.IsNullOrEmpty(additionMessage) ? "" : string.Format("<br/><strong style='color:red;'>Подсказка:</strong>{0}", additionMessage);
			errorMessage += additionMessage;
			EmailSender.SendError(errorMessage);
		}

		/// <summary>
		/// Обработка события перед обновлением модели
		/// </summary>
		/// <param name="preUpdate">результат события</param>
		/// <returns>veto</returns>
		public bool OnPreUpdate(PreUpdateEvent preUpdate)
		{
			try {
				CreatePreUpdateLog(preUpdate);
			}
			catch (Exception ex) {
				var errorMessage = string.Format(@"<h3>Произошла ошибка при формировании {0}, во время {1} записи.</h3><br/><strong>Сообщение</strong>:<br/>{2}<br/><strong>Содержание</strong>:<br/>{3}<br/><strong>Стэк</strong>:<br/>{4}",
					"лога", "обновления", ex.Message, ex.Data, ex.StackTrace);
				SendMailOnException(ex, errorMessage);
			}

			try {
				CreatePreUpdateAppeal(preUpdate);
			}
			catch (Exception ex) {
				var errorMessage = string.Format(@"<h3>Произошла ошибка при формировании {0}, во время {1} записи.</h3><br/><strong>Сообщение</strong>:<br/>{2}<br/><strong>Содержание</strong>:<br/>{3}<br/><strong>Стэк</strong>:<br/>{4}",
					"аппила", "обновления", ex.Message, ex.Data, ex.StackTrace);
				SendMailOnException(ex, errorMessage);
			}

			return false;
		}

		/// <summary>
		/// Обработка события после добавления модели
		/// </summary>
		/// <param name="postInsert">результат события</param>
		/// <returns></returns>
		public void OnPostInsert(PostInsertEvent postInsert)
		{
			try {
				CreatePostInsertLog(postInsert);
			}
			catch (Exception ex) {
				var errorMessage = string.Format(@"Произошла ошибка при формировании {0}, после {1} записи.<br/><strong>Сообщение</strong>:<br/>{2}<br/><strong>Содержание</strong>:<br/>{3}<br/><strong>Стэк</strong>:<br/>{4}",
					"лога", "добавления", ex.Message, ex.Data, ex.StackTrace);
				SendMailOnException(ex, errorMessage);
			}
		}

		/// <summary>
		/// Обработка события перед удалением  модели
		/// </summary>
		/// <param name="preDelete">результат события</param>
		/// <returns>veto</returns>
		public bool OnPreDelete(PreDeleteEvent preDelete)
		{
			try {
				CreatePreDeleteLog(preDelete);
			}
			catch (Exception ex) {
				var errorMessage = string.Format(@"Произошла ошибка при формировании {0}, при {1} записи.<br/><strong>Сообщение</strong>:<br/>{2}<br/><strong>Содержание</strong>:<br/>{3}<br/><strong>Стэк</strong>:<br/>{4}",
					"лога", "удалении", ex.Message, ex.Data, ex.StackTrace);
				SendMailOnException(ex, errorMessage);
			}
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
			if ((postUpdate.Entity as Log != null) || (postUpdate.Entity as Appeal != null)) {
				return;
			}
			if (postUpdate.Entity.GetType().GetInterfaces().Any(s => s == typeof(ILogAppeal))) {
				// сообщение
				var messageBuilder = new StringBuilder();
				// оформление логируемого поля модели
				const string pattern = "{0} было: {1} <br/>{0} стало: {2} <br/>";
				//получаем необходимые данные о событии
				var currentObj = ((ILogAppeal)postUpdate.Entity);
				var session = postUpdate.Session;
				// запрещаем Flush модели на время обработки
				(postUpdate.Session as ISession).FlushMode = FlushMode.Never;
				try {
					var oldState = postUpdate.OldState;
					var currentState = postUpdate.State;
					// объявляем нужные переменные
					Appeal appeal = null;
					var appealBase = currentObj.GetAppealClient(session);
					// реализуем логику обработки события
					if (appealBase != null) {
						// получаем логируемые поля модели 
						var appealFields = currentObj.GetAppealFields();
						// получаем поля обновленной модели
						var propNames = postUpdate.Persister.PropertyNames;
						// фиксируем изменения
						for (int i = 0; i < propNames.Length; i++) {
							// если поле логируемо
							if (appealFields.Contains(propNames[i])) {
								appealFields.Remove(propNames[i]); // убираем его из списка
								// проверяем было ли поля изменено
								if ((!(oldState.GetValue(i) == null && currentState.GetValue(i) == null) && (oldState.GetValue(i) == null || currentState.GetValue(i) == null))
								    || (!(oldState.GetValue(i) == null && currentState.GetValue(i) == null) && (!oldState.GetValue(i).Equals(currentState[i])))) {
									// так фиксируем изменение, если поле - это модель
									if (currentState.GetValue(i) != null && (currentState.GetValue(i) as BaseModel) != null) {
										// обработка полей, являющихся наследниками BaseModel,
										// чьи логируемые значения формируются внутри модели своим собственным методом 
										string changesText = currentObj.GetAdditionalAppealInfo(propNames[i], oldState.GetValue(i), session);
										if (changesText == "") {
											// если собственного сообщение нет, формируем стандартное сообщение
											messageBuilder.AppendFormat(pattern, postUpdate.Entity.GetDescription(propNames[i]),
												oldState.GetValue(i) == null ? "значение отсуствует" : oldState[i].ToString(),
												currentState.GetValue(i) == null ? "значение отсуствует" : currentState[i]);
										}
										else {
											// если собственное сообщение есть, просто добавляем его
											messageBuilder.Append(changesText);
										}
									}
									else {
										// так фиксируем изменение, если поле - это список
										if ((currentState.GetValue(i) as IList) != null) {
											// обработка обычных полей
											messageBuilder.AppendFormat(pattern, postUpdate.Entity.GetDescription(propNames[i]) + " (список) ",
												oldState.GetValue(i) == null ? "значение отсуствует" // пустое старое значение
													: (oldState.GetValue(i) as IList).Count.ToString(), // значение - список, выводим длину прежнего списка
												currentState.GetValue(i) == null ? "значение отсуствует" // пустое новое значение
													: (currentState.GetValue(i) as IList).Count.ToString() // значение - список, выводим длину текущего списка 
												);
										}
										else {
											// так фиксируем изменение, если поле не модель и не список
											messageBuilder.AppendFormat(pattern, postUpdate.Entity.GetDescription(propNames[i]),
												oldState.GetValue(i) == null ? "значение отсуствует" : oldState[i],
												currentState.GetValue(i) == null ? "значение отсуствует" : currentState[i]);
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
						// если существует логируемая разница между старой и новой моделями, сохраняем ее в лог 
						if (messageBuilder.Length > 0) {
							// добавляя в начало информацию о модели
							messageBuilder.Insert(0, string.Format("{0} , Id : {1} <br/>",
								postUpdate.Entity.GetDescription(),
								((postUpdate.Entity as BaseModel) != null ? "" + ((BaseModel)postUpdate.Entity).Id : "")));
							// формируем лог
							appeal = new Appeal() {
								Message = messageBuilder.ToString(),
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
				finally {
					// возвращаем значение Flush поумолчанию
					(postUpdate.Session as ISession).FlushMode = FlushMode.Auto;
				}
			}
		}

		/// <summary>
		/// логирование модели перед обновлением
		/// </summary>
		/// <param name="postUpdate">результат события</param>
		private void CreatePreUpdateLog(PreUpdateEvent postUpdate)
		{
			if ((postUpdate.Entity as Log != null) || (postUpdate.Entity as Appeal != null)) {
				return;
			}
			//получаем необходимые данные о событии  
			var session = postUpdate.Session;
			var oldState = postUpdate.OldState;
			var currentState = postUpdate.State;
			// сообщение
			var messageBuilder = new StringBuilder();
			// оформление логируемого поля модели
			const string pattern = @"<span class='c-pointer text' title='{3}'>{0}</span> <span title='{3}'>было: {1}</span> <br/>
<span class='c-pointer text white' title='{3}'>{0}</span><span title='{3}'> стало: {2}</span> <br/>";
			// объявляем нужные переменные
			Log log = null;
			// реализуем логику обработки события  
			// получаем поля обновленной модели
			var propNames = postUpdate.Persister.PropertyNames;
			// фиксируем изменения
			for (int i = 0; i < propNames.Length; i++) {
				// проверяем было ли поля изменено
				if ((!(oldState.GetValue(i) == null && currentState.GetValue(i) == null) && (oldState.GetValue(i) == null || currentState.GetValue(i) == null))
				    || (!(oldState.GetValue(i) == null && currentState.GetValue(i) == null) && (!oldState.GetValue(i).Equals(currentState[i])))) {
					// так фиксируем изменение, если поле - это список
					if ((currentState.GetValue(i) as IList) != null) {
						// формируем сообщение, с указанием ко-ва эл. списка 
						messageBuilder.AppendFormat(pattern, (postUpdate.Entity != null ? postUpdate.Entity.GetDescription(propNames[i]).Replace("Proxy", "") : propNames[i]) + " (список) ",
							oldState.GetValue(i) == null ? "значение отсуствует" : // пустое старое значение
								(currentState.GetValue(i) as IList).Count.ToString(), // значение - список, выводим длину прежнего списка 
							currentState.GetValue(i) == null ? "значение отсуствует" : // пустое новое значение
								(currentState.GetValue(i) as IList).Count.ToString(), // значение - список, выводим длину текущего списка 
							propNames[i]);
					}
					else {
						// так фиксируем изменение, если поле - не список
						// формируем сообщение, в случае если езмененное поле - модель - указываем разницу Id
						messageBuilder.AppendFormat(pattern, (postUpdate.Entity != null ? postUpdate.Entity.GetDescription(propNames[i]).Replace("Proxy", "") : propNames[i]),
							(oldState.GetValue(i) == null ? "значение отсуствует" // пустое старое значение
								: ((oldState.GetValue(i) as BaseModel) == null ? // если старое значение не является моделью
									(oldState[i] is Enum ? oldState[i].GetDescription() // если старое значение яв. перечислением, получаем его описание
										: (oldState[i] is bool ? ((bool)oldState[i]) ? "Да" : "Нет" // если бинарное старое значение - выводим да/нет
											: oldState[i])) // а вообще, старое значение обычное
									: (oldState.GetValue(i) as BaseModel).Id)), // значение старое - модель, выводим Id модели
							(currentState.GetValue(i) == null ? "значение отсуствует" // пустое новое значение
								: ((currentState.GetValue(i) as BaseModel) == null ? // если новое значение не является моделью
									(currentState[i] is Enum ? currentState[i].GetDescription() // если новое значение яв. перечислением, получаем его описание
										: (currentState[i] is bool ? ((bool)currentState[i]) ? "Да" : "Нет" // если бинарное новое значение - выводим да/нет
											: currentState[i])) // а вообще, новое значение обычное 
									: (currentState.GetValue(i) as BaseModel).Id)),
							propNames[i]); // значение новое - модель, выводим Id модели
					}
				}
			}
			// если существует логируемая разница между старой и новой моделями, сохраняем ее в лог 
			if (messageBuilder.Length > 0) {
				// добавляя в начало информацию о модели
				int modelId = ((postUpdate.Entity as BaseModel) != null ? ((BaseModel)postUpdate.Entity).Id : 0);
				messageBuilder.Insert(0, string.Format("Модель <strong> {0} </strong>, Id : <strong>{1}</strong> изменена :<br/>",
					"<span class='c-pointer' title='" + postUpdate.Entity.GetType().Name + "'>" + postUpdate.Entity.GetDescription() + "</span>", modelId));
				// формируем лог 
				log = new Log() {
					Message = messageBuilder.ToString(),
					Type = LogEventType.Update,
					Employee = CurrentEmployee,
					ModelId = modelId,
					ModelClass = postUpdate.Entity.GetType().Name
				};
			}
			// если лог сформирован, сохраняем его в БД
			if (log != null) {
				session.Save(log);
			}
		}

		/// <summary>
		/// логирование модели после ее добавления
		/// </summary>
		/// <param name="preInsert">результат события</param>
		private void CreatePostInsertLog(PostInsertEvent preInsert)
		{
			if ((preInsert.Entity as Log != null) || (preInsert.Entity as Appeal != null)) {
				return;
			}
			//получаем необходимые данные о событии 
			var currentObj = preInsert.State;
			var session = preInsert.Session;
			// объявляем нужные переменные
			Log log = null;
			// сообщение
			var messageBuilder = new StringBuilder();
			// оформление логируемого поля модели
			const string pattern = "{0} : {1} <br/>";
			// реализуем логику обработки события  
			// получаем поля обновленной модели
			var propNames = preInsert.Persister.PropertyNames;
			// фиксируем изменения
			for (int i = 0; i < propNames.Length; i++) {
				// так фиксируем изменение, если поле - это список
				if ((currentObj.GetValue(i) as IList) != null) {
					messageBuilder.AppendFormat(pattern, propNames.GetValue(i) + " (список) ",
						(currentObj.GetValue(i) == null ? "значение отсуствует" // пустое значение
							: (currentObj.GetValue(i) as IList).Count.ToString())); // значение - список, выводим длину списка
				}
				else {
					// так фиксируем изменение, если поле - не список
					messageBuilder.AppendFormat(pattern, propNames.GetValue(i),
						(currentObj.GetValue(i) == null ? "значение отсуствует" // пустое значение
							: ((currentObj.GetValue(i) as BaseModel) == null ? currentObj[i] // значение обычное
								: (currentObj.GetValue(i) as BaseModel).Id))); // значение - модель, выводим Id модели
				}
			}
			// сохраняем сообщение в лог 
			if (messageBuilder.Length > 0) {
				// добавляя в начало информацию о модели
				int modelId = ((preInsert.Entity as BaseModel) != null ? ((BaseModel)preInsert.Entity).Id : 0);
				messageBuilder.Insert(0, string.Format("Модель <strong> {0} </strong>, Id : <strong>{1}</strong> добавлена :<br/>",
					preInsert.Entity.GetDescription(), modelId));
				// формируем лог 
				log = new Log() {
					Message = messageBuilder.ToString(),
					Type = LogEventType.Insert,
					Employee = CurrentEmployee,
					ModelId = modelId,
					ModelClass = preInsert.Entity.GetType().Name
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
			if ((preDelete.Entity as Log != null) || (preDelete.Entity as Appeal != null)) {
				return;
			}
			//получаем необходимые данные о событии 
			var currentObj = preDelete.DeletedState;
			var session = preDelete.Session;
			// сообщение
			var messageBuilder = new StringBuilder();
			// оформление логируемого поля модели
			const string pattern = "{0} : {1} <br/>";
			// объявляем нужные переменные
			Log log = null;
			// реализуем логику обработки события  
			// получаем поля обновленной модели
			var propNames = preDelete.Persister.PropertyNames;
			// фиксируем изменения
			for (int i = 0; i < propNames.Length; i++) {
				// так фиксируем изменение, если поле - это список
				if ((currentObj.GetValue(i) as IList) != null) {
					messageBuilder.AppendFormat(pattern, propNames.GetValue(i) + " (список) ",
						(currentObj.GetValue(i) == null ? "значение отсуствует" // пустое значение
							: (currentObj.GetValue(i) as IList).Count.ToString())); // значение - список, выводим длину списка
				}
				else {
					// так фиксируем изменение, если поле - не список
					messageBuilder.AppendFormat(pattern, propNames.GetValue(i),
						(currentObj.GetValue(i) == null ? "значение отсуствует" // пустое значение
							: ((currentObj.GetValue(i) as BaseModel) == null ? currentObj[i] // значение обычное
								: (currentObj.GetValue(i) as BaseModel).Id))); // значение - модель, выводим Id модели
				}
			}
			// сохраняем сообщение в лог 
			if (messageBuilder.Length > 0) {
				// добавляя в начало информацию о модели
				int modelId = ((preDelete.Entity as BaseModel) != null ? ((BaseModel)preDelete.Entity).Id : 0);
				messageBuilder.Insert(0, string.Format("Модель <strong> {0} </strong>, Id : <strong>{1}</strong> удалена :<br/>",
					preDelete.Entity.GetDescription(), modelId));
				// формируем лог 
				log = new Log() {
					Message = messageBuilder.ToString(),
					Type = LogEventType.Delete,
					Employee = CurrentEmployee,
					ModelId = modelId,
					ModelClass = preDelete.Entity.GetType().Name
				};
			}
			// если лог сформирован, сохраняем его в БД
			if (log != null) {
				session.Save(log);
			}
		}
	}
}