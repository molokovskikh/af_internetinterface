using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Common.Web.Ui.Excel;
using ExcelLibrary.SpreadSheet;
using InternetInterface.Models;
using InternetInterface.Queries;

namespace InternetInterface.Helpers
{
	public class ExportModel
	{
		public static byte[] GetClients(SeachFilter filter)
		{
			filter.ExportInExcel = true;
			var clients = filter.Find();

			Workbook wb = new Workbook();
			Worksheet ws = new Worksheet("Статистика по клиентам");
			int row = 7;
			int colShift = 0;

			ws.Merge(0, 0, 0, 9);
			ExcelHelper.WriteHeader1(ws, 0, 0, "Статистика по клиентам", false, true);

			ws.Merge(1, 1, 1, 2);
			ExcelHelper.Write(ws, 1, 0, "Строка поиска:", false);
			ExcelHelper.Write(ws, 1, 1, filter.SearchText, false);

			ws.Merge(2, 1, 2, 2);
			ExcelHelper.Write(ws, 2, 0, "Искать по:", false);
			ExcelHelper.Write(ws, 2, 1, filter.SearchProperties.GetDescription(), false);

			ws.Merge(3, 1, 3, 2);
			ExcelHelper.Write(ws, 3, 0, "Тип клиента:", false);
			ExcelHelper.Write(ws, 3, 1, filter.ClientTypeFilter.GetDescription(), false);

			ws.Merge(4, 1, 4, 2);
			ExcelHelper.Write(ws, 4, 0, "Статус:", false);
			if (filter.StatusType > 0)
				ExcelHelper.Write(ws, 4, 1, ((StatusType)filter.StatusType).GetDescription(), false);
			else
				ExcelHelper.Write(ws, 4, 1, "Все", false);

			ws.Merge(5, 1, 5, 2);
			ExcelHelper.Write(ws, 5, 0, "Активность:", false);
			ExcelHelper.Write(ws, 5, 1, filter.EnabledTypeProperties.GetDescription(), false);

			foreach (var item in clients) {
				ExcelHelper.Write(ws, row, colShift + 0, item.client.Id, true);
				ExcelHelper.Write(ws, row, colShift + 1, item.client.Name, true);
				ExcelHelper.Write(ws, row, colShift + 2, item.Address, true);
				ExcelHelper.Write(ws, row, colShift + 3, item.client.ForSearchContactNoLight(filter.SearchText), true);
				ExcelHelper.Write(ws, row, colShift + 4, item.client.RegDate, true);
				ExcelHelper.Write(ws, row, colShift + 5, item.client.GetTariffName(), true);
				ExcelHelper.Write(ws, row, colShift + 6, item.client.Balance, true);
				ExcelHelper.Write(ws, row, colShift + 7, item.client.Status.Type.GetDescription(), true);

				row++;
			}
			FormatClientsStatisticXls(ws);

			wb.Worksheets.Add(ws);
			using (var ms = new MemoryStream()) {
				wb.Save(ms);
				return ms.ToArray();
			}
			return new byte[0];
		}

		private static void FormatClientsStatisticXls(Worksheet ws)
		{
			int headerRow = 7;
			ExcelHelper.WriteHeader1(ws, headerRow, 0, "Номер счета", true, true);
			ExcelHelper.WriteHeader1(ws, headerRow, 1, "ФИО", true, true);
			ExcelHelper.WriteHeader1(ws, headerRow, 2, "Адрес подключения", true, true);
			ExcelHelper.WriteHeader1(ws, headerRow, 3, "Контактная информация", true, true);
			ExcelHelper.WriteHeader1(ws, headerRow, 4, "Дата регистрации", true, true);
			ExcelHelper.WriteHeader1(ws, headerRow, 5, "Тариф", true, true);
			ExcelHelper.WriteHeader1(ws, headerRow, 6, "Баланс", true, true);
			ExcelHelper.WriteHeader1(ws, headerRow, 7, "Статус", true, true);

			ws.Cells.ColumnWidth[0] = 4000;
			ws.Cells.ColumnWidth[1] = 10000;
			ws.Cells.ColumnWidth[2] = 15000;
			ws.Cells.ColumnWidth[3] = 4000;
			ws.Cells.ColumnWidth[4] = 5000;
			ws.Cells.ColumnWidth[5] = 4000;
			ws.Cells.ColumnWidth[6] = 3000;
			ws.Cells.ColumnWidth[7] = 4000;

			ws.Cells.Rows[headerRow].Height = 514;
		}

