using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InternetInterface.Helpers
{
	[Serializable]
	public class BaseUsersException : Exception
	{
		public BaseUsersException()
		{
		}

		public BaseUsersException(string message) : base(message)
		{
		}

		public BaseUsersException(string message, Exception inner) : base(message, inner)
		{
		}

		// Constructor needed for serialization 
		// when exception propagates from a remoting server to the client. 
		protected BaseUsersException(System.Runtime.Serialization.SerializationInfo info,
			System.Runtime.Serialization.StreamingContext context)
		{
		}
	}
}