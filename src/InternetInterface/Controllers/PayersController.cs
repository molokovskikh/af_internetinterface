using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
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

        private int _lastRowsCount;

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
                             String.Format("filter.beginDate={0}", startDate),
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
            _lastRowsCount =
                Payment.Queryable.Where(
                    t =>
                    t.Agent.Id == agent && t.PaidOn >= startDate.Value &&
                    t.PaidOn <= endDate.Value.AddHours(23).AddMinutes(59) && t.Sum != 0 &&
                    t.Client.PhysicalClient != null).Count();
            if (_lastRowsCount > 0)
            {
                var getCount = _lastRowsCount - PageSize * CurrentPage < PageSize ? _lastRowsCount - PageSize * CurrentPage : PageSize;
                return
                    Payment.Queryable.Where(
                        t =>
                        t.Agent.Id == agent && t.PaidOn >= startDate.Value &&
                        t.PaidOn <= endDate.Value.AddHours(23).AddMinutes(59) && t.Sum != 0).ToList().GetRange(
                            PageSize*CurrentPage, getCount);
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
            PropertyBag["filter"] = filter;
			PropertyBag["agents"] = Agent.FindAll();
			PropertyBag["agentId"] = Agent.FindFirst().Id;
		}

		public void Show(uint registrator)
		{
			PropertyBag["Registrators"] = Partner.FindAll();
			PropertyBag["registrId"] = registrator;
			PropertyBag["Payers"] = Clients.Queryable.Where(p => p.WhoRegistered.Id == registrator && p.PhysicalClient != null);
		}

        public void ShowAgent([DataBind("filter")]AgentFilter filter)
		{
			PropertyBag["agents"] = Agent.FindAll();
            PropertyBag["agentId"] = filter.agent;
            //var _startDate = DateTime.Parse(filter.startDate);
			//var _endDate = DateTime.Parse(endDate);
			//var payments = Payment.Queryable.Where(t => t.Agent.Id == agent && t.PaidOn >= _startDate && t.PaidOn <= _endDate.AddHours(23).AddMinutes(59) && t.Sum != 0 && t.Client.PhysicalClient != null).ToList();
            var payments = filter.Find();
            PropertyBag["filter"] = filter;
			PropertyBag["Payments"] = payments;
			PropertyBag["TotalSumm"] = payments.Sum(h => h.Sum);


		}
	}
}