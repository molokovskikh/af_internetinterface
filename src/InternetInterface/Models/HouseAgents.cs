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
    [ActiveRecord("HouseAgents", Schema = "internet", Lazy = true)]
    public class HouseAgents : ValidActiveRecordLinqBase<HouseAgents>
    {
        [PrimaryKey]
        public virtual uint Id { get; set; }

        [Property, ValidateNonEmpty("Введите имя")]
        public virtual string Name { get; set; }

        [Property, ValidateNonEmpty("Введите фамилию")]
        public virtual string Surname { get; set; }

        [Property, ValidateNonEmpty("Введите отчество")]
        public virtual string Patronymic { get; set; }

        [Property, ValidateNonEmpty("Введите дату рождения"), ValidateDateTime("Неправельно введена дата")]
        public virtual DateTime DateOfBirth { get; set; }

        [
        Property,
        ValidateRegExp(@"^((\d{1})-(\d{3})-(\d{3})-(\d{2})-(\d{2}))", "Ошибка фотмата телефонного номера: мобильный телефн (8-***-***-**-**))"),
        ValidateNonEmpty("Введите номер телефона")
        ]
        public virtual string Telephone { get; set; }

        [Property, ValidateNonEmpty("Введите адрес проживания")]
        public virtual string Adress { get; set; }

        [Property, ValidateRegExp(@"^(\d{4})?$", "Неправильный формат серии паспорта (4 цифры)"), UserValidateNonEmpty("Поле не должно быть пустым")]
        public virtual string PassportSeries { get; set; }

        [Property, ValidateRegExp(@"^(\d{6})?$", "Неправильный формат номера паспорта (6 цифр)"), UserValidateNonEmpty("Поле не должно быть пустым")]
        public virtual string PassportNumber { get; set; }

        [Property, UserValidateNonEmpty("Введите дату выдачи паспорта"), ValidateDateTime("Ошибка формата даты **-**-****")]
        public virtual DateTime PassportDate { get; set; }

        [Property, UserValidateNonEmpty("Заполните поле 'Кем выдан паспорт'")]
        public virtual string WhoGivePassport { get; set; }

        [Property, UserValidateNonEmpty("Введите адрес регистрации")]
        public virtual string RegistrationAdress { get; set; }
    }
}