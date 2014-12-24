using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Inforoom2.Controllers;
using Moq;

namespace Inforoom2.Test.Components
{
public class ContextMocks
    {
        public Moq.Mock<HttpContextBase> HttpContext { get; set; }
        public Moq.Mock<HttpResponseBase> Response { get; set; }
        public Moq.Mock<HttpRequestBase> Request { get; set; }
        public RouteData RouteData { get; set; }
 
        public ContextMocks(ControllerBase controller)
        {
            HttpContext = new Mock<HttpContextBase>();
            Request = new Mock<HttpRequestBase>();
            Response = new Mock<HttpResponseBase>();
 
            HttpContext.Setup(x => x.Request).Returns(Request.Object);
            HttpContext.Setup(x => x.Response).Returns(Response.Object);
            HttpContext.Setup(x => x.Session).Returns(new FakeSession());
 
            Request.Setup(x => x.Cookies).Returns(new HttpCookieCollection());
            Response.Setup(x => x.Cookies).Returns(new HttpCookieCollection());
            Request.Setup(x => x.QueryString).Returns(new NameValueCollection());
            Request.Setup(x => x.Form).Returns(new NameValueCollection());
 
            var rc = new RequestContext(HttpContext.Object, new RouteData());
            controller.ControllerContext = new ControllerContext(rc, controller);
        }
 
        public void SetUser(IPrincipal user)
        {
            HttpContext.Setup(x => x.User).Returns(user);
        }
 
        public void IsAjaxRequest(bool b)
        {
            if (b)
                HttpContext.Setup(r => r.Request["X-Requested-With"]).Returns("XMLHttpRequest");
            else
                HttpContext.Setup(r => r.Request["X-Requested-With"]).Returns("");
        }
 
        private class FakeSession : HttpSessionStateBase
        {
            readonly Dictionary<string, object> _items = new Dictionary<string, object>();
 
            public override object this[string name]
            {
                get { return _items.ContainsKey(name) ? _items[name] : null; }
                set
                {
                    _items[name] = value;
                }
            }
        }
    }
}