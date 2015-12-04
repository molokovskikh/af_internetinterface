using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using Common.Tools;
using Inforoom2.Components;
using Inforoom2.Helpers;
using Inforoom2.Models;
using log4net;
using NHibernate;
using NHibernate.Context;
using NHibernate.Linq;

namespace Inforoom2.Controllers
{
	/// <summary>
	/// Базовый контроллер от которого все наследуются
	/// </summary>
	public class BaseController : Controller
	{
		public ISession DbSession;
		protected static readonly ILog log = LogManager.GetLogger(typeof(BaseController));

		protected ValidationRunner ValidationRunner;

		public BaseController()
		{
			//Hibernate
			var controller = this;
			var SessionFactory = MvcApplication.SessionFactory;
			if (!CurrentSessionContext.HasBind(SessionFactory)) {
				var session = SessionFactory.OpenSession();
				CurrentSessionContext.Bind(session);
				session.BeginTransaction();
				controller.DbSession = session;
			}
			else if (controller.DbSession == null)
				controller.DbSession = MvcApplication.SessionFactory.GetCurrentSession();

			EntityBinder.SetSession(DbSession);

			//Additional
			ValidationRunner = ViewBag.Validation ?? new ValidationRunner(DbSession);
			ViewBag.Validation = ValidationRunner;

			ViewBag.JavascriptParams = new Dictionary<string, string>();
			var currentDate = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
			AddJavascriptParam("Timestamp", currentDate.ToString());
		}

		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			base.OnActionExecuting(filterContext);
			ViewBag.JavascriptParams["baseurl"] = String.Format("{0}://{1}{2}", Request.Url.Scheme, Request.Url.Authority, UrlHelper.GenerateContentUrl("~/", HttpContext));
			ViewBag.ActionName = filterContext.RouteData.Values["action"].ToString();
			ViewBag.ControllerName = GetType().Name.Replace("Controller", "");
		}

		public void AddJavascriptParam(string name, string value)
		{
			ViewBag.JavascriptParams[name] = value;
		}

		public string GetJavascriptParam(string name)
		{
			string val = null;
			ViewBag.JavascriptParams.TryGetValue(name, out val);
			return val;
		}

		public virtual Employee GetCurrentEmployee()
		{
			if (Session == null || DbSession == null || Session["employee"] == null)
				return null;
			var employeeId = Convert.ToInt32(Session["employee"]);
			return DbSession.Query<Employee>().FirstOrDefault(k => k.Id == employeeId);
		}


		public HttpSessionStateBase HttpSession
		{
			get { return Session; }
		}

		public void SuccessMessage(string message)
		{
			SetCookie("SuccessMessage", message);
		}

		public void ErrorMessage(string message)
		{
			SetCookie("ErrorMessage", message);
		}

		public void WarningMessage(string message)
		{
			SetCookie("WarningMessage", message);
		}


		protected override void OnException(ExceptionContext filterContext)
		{
			//Формируем сообщение об ошибке
			var builder = CollectDebugInfo();
			var msg = filterContext.Exception.ToString();
			builder.Append(msg);
			EmailSender.SendError(builder.ToString());

			var showErrorPage = false;
			bool.TryParse(ConfigurationManager.AppSettings["ShowErrorPage"], out showErrorPage);
			DeleteCookie("SuccessMessage");

			if (showErrorPage) {
				filterContext.Result = new RedirectToRouteResult(
					new RouteValueDictionary
					{ { "controller", "StaticContent" }, { "action", "Error" } });
				filterContext.ExceptionHandled = true;
			}

			log.ErrorFormat("{0} {1}", filterContext.Exception.Message, filterContext.Exception.StackTrace);
			if (DbSession == null)
				return;

			// Иногда транзакции надо закрывать отдельно, так как метод OnResultExecuted не будет вызван
			// Это случается, когда ошибка не в SQL запросе хибернейта, а в самом проекте
			if (DbSession.Transaction.IsActive)
				DbSession.Transaction.Rollback();
			if (DbSession.IsOpen)
				DbSession.Close();
		}

		protected virtual StringBuilder CollectDebugInfo()
		{
			return new StringBuilder("");
		}


		protected override void OnResultExecuted(ResultExecutedContext filterContext)
		{
			base.OnResultExecuted(filterContext);
			var session = DbSession;
			if (session == null)
				return;


			session.SafeTransactionCommit(filterContext.Exception);

			//Я не понимаю зачем нужна следующая команда
			session = CurrentSessionContext.Unbind(MvcApplication.SessionFactory);
			if (session.IsOpen)
				session.Close();
		}


