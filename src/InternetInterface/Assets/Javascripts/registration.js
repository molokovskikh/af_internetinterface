function AppViewModel() {
	var self = this;
	this.tariffs = [];
	this.channels = [];

	this.internetActivated = ko.observable();
	this.tariff = ko.observable();
	this.connectSum = ko.observable();
	this.minSum = ko.computed(function () {
		var iptvCost = 0;
		for (var i = 0; i < this.channels.length; i++) {
			var channel = this.channels[i];
			if (channel.activated()) {
				if (this.internetActivated())
					iptvCost += channel.costWithInternet;
				else
					iptvCost += channel.cost;
			}
		}

		var internetCost = 0;
		if (this.internetActivated()) {
			var tariffValue = ko.utils.arrayFirst(this.tariffs, function (item) {
				return item.id == self.tariff();
			});
			if (tariffValue)
				internetCost = tariffValue.cost;
		}

		return (this.connectSum() || 0) + internetCost + iptvCost;
	}, this);

	this.getChannel = function (id) {
		return ko.utils.arrayFirst(this.channels, function (item) {
			return item.id == id;
		});
	};
}