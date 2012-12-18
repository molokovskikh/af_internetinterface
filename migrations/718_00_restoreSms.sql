DROP TEMPORARY TABLE IF EXISTS `internet`.`RESTOREDsms`;
CREATE TABLE  `internet`.`RESTOREDsms` (
  `CreateDate` datetime DEFAULT NULL,
  `SendToOperatorDate` datetime DEFAULT NULL,
  `ShouldBeSend` datetime DEFAULT NULL,
  `Text` varchar(255) DEFAULT NULL,
  `PhoneNumber` varchar(255) DEFAULT NULL,
  `IsSended` tinyint(1) DEFAULT NULL,
  `SMSID` varchar(255) DEFAULT NULL,
  `Client` int(10) unsigned DEFAULT NULL,
  `ServerRequest` int(11) DEFAULT NULL
)  engine=MEMORY ;

insert into internet.RESTOREDsms (Createdate, SendToOperatorDate, ShouldBeSend, `Text`, PhoneNumber, IsSended, smsid, `client`, ServerRequest)
SELECT Createdate, SendToOperatorDate, now() as ShouldBeSend, `Text`, PhoneNumber, IsSended, smsid, `client`, ServerRequest FROM `logs`.SmsMessageLogs S
where operation = 2
and CreateDate >= '2012-12-18';

insert into internet.SmsMessages (Createdate, SendToOperatorDate, ShouldBeSend, `Text`, PhoneNumber, IsSended, smsid, `client`, ServerRequest)
select * from  internet.RESTOREDsms;