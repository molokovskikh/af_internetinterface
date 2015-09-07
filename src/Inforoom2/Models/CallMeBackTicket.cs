using System;
using System.Collections.Generic;
using Common.Tools;
using Inforoom2.Components;
using NHibernate;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;
using NHibernate.Validator.Engine;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель вопроса от пользователя на странице бизнес
	/// </summary>
	[Class(Table = "callmeback_tickets", NameType = typeof(CallMeBackTicket))]
	public class CallMeBackTicket : BaseModel
	{
		public CallMeBackTicket()
		{
			CreationDate = SystemTime.Now();
		}

		[Property, NotNullNotEmpty(Message = "Введите комментарий")]
		public virtual string Text { get; set; }

		[Property]
		public virtual DateTime CreationDate { get; set; }

		[Property]
		public virtual DateTime AnswerDate { get; set; }

		[ManyToOne(Column = "Employee")]
		public virtual Employee Employee { get; set; }

		[Property]
		public new virtual string Email { get; set; }

		[Property(Column = "Phone"), Pattern(@"^\d{10}$", Message = "Введите номер в десятизначном формате"), NotNullNotEmpty(Message = "Введите номер телефона")]
		public virtual string PhoneNumber { get; set; }

		[Property, NotNullNotEmpty(Message = "Введите имя")]
		public virtual string Name { get; set; }

		[ManyToOne]
		public virtual Client Client { get; set; }

		[NotNullNotEmpty(Message = "Введите код с картинки")]
		public virtual string Captcha { get; set; }

		protected virtual string ConfirmCaptcha { get; set; }

		/// <summary>
		/// Установка значения капчи, которое было отображено клиенту
		/// </summary>
		/// <param name="str">Код, отображенный клиенту</param>
		public virtual void SetConfirmCaptcha(string str)
		{
			ConfirmCaptcha = str;
		}

		/// <summary>
		/// Проверка совпадения кода веденного пользователем и кода отображенного ему
		/// </summary>
		/// <param name="session"></param>
		/// <returns></returns>
		public override ValidationErrors Validate(ISession session)
		{
			var errors  = new ValidationErrors();
			if(Captcha != ConfirmCaptcha)
				errors.Add(new InvalidValue("Код введен неверно",GetType(),"Captcha",Captcha,this,new List<object>()));
			Captcha = "";
			return errors;
		}
	}
}