using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom2.Models;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;

namespace Inforoom2.Test.Infrastructure.Helpers
{
	/// <summary>
	/// Разбивает список Адресов (строк) на количество частей указанных в addressPosition; 
	/// Количество элементов списока всегда должен быть достаточным. (чтобы кто-то не остался без адреса)
	/// </summary>
	public class AddressCreateHelper
	{
		/// <summary>
		/// список адресов 
		/// </summary>
		private List<string> addresses = new List<string>();

		private int currentIndex = 0;

		/// <summary>
		///  метки для разделения адреса
		/// </summary>
		public enum addressPosition
		{
			city,
			region,
			street,
			house
		}

		public int Index
		{
			get { return currentIndex; }
			private set { currentIndex = value; }
		}

		public string City
		{
			get { return addresses[Index].Split(',')[(int)addressPosition.city]; }
		}

		public string Region
		{
			get { return addresses[Index].Split(',')[(int)addressPosition.region]; }
		}

		public string Street
		{
			get { return addresses[Index].Split(',')[(int)addressPosition.street]; }
		}

		public string House
		{
			get { return addresses[Index].Split(',')[(int)addressPosition.house]; }
		}

		/// <summary>
		///  добавление адресов в список addresses
		/// </summary> 
		public AddressCreateHelper()
		{
			addresses.Add("Борисоглебск,Борисоглебск,улица Ленина,8");
			addresses.Add("Борисоглебск,Борисоглебск,Третьяковская улица,6Б");
			addresses.Add("Борисоглебск,Борисоглебск,ул. Ленина,20");
			addresses.Add("Борисоглебск,Борисоглебск,ул. Ленина,22");
			addresses.Add("Борисоглебск,Борисоглебск,ул. Ленина,24А");
			addresses.Add("Борисоглебск,Борисоглебск,ул. Гагарина,1А");
			addresses.Add("Борисоглебск,Борисоглебск,ул. Гагарина,1Б");
			addresses.Add("Борисоглебск,Борисоглебск,ул. Гагарина,2В");
			addresses.Add("Борисоглебск,Борисоглебск,ул. Гагарина,2Г");
			addresses.Add("Борисоглебск,Борисоглебск,ул. Гагарина,2Д");
			addresses.Add("Борисоглебск,Борисоглебск,ул. Гагарина,90");
		}

		/// <summary>
		/// получение следующего адреса
		/// </summary>
		/// <returns>false по выходу индекса за пределы массива</returns>
		public bool GetNextAddress()
		{
			Index++;
			if (addresses.Count > Index) {
				return true;
			}
			Index = 0;
			return false;
		}
	}
}