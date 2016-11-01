using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Inforoom2.validators
{
	public class ApplicantPhoneValidator : CustomValidator
	{  
		protected override void Run(object value)
		{
			if (value == null) {
				AddError("Введите номер телефона");
			}
			if (value is string) {
				var telephone = value as string; 
				// проверка NotEmpty
				if (telephone.Length != 10 && telephone.Length >0)
				{
					AddError("Введите номер в десятизначном формате");
				} 
				if (string.IsNullOrEmpty(telephone))
				{
					AddError("Введите номер телефона"); 
				}
			}
		}
	}
}