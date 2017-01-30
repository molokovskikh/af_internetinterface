using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Common.Tools;
using Common.Tools.Calendar;
using ExcelLibrary.SpreadSheet;
using Inforoom2.Components;
using Inforoom2.Models;
using InforoomControlPanel.Models;
using NHibernate;
using NHibernate.Linq;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using Remotion.Linq.Clauses;
using BorderStyle = NPOI.SS.UserModel.BorderStyle;
using CellFormat = NPOI.SS.Format.CellFormat;
using CellFormatType = NPOI.SS.Format.CellFormatType;
using Font = System.Drawing.Font;
using HorizontalAlignment = ExcelLibrary.SpreadSheet.HorizontalAlignment;
using VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment;

namespace InforoomControlPanel.ReportTemplates
{
	public class PaymentsReport
	{

		public static byte[] GetGeneralReport(Controller controller,
			ref InforoomModelFilter<Payment> pager,
			bool dateNecessary = true)
		{
			if (string.IsNullOrEmpty(pager.GetParam("orderBy")))
				pager.SetOrderBy("PaidOn", OrderingDirection.Asc);

			pager.ParamDelete("filter.Equal.IsDuplicate");
			pager.ParamSet("filter.Equal.IsDuplicate", false.ToString());

			var selectedMonth = pager.GetParam("selectedMonth");
			var selectedYear = pager.GetParam("selectedYear");
			pager.ParamDelete("selectedYear");
			pager.ParamDelete("selectedMonth");
			DateTime selectedDate = DateTime.MinValue;
			if (!string.IsNullOrEmpty(selectedMonth) && !string.IsNullOrEmpty(selectedYear)) {
				DateTime.TryParse($"01.{selectedMonth}.{selectedYear}", out selectedDate);
				if (selectedDate != DateTime.MinValue) {
					pager.ParamDelete("filter.GreaterOrEqueal.PaidOn");
					pager.ParamDelete("filter.LowerOrEqual.PaidOn");
					pager.ParamSet("filter.GreaterOrEqueal.PaidOn", selectedDate.Date.FirstDayOfMonth().ToString("dd.MM.yyyy"));
					pager.ParamSet("filter.LowerOrEqual.PaidOn", selectedDate.Date.LastDayOfMonth().ToString("dd.MM.yyyy"));
				}
			}

			if (string.IsNullOrEmpty(pager.GetParam("filter.GreaterOrEqueal.PaidOn")) &&
			    string.IsNullOrEmpty(pager.GetParam("filter.LowerOrEqual.PaidOn"))) {
				pager.ParamDelete("filter.GreaterOrEqueal.PaidOn");
				pager.ParamDelete("filter.LowerOrEqual.PaidOn");
				pager.ParamSet("filter.GreaterOrEqueal.PaidOn", SystemTime.Now().Date.FirstDayOfMonth().ToString("dd.MM.yyyy"));
				pager.ParamSet("filter.LowerOrEqual.PaidOn", SystemTime.Now().Date.ToString("dd.MM.yyyy"));
			}
			var criteria = pager.GetCriteria();

			if (pager.IsExportRequested()) {
				//формирование шапки (отправляем значение строк в обратном порядке)
				var header = new List<string>();
				//строки между шапкой и содержанием 
				header.Add("");
				header.Add("Всего строк: " + pager.TotalItems);
				header.Add("");
				//Тип 
				if (pager.GetParam("filter.IsNotNull.Client.PhysicalClient") != null)
					header.Add(string.Format("Тип клиента: {0}",
						pager.GetParam("filter.IsNotNull.Client.PhysicalClient") == "1"
							? "Физ.лицо"
							: pager.GetParam("filter.IsNotNull.Client.PhysicalClient") != "" ? "Юр.лицо" : "Все"));
				//Регион
				if (pager.GetParam("clientregionfilter.Client") != null)
					header.Add(string.Format("Регион: {0}", pager.GetParam("clientregionfilter.Client")));
				//Дата списания
				if (pager.GetParam("filter.GreaterOrEqueal.PaidOn") != null)
					header.Add(string.Format("Дата поступления: {0}", pager.GetParam("filter.GreaterOrEqueal.PaidOn")));
				//поля шапки
				header.Add("");
				header.Add("Отчет по списаниям - " + SystemTime.Now().ToString("dd.MM.yyyy HH:mm"));
				pager.SetHeaderLines(header);

				//формирование блока выгрузки
				pager.SetExportFields(
					s => new
					{
						ЛС = s.Client.Id,
						Клиент = s.Client.Fullname,
						Регион = s.Client.GetRegion().Name,
						Сумма = s.Sum,
						Дата = s.PaidOn,
						Бонусный = s.Virtual == true ? "Да" : "Нет",
						Комментарий = s.Comment != null ? s.Comment : ""
					}, complexLinq: true);
                //выгрузка в файл
                return pager.ExportToExcelFile();
			}
			controller.ViewBag.Pager = pager;
			return null;
		}

