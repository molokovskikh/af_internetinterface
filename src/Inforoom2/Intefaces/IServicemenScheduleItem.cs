using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Inforoom2.Models;

namespace Inforoom2.Intefaces
{
	public interface IServicemenScheduleItem
	{
		string GetAddress();
		Client GetClient();
		string GetPhone();
	}
}
