alter table internet.PaymentForConnect add column Paid TINYINT(1) default 0 not null;

update internet.PaymentForConnect set Paid = true;