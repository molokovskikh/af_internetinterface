using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom2.Models;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;

namespace InforoomControlPanel.Test.Functional.Ticket
{
    class EditCallMeBackTicketFixture : TicketFixture
    {
        [Test, Description("Изменение запроса на обратный звонок")]
        public void EditCallMeBackTicket()
        {
            //создаем запрос на обратный звонок
            var ticketNew = new CallMeBackTicket();
            ticketNew.PhoneNumber = "9685473196";
            ticketNew.Text = "Не работает интернет";
            ticketNew.Name = "Владимир";
            DbSession.Save(ticketNew);

            Open("Ticket/CallMeBackTicketIndex");
            var callMeBackTicket = DbSession.Query<CallMeBackTicket>().First(p => p.PhoneNumber == "9685473196");
            var targetTicket = browser.FindElementByXPath("//td[contains(.,'" + callMeBackTicket.PhoneNumber + "')]");
            var row = targetTicket.FindElement(By.XPath("../.."));
            var button = row.FindElement(By.CssSelector("a.btn-success"));
            button.Click();
            browser.FindElementByCssSelector(".btn-green").Click();
            AssertText("Запрос на обратный звонок успешно изменен");
            DbSession.Refresh(callMeBackTicket);
            var employee = DbSession.Query<Employee>().First(p => p.Login == Environment.UserName);
            Assert.That(callMeBackTicket.AnswerDate, Is.Not.Null, "Дата ответа на обратный звонок должна заполниться в базе данных");
            Assert.That(callMeBackTicket.Employee, Is.EqualTo(employee), "Пользователь обработавший заявку на обратный звонок должен заполниться в базе данных");
        }
    }
}
