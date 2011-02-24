using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Ionic.Zip;
using Microsoft.Win32;

namespace MySqlCreatorForTest
{
	class Program
	{
		static void Main(string[] args)
		{
			var zipFilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			var zipFileName = string.Empty;
			var handZipFileName = string.Empty;
			Console.WriteLine("Вы хотите начать скачивание MySql? (132.8Mb) Y/N");
			if (Console.ReadLine() == "Y")
			{
				Console.WriteLine("Введите название ZIP файла в который следует сохранить MySql (пример MySql.zip)");
				zipFileName = Console.ReadLine();
				var webClient = new WebClient();
				webClient.DownloadFileCompleted += Completed;
				webClient.DownloadProgressChanged += ProgressChanged;
				Console.WriteLine("Скачиваю MySQL");
				if (!Directory.Exists(Path.Combine(zipFilePath, "MySQL")))
					Directory.CreateDirectory(Path.Combine(zipFilePath, "MySQL"));
				webClient.DownloadFile(
					"http://dev.mysql.com/get/Downloads/MySQL-5.5/mysql-5.5.9-win32.zip/from/http://mysql.infocom.ua/",
					Path.Combine(zipFilePath, "MySQL", zipFileName));
				Console.WriteLine("Скачивание прошло успешно");
			}
			else
			{
				if (zipFileName == string.Empty)
				{
					Console.WriteLine("Введите путь к файлу с ZIP архивом MySql");
					handZipFileName = Console.ReadLine();
				}
			}
			try
			{
				var extractPath = Path.Combine(zipFilePath, "MySQL");
				var mysqlFolderName = string.Empty;
				zipFileName = string.IsNullOrEmpty(handZipFileName) ? Path.Combine(extractPath , zipFileName) : handZipFileName;
				using (var zip = ZipFile.Read(zipFileName))
				{
					mysqlFolderName = Path.GetDirectoryName(zip.Entries.First().FileName).Split('\\').First();
					foreach (var e in zip)
					{
						e.Extract(extractPath, ExtractExistingFileAction.OverwriteSilently);
					}
				}
				Console.WriteLine("Файл разархивирован");

				var readKey = Registry.CurrentUser.OpenSubKey("Environment", true);
				var loadString = (string)readKey.GetValue("PATH");
				if (loadString.Split(';').Where(s => s == Path.Combine(extractPath, mysqlFolderName, "bin")).ToList().Count == 0)
				readKey.SetValue("PATH", loadString + Path.Combine(extractPath, mysqlFolderName, "bin") + ";");
				readKey.Close();

				Console.WriteLine("PATH обновлен");
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadLine();
			}

		}


		private static void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
		{
			Console.WriteLine(e.ProgressPercentage);
		}

		private static void Completed(object sender, AsyncCompletedEventArgs e)
		{
			Console.WriteLine("Download completed!");
		}

	}
}
