
use internet;
ALTER TABLE `inforoom2_network_nodes`
	CHANGE COLUMN `Virtual` `IsVirtual` TINYINT(4) NOT NULL AFTER `Name`;