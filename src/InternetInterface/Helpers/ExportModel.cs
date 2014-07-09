﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Common.Web.Ui.Excel;
using Common.Web.Ui.Helpers;
using ExcelLibrary.SpreadSheet;
using InternetInterface.Controllers;
using InternetInterface.Models;
using InternetInterface.Queries;
using NPOI.HSSF.UserModel;
using NPOI.SS.Util;

namespace InternetInterface.Helpers
{
	public class ExportModel
	{
		public static byte[] GetClients(SearchFilter filter, IList<ClientInfo> clients)
		{
			filter.ExportInExcel = true;

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

		public static byte[] GetWriteOffsExcel(WriteOffsFilter filter)
		{
			var book = new HSSFWorkbook();

			var sheet = book.CreateSheet("Выгрузка списаний клиентов за период");

			var row = 0;
			var headerStyle = NPOIExcelHelper.GetHeaderStype(book);
			var dataStyle = NPOIExcelHelper.GetDataStyle(book);
			var sheetRow = sheet.CreateRow(row++);
			NPOIExcelHelper.FillNewCell(sheetRow, 0, "Выгрузка списаний клиентов за период", headerStyle);
			sheetRow = sheet.CreateRow(row++);
			NPOIExcelHelper.FillNewCell(sheetRow, 0, String.Format("Период: с {0} по {1}",
				filter.BeginDate.ToString("dd.MM.yyyy"),
				filter.EndDate.ToString("dd.MM.yyyy")),
				book.CreateCellStyle());
			sheetRow = sheet.CreateRow(row++);
			NPOIExcelHelper.FillNewCell(sheetRow, 0, String.Format("Тип клиентов: {0}", filter.ClientType.GetDescription()), book.CreateCellStyle());
			sheetRow = sheet.CreateRow(row++);
			var region = filter.Session.Get<RegionHouse>(filter.Region);
			NPOIExcelHelper.FillNewCell(sheetRow, 0, String.Format("Регион: {0}", region == null ? "Все" : region.Name), book.CreateCellStyle());
			sheetRow = sheet.CreateRow(row++);
			NPOIExcelHelper.FillNewCell(sheetRow, 0, String.Format("Клиент: {0}", filter.Name), book.CreateCellStyle());
			sheet.CreateRow(row++);
			var tableHeaderRow = row;
			sheetRow = sheet.CreateRow(row++);
			NPOIExcelHelper.FillNewCell(sheetRow, 0, "Код клиента", headerStyle);
			NPOIExcelHelper.FillNewCell(sheetRow, 1, "Наименование клиента", headerStyle);
			NPOIExcelHelper.FillNewCell(sheetRow, 2, "Регион", headerStyle);
			NPOIExcelHelper.FillNewCell(sheetRow, 3, "Сумма", headerStyle);
			NPOIExcelHelper.FillNewCell(sheetRow, 4, "Дата", headerStyle);
			NPOIExcelHelper.FillNewCell(sheetRow, 5, "Комментарий", headerStyle);

			var items = filter.ToExcel().Cast<WriteOffsItem>();
			foreach (var item in items) {
				sheetRow = sheet.CreateRow(row++);
				NPOIExcelHelper.FillNewCell(sheetRow, 0, item.ClientId, dataStyle);
				NPOIExcelHelper.FillNewCell(sheetRow, 1, item.Name, dataStyle);
				NPOIExcelHelper.FillNewCell(sheetRow, 2, item.Region, dataStyle);
				NPOIExcelHelper.FillNewCell(sheetRow, 3, item.Sum, dataStyle);
				NPOIExcelHelper.FillNewCell(sheetRow, 4, item.Date, dataStyle);
				NPOIExcelHelper.FillNewCell(sheetRow, 5, item.Comment, dataStyle);
			}

			// добавляем автофильтр
			sheet.SetAutoFilter(new CellRangeAddress(tableHeaderRow, row, 0, 5));

			sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, 5));

			// устанавливаем ширину столбцов
			for (int i = 0; i < 6; i++) {
				sheet.SetColumnWidth(i, sheet.GetColumnWidth(i) * 2);
			}

			sheet.SetColumnWidth(1, sheet.GetColumnWidth(1) * 3);
			sheet.SetColumnWidth(5, sheet.GetColumnWidth(5) * 3);

			var buffer = new MemoryStream();
			book.Write(buffer);
			return buffer.ToArray();
		}
	}
}