using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using InternetInterface.Helpers;
using InternetInterface.Models.Universal;

namespace InternetInterface.Models
{
    /*[ActiveRecord("HouseAgents", Schema = "internet", Lazy = true)]
    public class HouseAgent : ValidActiveRecordLinqBase<HouseAgent>
    {
        [PrimaryKey]
        public virtual uint Id { get; set; }

        [Property, ValidateNonEmpty("Введите имя")]
        public virtual string Name { get; set; }

        [Property, ValidateNonEmpty("Введите фамилию")]
        public virtual string Surname { get; set; }

        [Property, ValidateNonEmpty("Введите отчество")]
        public virtual string Patronymic { get; set; }

        [Property, ValidateNonEmpty("Введите дату рождения"), ValidateDate("Неправельно введена дата")]
        public virtual DateTime DateOfBirth { get; set; }

        [
        Property,
        ValidateRegExp(@"^(([0-9]{1})-([0-9]{3})-([0-9]{3})-([0-9]{2})-([0-9]{2}))", "Ошибка фотмата телефонного номера: мобильный телефн (8-***-***-**-**))"),
        ValidateNonEmpty("Введите номер телефона")
        ]
        public virtual string Telephone { get; set; }

        [Property, ValidateNonEmpty("Введите адрес проживания")]
        public virtual string Adress { get; set; }

        [Property, ValidateRegExp(@"^([0-9]{4})$", "Неправильный формат серии паспорта (4 цифры)"), UserValidateNonEmpty("Поле не должно быть пустым")]
        public virtual string PassportSeries { get; set; }

        [Property, ValidateRegExp(@"^([0-9]{6})$", "Неправильный формат номера паспорта (6 цифр)"), UserValidateNonEmpty("Поле не должно быть пустым")]
        public virtual string PassportNumber { get; set; }

        [Property, UserValidateNonEmpty("Введите дату выдачи паспорта"), ValidateDate("Ошибка формата даты **-**-****")]
        public virtual DateTime PassportDate { get; set; }

        [Property, UserValidateNonEmpty("Заполните поле 'Кем выдан паспорт'")]
        public virtual string WhoGivePassport { get; set; }

        [Property, UserValidateNonEmpty("Введите адрес регистрации")]
        public virtual string RegistrationAdress { get; set; }

        public virtual string GetFIO()
        {
            return string.Format("{0} {1} {2}", Surname, Name, Patronymic);
        }
    }*/
}