		public static byte[] GetWriteOffs(WriteOffsFilter filter)
		{
			filter.ExportInExcel = true;
			var writeOffs = filter.Find().Cast<WriteOffsItem>();

			Workbook wb = new Workbook();
			Worksheet ws = new Worksheet("Выгрузка списаний");
			int row = 8;
			int colShift = 0;

			ws.Merge(0, 0, 0, 9);
			ExcelHelper.WriteHeader1(ws, 0, 0, "Выгрузка списаний", false, true);

			ws.Merge(1, 1, 1, 2);
			ExcelHelper.Write(ws, 1, 0, "Строка поиска:", false);
			ExcelHelper.Write(ws, 1, 1, filter.Name, false);

			ws.Merge(2, 1, 2, 2);
			ExcelHelper.Write(ws, 2, 0, "Искать по:", false);
			ExcelHelper.Write(ws, 2, 1, filter.ClientType.GetDescription(), false);

			ws.Merge(3, 1, 3, 2);
			ExcelHelper.Write(ws, 3, 0, "Регион:", false);
			var region = filter.Session.Get<RegionHouse>(filter.Region);
			ExcelHelper.Write(ws, 3, 1, region.Name, false);

			ws.Merge(4, 1, 4, 2);
			ExcelHelper.Write(ws, 4, 0, "Интервал дат:", false);
			ExcelHelper.Write(ws, 4, 1, string.Format("C {0} по {1}", filter.BeginDate.ToShortDateString(), filter.EndDate.ToShortDateString()), false);

			foreach (var item in writeOffs) {
				ExcelHelper.Write(ws, row, colShift + 0, item.ClientId, true);
				ExcelHelper.Write(ws, row, colShift + 1, item.Name, true);
				ExcelHelper.Write(ws, row, colShift + 2, item.Region, true);
				ExcelHelper.Write(ws, row, colShift + 3, item.Sum, true);
				ExcelHelper.Write(ws, row, colShift + 4, item.Date, true);
				ExcelHelper.Write(ws, row, colShift + 5, item.Comment, true);

				row++;
			}
			FormatWriteOffsStatisticXls(ws);

			wb.Worksheets.Add(ws);
			using (var ms = new MemoryStream()) {
				wb.Save(ms);
				return ms.ToArray();
			}
		}

		private static void FormatWriteOffsStatisticXls(Worksheet ws)
		{
			int headerRow = 7;
			ExcelHelper.WriteHeader1(ws, headerRow, 0, "Код клиента", true, true);
			ExcelHelper.WriteHeader1(ws, headerRow, 1, "Клиент", true, true);
			ExcelHelper.WriteHeader1(ws, headerRow, 2, "Регион", true, true);
			ExcelHelper.WriteHeader1(ws, headerRow, 3, "Сумма", true, true);
			ExcelHelper.WriteHeader1(ws, headerRow, 4, "Дата", true, true);
			ExcelHelper.WriteHeader1(ws, headerRow, 5, "Комментарий", true, true);

			ws.Cells.ColumnWidth[0] = 4000;
			ws.Cells.ColumnWidth[1] = 15000;
			ws.Cells.ColumnWidth[2] = 6000;
			ws.Cells.ColumnWidth[3] = 4000;
			ws.Cells.ColumnWidth[4] = 6000;
			ws.Cells.ColumnWidth[5] = 10000;

			ws.Cells.Rows[headerRow].Height = 514;
		}
	}
}