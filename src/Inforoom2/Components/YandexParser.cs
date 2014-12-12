using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Inforoom2.Models;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;

namespace Inforoom2.Components
{
	public class YandexAddress
	{
		public string YandexRegion { get; set; }
		public string YandexStreet { get; set; }
		public string YandexHouse { get; set; }

		public Region Region { get; set; }
		public Street Street { get; set; }
		public House House { get; set; }

		public bool IsCorrect { get; set; }
	}

	public class YandexParser
	{
		private ISession session;

		public YandexParser(ISession session)
		{
			this.session = session;
		}

		public YandexAddress GetYandexAddress(string region, string street, string house)
		{
			region = region.Trim().ToLower();
			street = street.Trim().ToLower();
			house =  house.Trim().ToLower().Replace(" ","");

			var address = region + " " + street + " " + house;
			string resultSearchObject = SearchObject(address);
			var response = GetAddress(resultSearchObject).Split(',');

			var yandexAddress = new YandexAddress();
			yandexAddress.IsCorrect = true;

			try {
				//Яндекс возвращает разный формат данных при разных городах
				if (address.Contains("борисоглебск")) {
					yandexAddress.YandexRegion = response[2].Trim().ToLower();
					yandexAddress.YandexStreet = response[3].Trim().ToLower();
					if (response.Length > 4) {
						yandexAddress.YandexHouse = response[4].Trim().ToLower();
					}
				}
				else {
					yandexAddress.YandexRegion = response[1].Trim().ToLower();
					yandexAddress.YandexStreet = response[2].Trim().ToLower();
					if (response.Length > 3) {
						yandexAddress.YandexHouse = response[3].Trim().ToLower();
					}
				}
			}
			catch (Exception e) {
				yandexAddress.YandexRegion = region;
				yandexAddress.YandexStreet = street;
				yandexAddress.YandexHouse = house;
				yandexAddress.IsCorrect = false;
			}
			if (!address.Contains(yandexAddress.YandexRegion)) {
				//Если регоин не тот, адрес неправильный
				yandexAddress.YandexRegion = region;
				yandexAddress.YandexStreet = street;
				yandexAddress.YandexHouse = house;
				yandexAddress.IsCorrect = false;
			}

			yandexAddress.Region = FindRegion(yandexAddress);
			yandexAddress.Street = FindStreet(yandexAddress);
			if (yandexAddress.YandexHouse != null) {
				yandexAddress.House = FindHouseNumber(yandexAddress);
			}
			
			return yandexAddress;
		}

		private Region FindRegion(YandexAddress yandexAddress)
		{
			return session.Query<Region>().ToList().FirstOrDefault(r => r.Name.Equals(yandexAddress.YandexRegion, StringComparison.InvariantCultureIgnoreCase));
		}

		private House FindHouseNumber(YandexAddress yandexAddress)
		{
			var house = session.Query<House>().ToList().FirstOrDefault(s => s.Number.Equals(yandexAddress.YandexHouse, StringComparison.InvariantCultureIgnoreCase)
			                                                                && s.Street.Id == yandexAddress.Street.Id
			                                                                && s.Street.Region.Id == yandexAddress.Region.Id);
			if (house == null) {
				house = new House(yandexAddress.YandexHouse, yandexAddress.Street);
				session.Save(house);
			}

			return house;
		}

		private Street FindStreet(YandexAddress yandexAddress)
		{
			var street = session.Query<Street>().ToList().FirstOrDefault(s => s.Name.Equals(yandexAddress.YandexStreet, StringComparison.InvariantCultureIgnoreCase)
			                                                                  && s.Region.Id == yandexAddress.Region.Id);
			if (street == null) {
				street = new Street(yandexAddress.YandexStreet, yandexAddress.Region);
				session.Save(street);
			}
			return street;
		}

		private string SearchObject(string address)
		{
			string urlXml = "http://geocode-maps.yandex.ru/1.x/?geocode=" + address + "&results=1";
			var request = new Request();
			string result = request.GetResponseToString(request.GET(urlXml));
			return result;
		}

		private string GetAddress(string resultSearchObject)
		{
			string address = "";
			var xd = new XmlDocument();
			xd.LoadXml(resultSearchObject);
			XmlNodeList GeoObjectTemp = xd.GetElementsByTagName("GeocoderMetaData");
			foreach (XmlNode node in GeoObjectTemp) {
				foreach (XmlNode item in node.ChildNodes) {
					if (item.Name == "text") {
						address = item.InnerText;
					}
				}
				break;
			}
			return address;
		}
	}

	public class Request
	{
		private Stream responseStream;

		public Stream POST(string Url, string Command)
		{
			responseStream = ResponseStreamPOST(Url, Command);
			return responseStream;
		}

		public Stream POST(string Url, string Command, WebProxy Proxy)
		{
			responseStream = ResponseStreamPOST(Url, Command, Proxy);
			return responseStream;
		}

		public Stream GET(string Url)
		{
			responseStream = ResponseStreamGET(Url);
			return responseStream;
		}

		public Stream GET(string Url, WebProxy Proxy)
		{
			responseStream = ResponseStreamGET(Url, Proxy);
			return responseStream;
		}

		public XDocument GetResponseToXDocument(Stream RequestMetod)
		{
			XmlReader xmlReader = XmlReader.Create(RequestMetod);
			return XDocument.Load(xmlReader);
		}

		public string GetResponseToString(Stream RequestMetod)
		{
			using (var ResponseStreamReader = new StreamReader(RequestMetod)) {
				return ResponseStreamReader.ReadToEnd();
			}
		}

		private Stream ResponseStreamGET(string Url)
		{
			return ResponseStreamGET(Url, null);
		}

		private Stream ResponseStreamGET(string Url, WebProxy Proxy)
		{
			var request = (HttpWebRequest)WebRequest.Create(Url);

			if (Proxy != null) {
				request.Proxy = Proxy;
			}

			//Получение ответа.
			var response = (HttpWebResponse)request.GetResponse();
			Stream responsestream = response.GetResponseStream();
			return responsestream;
		}

		private Stream ResponseStreamPOST(string Url, string Command)
		{
			return ResponseStreamPOST(Url, Command, null);
		}

		private Stream ResponseStreamPOST(string Url, string Command, WebProxy Proxy)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(Command);

			// Объект, с помощью которого будем отсылать запрос и получать ответ.
			var policy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
			HttpWebRequest.DefaultCachePolicy = policy;
			var request = (HttpWebRequest)WebRequest.Create(Url);

			request.Method = "POST";
			request.ContentLength = bytes.Length;
			request.ContentType = "text/xml";

			if (Proxy != null) {
				request.Proxy = Proxy;
			}

			// Пишем наш XML-запрос в поток
			using (Stream requestStream = request.GetRequestStream()) {
				requestStream.Write(bytes, 0, bytes.Length);
			}

			// Получаем ответ
			var response = (HttpWebResponse)request.GetResponse();
			Stream responsestream = response.GetResponseStream();
			return responsestream;
		}
	}
}