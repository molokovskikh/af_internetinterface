using Castle.ActiveRecord;
using InternetInterface.Models.Universal;

namespace InternetInterface.Helpers
{
	public class ValidBuilderHelper<T> where T : ValidActiveRecordLinqBase<T>, new()
	{
		public ValidBuilderHelper(T cl)
		{
			_validClient = cl;
		}
		private readonly T _validClient;

		public string GetBlock(string blockName)
		{
			if (!string.IsNullOrEmpty(_validClient.GetErrorText(blockName)))
			{
				return "<div class=\"flash\" style=\"margin:0px; padding:0px; height:100%; width:100%;\"> \r\n" +
					   "<div class=\"message error\" style=\"margin:0px; padding:0px;\"> \r\n" +
				       "<p>" + _validClient.GetErrorText(blockName) + "</p> \r\n" +
				       "</div> \r\n" +
				       "</div>";
			}
			else
			{
				return string.Empty;
			}
		}
	}
}