		private static ICell FillNewCell(IRow row, int collumnIndex, object value, ICellStyle style)
		{
			var cell = row.CreateCell(collumnIndex);
			cell.CellStyle = style;
			if (value is string) {
				cell.SetCellValue(value.ToString());
			} else {
				cell.SetCellValue(double.Parse(value.ToString()) );
			}
			return cell;
		}

		public static byte[] GetGeneralReportEmployeesByGroups(ISession dbSession, ref List<ViewModelEmployeeGroupPaymentsReport> result, DateTime dateFrom, DateTime dateTo, int groupId = 0,
			int employeeId = 0, bool ignoreZero = true, bool excelGet = false, Employee currentEmployee = null)
		{
			result= new List<ViewModelEmployeeGroupPaymentsReport>();
			var groupList = new List<EmployeeGroup>();
			var query = dbSession.Query<EmployeeGroup>();
			if (groupId!=0) {
				groupList = query.Where(s => s.Id == groupId).ToList();
			} else
			{
				groupList = query.OrderBy(s=>s.Name).ToList();
			}
			dateTo = dateTo.Date.AddDays(1).AddSeconds(-1);
			foreach (var group in groupList) {
				foreach (
					var employee in (employeeId == 0 ? group.EmployeeList : group.EmployeeList.Where(s => s.Id == employeeId).OrderBy(s=>s.Name).ToList())
					)
				{
					var virtualSum = dbSession.Query<Payment>()
							.Where(s => s.Employee.Id == employee.Id && s.PaidOn >= dateFrom && s.PaidOn <= dateTo && s.Virtual.Value == true && s.IsDuplicate == false).ToList()
							.Sum(s => s.Sum);
					var sum =
						dbSession.Query<Payment>()
							.Where(s => s.Employee.Id == employee.Id && (s.Virtual == null || s.Virtual == false) && s.PaidOn >= dateFrom && s.PaidOn <= dateTo && s.IsDuplicate == false).ToList()
							.Sum(s => s.Sum);
					if (sum == 0 && virtualSum == 0 && ignoreZero) {
						continue;
					}

					var newItem = new ViewModelEmployeeGroupPaymentsReport();
					newItem.EmployeeGroup = group;
					newItem.Employee = employee;
					newItem.Sum = sum;
					newItem.VirtualSum = virtualSum;
					result.Add(newItem);
				}
			}

			var virtualSumSystem = dbSession.Query<Payment>()
					.Where(s => s.Employee == null && s.PaidOn >= dateFrom && s.PaidOn <= dateTo && s.Virtual.Value == true && s.IsDuplicate == false).ToList()
					.Sum(s => s.Sum);
			var newItemvirtualSumSystem = new ViewModelEmployeeGroupPaymentsReport();
			newItemvirtualSumSystem.Sum = 0;
			newItemvirtualSumSystem.VirtualSum = virtualSumSystem;

			if (!excelGet) {
				if (groupId == 0 && employeeId == 0) {
					result.Add(newItemvirtualSumSystem);
				}
			} else { 
			var startIndex = 0;
				//создаем новый xls файл 
				var wb = new HSSFWorkbook();
				var ws = wb.CreateSheet("Первый лист");

				var defaultFont = wb.CreateFont();
				defaultFont.Boldweight = (short)FontBoldWeight.Normal;

				var defaultFontBold = wb.CreateFont();
				defaultFontBold.Boldweight = (short)FontBoldWeight.Bold;

				var defaultSyle = wb.CreateCellStyle();
				defaultSyle.BorderRight = BorderStyle.Thin;
				defaultSyle.BorderLeft = BorderStyle.Thin;
				defaultSyle.BorderBottom = BorderStyle.Thin;
				defaultSyle.BorderTop = BorderStyle.Thin;
				//defaultSyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Left;
				defaultSyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.White.Index;
				defaultSyle.FillPattern = FillPattern.SolidForeground;
				defaultSyle.SetFont(defaultFont);
				defaultSyle.WrapText = true;

				var groupTextSyle = wb.CreateCellStyle();
				groupTextSyle.CloneStyleFrom(defaultSyle);
				groupTextSyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Yellow.Index;
				
				var totalSyle = wb.CreateCellStyle();
				totalSyle.CloneStyleFrom(defaultSyle);
				totalSyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.BrightGreen.Index;

				var totalSyleBold = wb.CreateCellStyle();
				totalSyleBold.CloneStyleFrom(defaultSyle);
				totalSyleBold.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.00");
				totalSyleBold.SetFont(defaultFontBold);
				totalSyleBold.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Right;
				totalSyleBold.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.BrightGreen.Index;

				var groupSumSyle = wb.CreateCellStyle();
				groupSumSyle.CloneStyleFrom(defaultSyle);
				groupSumSyle.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.00");
				groupSumSyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Grey25Percent.Index;

				var groupSumSyleBold = wb.CreateCellStyle();
				groupSumSyleBold.CloneStyleFrom(defaultSyle);
				groupSumSyleBold.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.00");
				groupSumSyleBold.SetFont(defaultFontBold);
				groupSumSyleBold.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Right;
				groupSumSyleBold.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Grey25Percent.Index;


				var allSumSyle = wb.CreateCellStyle();
				allSumSyle.CloneStyleFrom(defaultSyle);
				allSumSyle.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.00");
				allSumSyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
				allSumSyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.SkyBlue.Index;

				var allSumSyleBold = wb.CreateCellStyle();
				allSumSyleBold.CloneStyleFrom(defaultSyle);
				allSumSyleBold.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.00");
				allSumSyleBold.SetFont(defaultFontBold);
				allSumSyleBold.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Right;
				allSumSyleBold.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.SkyBlue.Index;


				// проходим строки
				decimal totalSum = 0m;
				decimal totalSumVirtual = 0m;


				var groupResult = result.Select(s => s.EmployeeGroup).GroupBy(s => s.Id).Select(s=>s.FirstOrDefault()).OrderBy(s=>s.Name).ToList();
				for (int i = 0; i < groupResult.Count(); i++) {
					
					decimal groupSum = 0m;
					decimal groupSumVirtual = 0m;
					var rows = result.Where(s => s.EmployeeGroup.Id == groupResult[i].Id).OrderBy(s => s.Employee.Name).ToList();
					
					startIndex++;
					var currentColumn = 0;
					var currentRow = ws.CreateRow(startIndex);
					FillNewCell(currentRow, currentColumn++, groupResult[i].Name, groupTextSyle);
					FillNewCell(currentRow, currentColumn++, "", groupTextSyle);
					FillNewCell(currentRow, currentColumn++, "", groupTextSyle);

					//если это значение число, нужно поместить в ячейку соответствующего типа
					for (int j = 0; j < rows.Count; j++) {
						groupSum += rows[j].Sum;
						groupSumVirtual += rows[j].VirtualSum;

						startIndex++;
						currentColumn = 0;
						currentRow = ws.CreateRow(startIndex);
						FillNewCell(currentRow, currentColumn++, rows[j].Employee.Name, defaultSyle);
						FillNewCell(currentRow, currentColumn++, rows[j].Sum, defaultSyle);
						FillNewCell(currentRow, currentColumn++, rows[j].VirtualSum, defaultSyle);
					}
					totalSum += groupSum;
					totalSumVirtual += groupSumVirtual;

					startIndex++;
					currentColumn = 0;
					currentRow = ws.CreateRow(startIndex);
					FillNewCell(currentRow, currentColumn++, "Итого:", groupSumSyleBold);
					FillNewCell(currentRow, currentColumn++, groupSum, groupSumSyle);
					FillNewCell(currentRow, currentColumn++, groupSumVirtual, groupSumSyle);
				}

				var currentColumnLast = 0;
				IRow currentRowLast;
				if (groupId == 0 && employeeId == 0) {
					totalSumVirtual += newItemvirtualSumSystem.VirtualSum;
					startIndex++;
					currentRowLast = ws.CreateRow(startIndex);
					FillNewCell(currentRowLast, currentColumnLast++, "Бонусные платежи — Система:", defaultSyle);
					FillNewCell(currentRowLast, currentColumnLast++, "", defaultSyle);
					FillNewCell(currentRowLast, currentColumnLast++, newItemvirtualSumSystem.VirtualSum, defaultSyle);
				}

				startIndex++;
				currentColumnLast = 0;
				currentRowLast = ws.CreateRow(startIndex);
				FillNewCell(currentRowLast, currentColumnLast++, "Сумма:", totalSyleBold);
				FillNewCell(currentRowLast, currentColumnLast++, totalSum, totalSyleBold);
				FillNewCell(currentRowLast, currentColumnLast++, totalSumVirtual, totalSyleBold);

				startIndex++;
				currentColumnLast = 0;
				currentRowLast = ws.CreateRow(startIndex);
				FillNewCell(currentRowLast, currentColumnLast++, "Всего:", allSumSyleBold);
				FillNewCell(currentRowLast, currentColumnLast++, (totalSum+ totalSumVirtual), allSumSyle);
				FillNewCell(currentRowLast, currentColumnLast++, "", allSumSyle);
				
					ws.AddMergedRegion(new CellRangeAddress(startIndex, startIndex,1,2));

				wb.GetSheetAt(0).AutoSizeColumn(0);
				wb.GetSheetAt(0).AutoSizeColumn(1);
				wb.GetSheetAt(0).AutoSizeColumn(2);


				var buffer = new MemoryStream();
				wb.Write(buffer);
				return buffer.ToArray();
			}
			return null;
		}
	}
}