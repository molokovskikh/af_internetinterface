using System;
using System.Collections.Generic;
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
	public class ModelUpdateListener : IPostUpdateEventListener
	{
		public static Employee CurrentEmployee { get; private set; }

		public static void SetEmployee(Employee currentUser)
		{
			CurrentEmployee = currentUser;
		}

		public void OnPostUpdate(PostUpdateEvent postUpdate)
		{
			CreatePostUpdateAppeal(postUpdate);
		}

		private void CreatePostUpdateAppeal(PostUpdateEvent postUpdate)
		{
			if (postUpdate.Entity.GetType().GetInterfaces().Any(s => s == typeof(ILogAppeal))) {
				//получаем необходимые данные о событии
				var currentObj = ((ILogAppeal)postUpdate.Entity);
				var session = postUpdate.Session;
				var oldState = postUpdate.OldState;
				var currentState = postUpdate.State;
				// объявляем нужные переменные
				Appeal appeal = null;
				Client appealBase = currentObj.GetAppealClient(session);
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
									string changesText =  currentObj.GetRelationChanges(propNames[i], oldState.GetValue(i));
									if (changesText=="") {
										changesText = propNames.GetValue(i) + " было: " + (oldState.GetValue(i) == null ? "значение отсуствует" : oldState[i]).ToString() + " <br/> "
									           + propNames.GetValue(i) + " стало: " + (currentState.GetValue(i) == null ? "значение отсуствует" : currentState[i]).ToString() + " <br/> ";
									}
									message += changesText;
								}
								else {
									// обработка обычных полей
									message += propNames.GetValue(i) + " было: " + (oldState.GetValue(i) == null ? "значение отсуствует" : oldState[i]).ToString() + " <br/> "
									           + propNames.GetValue(i) + " стало: " + (currentState.GetValue(i) == null ? "значение отсуствует" : currentState[i]).ToString() + " <br/> ";
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
	}
}