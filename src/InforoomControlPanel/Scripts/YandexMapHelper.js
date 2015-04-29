function YandexMapHelper() {
	var cityInput = null;
	var streetInput = null;
	var houseInput = null;
	var additionalInput = null;
	var cityOutput = null;
	var streetOutput = null;
	var houseOutput = null;
	var delay = 1000;
	var timeout = 0;
	var self = this;
	var errorElement = null;
	var currentPosition = null;
	var currentPositionOutput = null;
	var currentMark = null;
	var markOutput = null;
	var map = null;
	var mapElement = null;
	var conditions = [];

	this.init = function (elementId) {
		mapElement = $("#" + elementId).get(0);
		map = new ymaps.Map(elementId, {
			center: [55.76, 37.64],
			zoom: 15,
			controls: ['zoomControl']
		});
		
		if (currentMark) {
			console.log("Добавляем марку", currentMark);
			map.balloon.open(currentMark, { contentBody: "Метка" });
		}
	}

	this.addCondition = function(func) {
		conditions.push(func);
	}
	this.checkConditions = function() {
		for(var i =conditions.length-1; i >=0; i--)
			if (!conditions[i]())
				return false;
		return true;
	}
	this.enableMarks = function () {
		console.log("Включены метки на карте",map);
		map.events.add("click", function (e) {
			map.balloon.open(e.get("coords"), { contentBody: "Метка" });
			currentMark = e.get("coords");
			if (markOutput)
				$(markOutput).val(currentMark[0]+","+currentMark[1]);
		});
	}
	this.setPositionOutput = function(element) {
		currentPositionOutput = element;
	}
	this.getMap = function() {
		return map;
	}

	this.getCurrentMark = function() {
		return currentMark;
	}

	this.setCurrentMark = function (x, y) {
		currentMark = [x, y];
		console.log("Добавляем марку", currentMark);
		if(map)
			map.balloon.open(currentMark, { contentBody: "Метка" });
	}
	this.setMarkOutput = function (element) {
		markOutput = element;
	}
	this.setErrorElement = function(element) {
		errorElement = element;
	}
	this.setAdditionalInput = function (element) {
		additionalInput = element;
	}
	this.setStreetInput = function(element) {
		streetInput = element;
	}

	this.setHouseInput = function (element) {
		houseInput = element;
	}
	this.setCityInput = function (element) {
		cityInput = element;
	}
	this.setCityOutput = function (element) {
		cityOutput = element;
	}

	this.setStreetOutput = function (element) {
		streetOutput = element;
	}

	this.setHouseOutput = function (element) {
		houseOutput = element;
	}

	this.run = function (d) {
		if (!self.checkConditions())
			return;
		if (errorElement)
			$(errorElement).html("");
		clearTimeout(timeout);
		if (d == null)
			d = delay;
		timeout = setTimeout(self.findAddress.bind(self, self.getAddress(), self.checkAddress), d);
	}

	this.getAddress = function () {
		var str = "";
		if (cityInput)
			str += "г. " + this.getInputVal(cityInput);
		if (streetInput)
			str += " "+ this.getInputVal(streetInput);
		if (houseInput)
			str += " д. " + this.getInputVal(houseInput);
		if (additionalInput)
			str += this.getInputVal(additionalInput);

		return str;
	}

	this.getInputVal = function(input) {
		var options = $(input).find("option");
		if (options.length > 0)
			return $(input).find("option:selected").html();

		return $(input).val();
	}

	this.findAddress = function(address,callback) {
		console.log("Запрос адреса: ", address);
		window.ymaps.geocode(address, {
			results: 1
		}).then(function (res) {
			var firstGeoObject = res.geoObjects.get(0);
			map.geoObjects.removeAll();
			callback(firstGeoObject);
		});
	}

	this.checkAddress = function(firstGeoObject) {
		if (!firstGeoObject && errorElement) {
			$(errorElement).html("Яндекс не смог найти адрес");
			return;
		}
		var yandexCity;
		var yandexStreet;
		var yandexHouseDetails;
		ymaps.geocode(firstGeoObject.geometry.getCoordinates(), {

		}).then(function (res) {
			var backwardGeoObject = res.geoObjects.get(0);
			var addressDetails = backwardGeoObject.properties.get('metaDataProperty.GeocoderMetaData.AddressDetails');
			yandexCity = addressDetails.Country.AdministrativeArea.SubAdministrativeArea.Locality.LocalityName.toLowerCase();
			if (addressDetails.Country.AdministrativeArea.SubAdministrativeArea.Locality.Thoroughfare !== undefined) {
				yandexStreet = addressDetails.Country.AdministrativeArea.SubAdministrativeArea.Locality.Thoroughfare.ThoroughfareName.toLowerCase();
				if (addressDetails.Country.AdministrativeArea.SubAdministrativeArea.Locality.Thoroughfare.Premise !== undefined) {
					yandexHouseDetails = addressDetails.Country.AdministrativeArea.SubAdministrativeArea.Locality.Thoroughfare.Premise.PremiseNumber;
				}
			}

			console.log('Получен адрес: ', yandexCity, yandexStreet, yandexHouseDetails);
			map.geoObjects.add(firstGeoObject);
			map.setCenter(firstGeoObject.geometry.getCoordinates(), 17, {
				checkZoomRange: true
			});
			currentPosition = firstGeoObject.geometry.getCoordinates();
			if(cityOutput)
				$(cityOutput).val(yandexStreet);
			if (streetOutput)
				$(streetOutput).val(yandexStreet);
			if (houseOutput)
				$(houseOutput).val(yandexHouseDetails);
			if (currentPositionOutput)
				$(currentPositionOutput).val(currentPosition[0]+","+currentPosition[1]);
		});
	}

}
