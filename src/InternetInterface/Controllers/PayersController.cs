using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services.Description;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;

namespace InternetInterface.Controllers
{
    public class AgentFilter : IPaginable
    {
        public uint agent { get; set; }
        public DateTime? startDate { get; set; }
        public DateTime? endDate { get; set; }

        public int _lastRowsCount;
        public decimal TotalSum;

        public int RowsCount
        {
            get { return _lastRowsCount; }
        }

        public int PageSize { get { return 20; } }

        public int CurrentPage { get; set; }

        public string[] ToUrl()
        {
            return new[] {
                             String.Format("filter.agent={0}", agent),
                             String.Format("filter.startDate={0}", startDate),
                             String.Format("filter.endDate={0}", endDate)
                         };
        }

        public string ToUrlQuery()
        {
            return string.Join("&", ToUrl());
        }

        public string GetUri()
        {
            return ToUrlQuery();
        }

        public List<Payment> Find()
        {
            var thisD = DateTime.Now;
            if (startDate == null)
                startDate = new DateTime(thisD.Year, thisD.Month, 1);
            if (endDate == null)
                endDate = DateTime.Now;
            if (!CategorieAccessSet.AccesPartner("SSI"))
                agent = Agent.Queryable.Where(a => a.Partner == InithializeContent.partner).FirstOrDefault().Id;
            var totalRes = agent > 0 ?
                Payment.Queryable.Where(t => t.Agent.Id == agent).ToList() : Payment.FindAll().ToList();
            totalRes = totalRes.Where(t => t.PaidOn >= startDate.Value &&
                                           t.PaidOn <= endDate.Value.AddHours(23).AddMinutes(59) && t.Sum != 0 &&
                                           t.Client.PhysicalClient != null).ToList();
            _lastRowsCount = totalRes.Count();
            TotalSum = totalRes.Sum(h => h.Sum);
            if (_lastRowsCount > 0)
            {
                var getCount = _lastRowsCount - PageSize * CurrentPage < PageSize ? _lastRowsCount - PageSize * CurrentPage : PageSize;
                return
                    totalRes.GetRange(PageSize*CurrentPage, getCount);
            }
            return new List<Payment>();
        }
    }

	[Layout("Main")]
    [Helper(typeof(PaginatorHelper))]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class PayersController : SmartDispatcherController
	{
		public void Filter()
		{
			PropertyBag["Registrators"] = Partner.FindAll();
			PropertyBag["registrId"] = Partner.FindFirst().Id;
		}

        public void AgentFilter([DataBind("filter")]AgentFilter filter)
		{
            var thisD = DateTime.Now;
            if (filter.startDate == null)
                filter.startDate = new DateTime(thisD.Year, thisD.Month, 1);
            if (filter.endDate == null)
                filter.endDate = DateTime.Now;
            PropertyBag["filter"] = filter;
			PropertyBag["agents"] = Agent.FindAll();
			PropertyBag["agentId"] = filter.agent;
            PropertyBag["colorId"] = 0;
		}

		public void Show(uint registrator)
		{
			PropertyBag["Registrators"] = Partner.FindAll();
			PropertyBag["registrId"] = registrator;
			PropertyBag["Payers"] = Client.Queryable.Where(p => p.WhoRegistered.Id == registrator && p.PhysicalClient != null);
		}

        public void ShowAgent([DataBind("filter")]AgentFilter filter)
		{
			PropertyBag["agents"] = Agent.FindAll();
            PropertyBag["agentId"] = filter.agent;
            var payments = filter.Find();
            PropertyBag["filter"] = filter;
			PropertyBag["Payments"] = payments;
			PropertyBag["TotalSumm"] = filter.TotalSum;
            if (filter.startDate.Value.Month == filter.endDate.Value.Month)
                PropertyBag["colorId"] = filter.startDate.Value.Month;
            else
                PropertyBag["colorId"] = 0;
		}

        public void NewPaymets()
        {
            if (IsPost)
            {
                var file = Request.Files["inputfile"] as HttpPostedFile;
                if (file == null || file.ContentLength == 0)
                {
                    //PropertyBag["Message"] = Message.Error("Нужно выбрать файл для загрузки");
                    return;
                }

                Session["payments"] = BankPayment.Parse(file.FileName, file.InputStream);
                RedirectToReferrer();
            }
            else
            {
                PropertyBag["payments"] = Session["payments"];
            }
        }
	}
}