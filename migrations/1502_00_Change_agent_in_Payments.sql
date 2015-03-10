update internet.payments P
set P.Agent = 243
where P.Agent is NULL and ((DATE(P.PaidOn) = '2014-10-15' and P.`Client` = 17221) or
                           (DATE(P.PaidOn) = '2014-10-15' and P.`Client` = 17223) or
                           (DATE(P.PaidOn) = '2014-10-16' and P.`Client` = 17241) or
                           (DATE(P.PaidOn) = '2014-10-17' and P.`Client` = 15431) or
                           (DATE(P.PaidOn) = '2014-10-20' and P.`Client` = 17299) or
                           (DATE(P.PaidOn) = '2014-10-22' and P.`Client` = 17329) or
                           (DATE(P.PaidOn) = '2014-10-23' and P.`Client` = 17341) or
                           (DATE(P.PaidOn) = '2014-10-23' and P.`Client` = 17365) or
                           (DATE(P.PaidOn) = '2014-10-25' and P.`Client` = 17393) or
                           (DATE(P.PaidOn) = '2014-10-25' and P.`Client` = 17395) or
                           (DATE(P.PaidOn) = '2014-10-27' and P.`Client` = 17389) or
                           (DATE(P.PaidOn) = '2014-10-28' and P.`Client` = 17415) or
                           (DATE(P.PaidOn) = '2014-10-29' and P.`Client` = 17451));
