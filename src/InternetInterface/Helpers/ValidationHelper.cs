using Castle.Components.Validator;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;

namespace InternetInterface.Helpers
{
	public class UserValidateNonEmpty : AbstractValidationAttribute
	{
		public UserValidateNonEmpty(string errorMessage) : base(errorMessage)
		{}

		public override IValidator Build()
		{
			IValidator validator = new NonEmptyValidator();

			ConfigureValidatorMessage(validator);

			return validator;
		}
	}

	public class NonEmptyValidator : AbstractValidator
	{
		public override bool IsValid(object instance, object fieldValue)
		{
			if (InitializeContent.partner.Categorie.ReductionName == "Office")
				return true;
			if (InitializeContent.partner.Categorie.ReductionName == "Diller")
			{
				if (string.IsNullOrEmpty((string)fieldValue))
					return false;
			}
			return true;
		}

		public override bool SupportsBrowserValidation
		{
			get { return false; }
		}
	}
}