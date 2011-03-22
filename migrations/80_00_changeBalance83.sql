update internet.Clients C set
C.ShowBalanceWarningPage = false
where PhisicalClient = 83;

update internet.PhysicalClients P set
P.Balance = P.Balance + 590
where P.Id = 83;