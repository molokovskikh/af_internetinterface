using System;
using Castle.Components.Validator;

namespace InternetInterface.Models.Universal
{
	public class ValidActiveRecordLinqBase<T> : ChildActiveRecordLinqBase<T>
	{
		private  ErrorSummary ValidationErrors;

		/*public ValidActiveRecordLinqBase()
		{
			ValidationErrors = null;
		}*/

		public virtual void SetValidationErrors(ErrorSummary _ValidationErrors)
		{
			ValidationErrors = _ValidationErrors;
		}

		public virtual ErrorSummary GetValidationErrors()
		{
			return ValidationErrors;
		}

		public virtual string GetErrorText(string field)
		{
			if (ValidationErrors != null)
			{
				for (int i = 0; i < ValidationErrors.ErrorsCount; i++)
				{
					if (ValidationErrors.ErrorMessages != null)
					{
						if (ValidationErrors.InvalidProperties[i] == field)
						{
							return ValidationErrors.ErrorMessages[i];
						}
					}
				}
			}
			return string.Empty;
		}


		public static string GeneratePassword()
		{
			var availableChars = "23456789qwertyupasdfghjkzxcvbnmQWERTYUPASDFGHJKLZXCVBNM";
			var password = String.Empty;
			var random = new Random();
			while (password.Length < 8)
				password += availableChars[random.Next(0, availableChars.Length - 1)];
			return password;
		}
	}
}