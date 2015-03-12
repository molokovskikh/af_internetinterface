using System;

namespace Inforoom2.validators
{
	//todo Переделать в NotEmpty - так как не имеет смысла делать отдельный валидатор для DateTime
	class DateTimeNotEmpty : CustomValidator
	{
		protected override void Run(object value)
		{
			var val = value as DateTime?;
			if (!val.HasValue)
				AddError("Отсутствует значение");
			else if(val.Value == DateTime.MinValue)
				AddError("Отсутствует значение");
		}
	}
}