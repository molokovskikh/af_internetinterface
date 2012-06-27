update Internet.Tariffs
set CanUseForSelfConfigure = 1
where Name in ('Лёгкий', 'Оптимальный', 'Профи')
