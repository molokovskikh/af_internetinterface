using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using System.Xml;

namespace Inforoom2.Components
{
	/// <summary>
	/// Редактор html-документа после обработки запроса
	/// </summary>
	public class HtmlSniffer
	{
		private Controller currentController;
		private string currentText = "";

		/// <summary>
		/// Метод, редактирубщий html 
		/// </summary>
		/// <param name="controller">текущий контроллер</param>
		/// <param name="html">html-документ</param>
		/// <returns></returns>
		public delegate string GetChangedHtml(Controller controller, string html);

		/// <summary>
		/// Инициализация редактора
		/// </summary>
		/// <param name="controller">Текущий контроллер</param>
		public HtmlSniffer(Controller controller)
		{
			currentController = controller;
		}

		/// <summary>
		/// Попытка активировать редактор
		/// </summary>
		/// <param name="HttpContext"></param>
		/// <param name="htmlEdit">Метод(ы) редактирующие html</param>
		public void TryActivate(HttpContextBase HttpContext, GetChangedHtml htmlEdit)
		{
			if (IsPermitted()) {
				if (currentController == null) return;
				//чтение html
				ReadText();
				//редактирование html
				foreach (var method in htmlEdit.GetInvocationList()) {
					var run = method as GetChangedHtml;
					currentText = run.Invoke(currentController, currentText);
				}
				//запись html
				WriteText(currentText);
			}
		}


		private void ReadText()
		{
			var controllerContext = currentController.Request.RequestContext;
			var result = ViewEngines.Engines.FindView(currentController.ControllerContext,
				currentController.RouteData.Values["action"].ToString(), null);
			if (result == null || result.View == null) {
				currentText = "";
				return;
			}
			StringWriter currentOutput;
			using (currentOutput = new StringWriter()) {
				var viewContext = new ViewContext(currentController.ControllerContext, result.View, currentController.ViewData,
					currentController.TempData, currentOutput);
				result.View.Render(viewContext, currentOutput);
				result.ViewEngine.ReleaseView(currentController.ControllerContext, result.View);
			}

			currentText = currentOutput.ToString();
		}

		//запись html
		private void WriteText(string text)
		{
			if (IsPermitted()) {
				//currentController.HttpContext.Response.Output.WriteLine(text);
				currentController.HttpContext.Response.Output.Write(text);
			}
		}

		/// <summary>
		/// Нельзя заменять TextWriter ответов типа изображений, файлов
		/// </summary>
		/// <returns>Разрешение изменять содержимое html-документа</returns>
		private bool IsPermitted()
		{
			//проверка возвращаемого значения, отрабатываемого метода (это должен быть ActionResult)
			var currentAction = currentController.RouteData.Values["action"].ToString().ToLower();
			if (!currentController.RouteData.Values.ContainsKey("action")) {
				return false;
			}
			var cMethods = currentController.GetType().GetMethods();
			bool actionIsOk = cMethods.Any(
				s =>
					s.Name.ToLower() == currentAction &&
					(s.ReturnType == typeof (ActionResult) || s.ReturnType == typeof (Task<ActionResult>)));
			//если это так и 
			actionIsOk = actionIsOk
				//нет переадресации,
			             && string.IsNullOrEmpty(currentController.HttpContext.Response.RedirectLocation)
				//тип ожидаемого ответа "text/html"
			             && currentController.HttpContext.Response.ContentType == "text/html";
			//результат проверки
			return actionIsOk;
		}
	}
}