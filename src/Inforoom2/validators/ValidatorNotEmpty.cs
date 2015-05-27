using System;

namespace Inforoom2.validators
{
	//todo Переделать в NotEmpty - так как не имеет смысла делать отдельный валидатор для DateTime
	class ValidatorNotEmpty : CustomValidator
	{
		protected override void Run(object value)
		{
			if (value is DateTime?)
			{
				var val = value as DateTime?;
			if (!val.HasValue)
				AddError("Отсутствует значение");
			else if(val.Value == DateTime.MinValue)
				AddError("Отсутствует значение");
			}
			if (value == null) {
				AddError("Значение Null");
			}

		}
	}
}