		protected List<TModel> GetList<TModel>()
		{
			var entities = DbSession.Query<TModel>().ToList();
			if (entities.Count == 0) {
				entities = new List<TModel>();
			}
			return entities;
		}

		protected string GetCookie(string cookieName)
		{
			try {
				var cookie = Request.Cookies.Get(cookieName);
				var base64EncodedBytes = Convert.FromBase64String(cookie.Value);
				return Encoding.UTF8.GetString(base64EncodedBytes);
			}
			catch (Exception e) {
				return null;
			}
		}

		public void SetCookie(string name, string value)
		{
			if (value == null) {
				Response.Cookies.Add(new HttpCookie(name, "false") { Path = "/", Expires = SystemTime.Now() });
				return;
			}
			var plainTextBytes = Encoding.UTF8.GetBytes(value);
			var text = Convert.ToBase64String(plainTextBytes);
			Response.Cookies.Add(new HttpCookie(name, text) { Path = "/" });
		}

		public void DeleteCookie(string name)
		{
			Response.Cookies.Remove(name);
		}

		protected ActionResult Authenticate(string action, string controller, string username, bool shouldRemember,
			string userData = "")
		{
			var ticket = new FormsAuthenticationTicket(
				1,
				username,
				SystemTime.Now(),
				SystemTime.Now().AddMinutes(FormsAuthentication.Timeout.TotalMinutes),
				shouldRemember,
				userData,
				FormsAuthentication.FormsCookiePath);
			var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket));
			if (shouldRemember)
				cookie.Expires = SystemTime.Now().AddMinutes(FormsAuthentication.Timeout.TotalMinutes);
			Response.Cookies.Set(cookie);
			return RedirectToAction(action, controller, RouteData);
		}

		/// <summary>
		///  Получение текущим действием выходных данных запрашиваемого действия с заданными параметрами 
		/// </summary>
		/// <param name="controllerString">наименование контроллера</param>
		/// <param name="actionString">наименование действие</param>
		/// <param name="parameters">параметры действия</param>
		public void ForwardToAction(string controllerString, string actionString, object[] parameters = null)
		{
			var type = Assembly.GetExecutingAssembly().GetTypes().FirstOrDefault(t => t.Name == controllerString + "Controller");
			if (type == null) {
				ForwardToAction("Home", "Index", new object[0]);
				return;
			}

			// Получение сведений о контроллере, действиях и их параметрах
			var module = new UrlRoutingModule();
			var col = module.RouteCollection;
			HttpContext.RewritePath("/" + controllerString + "/" + actionString);
			var fakeRouteData = col.GetRouteData(HttpContext);

			// Создание нового контроллера, соответствующего запросу 
			var ctxt = new RequestContext(ControllerContext.HttpContext, fakeRouteData);
			var factory = ControllerBuilder.Current.GetControllerFactory();
			var iController = factory.CreateController(ctxt, controllerString);

			// Передача новому контроллеру данных, имеющихся в базовогом конроллере   
			var controller = iController as BaseController;
			controller.DbSession = DbSession;
			controller.ControllerContext = new ControllerContext(ctxt, this);
			var methodTypes = parameters == null ? new List<Type>() : parameters.Select(parameter => parameter.GetType()).ToList();
			var actionMethod = type.GetMethod(actionString, methodTypes.ToArray());
			if (actionMethod == null) {
				ForwardToAction("Home", "Index", new object[0]);
				return;
			}
			// Формирование данных, передающихся запрашиваемуму действию 
			var c = new ActionExecutingContext();
			c.HttpContext = ctxt.HttpContext;
			c.RouteData = ctxt.RouteData;
			controller.ViewData = ViewData;
			controller.OnActionExecuting(c);

			// Получение результата действия   
			var actionResult = (ActionResult)actionMethod.Invoke(controller, parameters);

			// Передача полученного результата текущему действию
			actionResult.ExecuteResult(controller.ControllerContext);
		}

		/// <summary>
		/// Безопасное удаление модели из базы данных. Удаляет модель, выводит на экран сообщение.
		/// В случае наличия связей у модели, выведет соответствующее сообщение.
		/// </summary>
		/// <typeparam name="TBaseModel">Тип модели</typeparam>
		/// <param name="id">Идентификатор модели</param>
		protected void SafeDelete<TBaseModel>(int id)
			where TBaseModel : BaseModel
		{
			var obj = DbSession.Get<TBaseModel>(id);
			if (DbSession.AttemptDelete(obj))
				SuccessMessage("Объект успешно удален!");
			else
				ErrorMessage("Объект не удалось удалить! Возможно уже был связан с другими объектами.");
		}
	}
}