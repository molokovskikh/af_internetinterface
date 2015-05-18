using System;

namespace Inforoom2.validators
{
	class ValidatorNumberic : CustomValidator
	{
		public enum Type
		{
			Greater = 0,
			GreaterOrEqual = 1,
			Less = 2,
			LessOrEqual =3
		}

		protected Type CurrentType; 
		protected float CurrentValue;
		protected bool AllowNull;

		public ValidatorNumberic(float value, ValidatorNumberic.Type type, bool allowNull = false)
		{
			CurrentType = type;
			CurrentValue = value;
			AllowNull = allowNull;
		}

		protected override void Run(object value)
		{
			
			if (value == null && AllowNull)
				return;
			if (value == null)
				value = "";
			float val;
			var converted = float.TryParse(value.ToString(), out val);
			if (!converted && !AllowNull) {
				AddError("Значение должно быть числом");
				return;
			}
				
			

			switch (CurrentType) {
				case Type.Greater :
					Greater(val);
					break;
				case Type.GreaterOrEqual :
					GreaterOrEqual(val);
					break;
				case Type.Less:
					Less(val);
					break;
				case Type.LessOrEqual :
					LessOrEqual(val);
					break;

			}
		}

		private void Greater(float value)
		{
			if (value <= CurrentValue)
				AddError("Значение должно быть больше " + GetValue());
		}

		private void GreaterOrEqual(float value)
		{
			if (value < CurrentValue)
				AddError("Значение должно быть больше или равно " + GetValue());
		}

		private void Less(float value)
		{
			if (value >= CurrentValue)
				AddError("Значение должно быть меньше " + GetValue());
		}

		private void LessOrEqual(float value)
		{
			if (value > CurrentValue)
				AddError("Значение должно быть меньше или равно " + GetValue());
		}

		private string GetValue()
		{
			var temp = (int)CurrentValue;
			if (temp == CurrentValue)
				return temp.ToString();
			else
				return CurrentValue.ToString();
		}
	}
}