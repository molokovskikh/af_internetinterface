using System;
using System.Linq;
using Castle.Components.Binder;
using Castle.Components.Validator;
using Castle.MonoRail.ActiveRecordSupport;
using InternetInterface.Models;

namespace InternetInterface.Helpers
{
	public class Validation
	{
		public static string ValidationConnectInfo(ConnectInfo info, bool register, uint endpointId = 0)
		{
			if (string.IsNullOrEmpty(info.Port) && !register)
				return "������� ����";
			if (info.Switch == 0 && !register)
				return "�������� ����������";
			int res;
			if (Int32.TryParse(info.Port, out res)) {
				if (res > 48 || res < 1)
					return "���� ������ ���� � �������� � 1 �� 48";
				if (ClientEndpoint.Queryable.Any(e => e.Port == res && e.Switch.Id == info.Switch && e.Id != endpointId))
					return "����� ���� ����/���������� ��� ����������";
			}
			return string.Empty;
		}
	}

	public class DecimalValidateBinder : ARDataBinder
	{
		protected override void BeforeBindingProperty(object instance, System.Reflection.PropertyInfo prop, string prefix, CompositeNode node)
		{
			dynamic child = node.GetChildNode(prop.Name);
			if (child != null) {
				if (child.GetType() != typeof(LeafNode))
					return;
				var validators = Validator.GetValidators(instance.GetType(), prop);
				var decValidator = validators.OfType<DecimalValidator>().FirstOrDefault();
				if (decValidator != null && !decValidator.IsValid(instance, child.Value)) {
					var errorSummary = GetValidationSummary(instance);
					errorSummary.RegisterErrorMessage(prop, decValidator.ErrorMessage);
				}
			}
		}
	}
}