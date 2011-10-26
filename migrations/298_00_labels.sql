update internet.labels l set
Deleted = true
where ShortComment is null;

delete from internet.labels
where id in (37,39,41,43);