update internet.requests r
set
ApplicantPhoneNumber = if (ASCII(ApplicantPhoneNumber) = 56 and length(ApplicantPhoneNumber) = 15,
REPLACE(SUBSTRING(ApplicantPhoneNumber, 2), "-", ""),
REPLACE(ApplicantPhoneNumber, "-", ""));