using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.Components.Validator;

namespace InternetInterface.Helpers
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true), CLSCompliant(false)] [Serializable]
	class ValidateGreaterOrEqualZeroAttribute : AbstractValidationAttribute
	{
		private readonly IValidator _validator;

		public ValidateGreaterOrEqualZeroAttribute()
		{
			_validator = new GreaterOrEqualZeroValidator();
		}

		public ValidateGreaterOrEqualZeroAttribute(string errorMessage)
			: base(errorMessage)
		{
			_validator = new GreaterOrEqualZeroValidator();
		}

		public override IValidator Build()
		{
			base.ConfigureValidatorMessage(_validator);
			return _validator;
		}
	}

	[Serializable]
	public class GreaterOrEqualZeroValidator : AbstractValidator
	{
		public override bool SupportsBrowserValidation
		{
			get
			{
				return false;
			}
		}

		public override bool IsValid(object instance, object fieldValue)
		{
			if (fieldValue == null)
				return false;

			return (decimal)fieldValue >= 0;
		}

		protected override string BuildErrorMessage()
		{
			return "Не может быть меньше нуля";
		}
	}
}