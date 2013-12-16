using Castle.Components.Validator;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;

namespace InternetInterface.Helpers
{
	public class UserValidateNonEmpty : AbstractValidationAttribute
	{
		public UserValidateNonEmpty(string errorMessage) : base(errorMessage)
		{
		}

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
			//если редактирование производится в личном кабинете
			//то делать проверки не имеет смысла тк даже если они не пройдут
			//человек ничего сделать не сможет
			var partner = InitializeContent.GetAdministrator();
			if (partner == null)
				return true;

			if (partner.Role.ReductionName == "Office")
				return true;

			if (partner.Role.ReductionName == "Diller") {
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