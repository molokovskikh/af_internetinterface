use internet;
UPDATE internet.clients SET BeginWork = NULL  WHERE Status = 10 AND BeginWork IS NOT NULL;