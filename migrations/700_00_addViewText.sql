CREATE TABLE `internet`.`ViewTexts` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Description` VARCHAR(45) NOT NULL,
  `Text` TEXT NOT NULL,
  PRIMARY KEY (`Id`)
)
ENGINE = InnoDB;

insert into internet.viewtexts (description, text) value
("BecauseNoPassport", "Уважаемый абонент! <br/><br/>
При регистрации в сети Инфорум Вами были указаны некорректные паспортные данные. Пожалуйста, обратитесь в офис компании Инфорум для внесения изменений.</br><br/>
С уважением, Инфорум.");