delete from internet.writeoff
where writeoffsum = 0;

delete from internet.payments
where `sum` = 0;