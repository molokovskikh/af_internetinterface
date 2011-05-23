alter table internet.Tariffs add column FinalPriceInterval INTEGER not null default 0;
alter table internet.Tariffs add column FinalPrice NUMERIC(19,5) not null default 0;
