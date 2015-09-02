use internet;
ALTER TABLE `networkswitches`
	ADD CONSTRAINT `NetworkSwitchesNetworkNode` FOREIGN KEY (`networkNode`) REFERENCES `inforoom2_network_nodes` (`Id`);
ALTER TABLE `inforoom2_switchaddress`
	ADD CONSTRAINT `SwitchAddressNetworkNode` FOREIGN KEY (`NetworkNode`) REFERENCES `inforoom2_network_nodes` (`Id`) ON DELETE CASCADE;
