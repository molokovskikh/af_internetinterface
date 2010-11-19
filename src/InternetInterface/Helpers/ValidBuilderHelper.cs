using InternetInterface.Models.Universal;

namespace InternetInterface.Helpers
{
	public class ValidBuilderHelper<T> where T : ValidActiveRecordLinqBase<T>
	{
		public ValidBuilderHelper(T cl)
		{
			_validClient = cl;
		}
		private readonly T _validClient;

		public string GetBlock(string blockName)
		{
			if (_validClient.GetErrorText(blockName)!="")
			{
				return "<div class=\"flash\"> \r\n" +
				       "<div class=\"message error\"> \r\n" +
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