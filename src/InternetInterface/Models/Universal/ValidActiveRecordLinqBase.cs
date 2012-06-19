using System;
using Castle.ActiveRecord;
using Castle.Components.Validator;

namespace InternetInterface.Models.Universal
{
	public class ValidActiveRecordLinqBase<T> : ChildActiveRecordLinqBase<T> where T : ActiveRecordBase, new()
	{
		private  ErrorSummary ValidationErrors;


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
	}
}