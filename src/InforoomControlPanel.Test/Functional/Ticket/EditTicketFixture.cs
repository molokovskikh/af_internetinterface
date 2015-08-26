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
    class EditTicketFixture : TicketFixture
    {
        [Test, Description("Изменение запроса в техподдержку")]
        public void EditTickets()
        {
            //создаем запрос в техподдержку
            var employee = DbSession.Query<Employee>().First(p => p.Name == "test");
            var ticketNew = new Inforoom2.Models.Ticket();
            ticketNew.Employee = employee;
            ticketNew.Email = "apopov@mail.ru";
            ticketNew.Text = "Не работает интернет";
            DbSession.Save(ticketNew);

            Open("Ticket/TicketIndex");
            var ticket = DbSession.Query<Inforoom2.Models.Ticket>().First(p => p.Email == "apopov@mail.ru");
            var targetTicket = browser.FindElementByXPath("//td[contains(.,'" + ticket.Email + "')]");
            var row = targetTicket.FindElement(By.XPath("../.."));
            var button = row.FindElement(By.CssSelector("a.btn-success"));
            button.Click();
            browser.FindElementByCssSelector("textarea[id=ticket_Answer]").SendKeys("Проводятся технические работы");
            browser.FindElementByCssSelector(".btn-green").Click();
            AssertText("Ответ отправлен пользователю");
            DbSession.Refresh(ticket);
            Assert.That(ticket.IsNotified, Is.True, "Поле оповещение пользователя у запроса в техподдержку должно измениться");
            Assert.That(ticket.AnswerDate, Is.Not.Null, "Дата ответа на запрос должна заполниться в базе данных");
        }
    }
}
