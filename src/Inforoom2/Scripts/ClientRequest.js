console.log("ClientRequest.js");
ymaps.ready(init);
var myMap;
var userCity;
var userStreet;
var userHouse;
var userHousing;
var yandexCity;
var yandexStreet;
var yandexHouseDetails;



function init() { // Создаем карту.

	myMap = new ymaps.Map("yandexMap", {
		center: [55.76, 37.64],
		zoom: 15,
		controls: ['zoomControl']
	});

	typeWatcher(0);
}


function checkAddress(firstGeoObject) {
	//запрос адреса в формате яндекса
	if (!firstGeoObject) {
		$('.YMapAddressResult').html("Яндекс: Адрес введен некорректно");
		return;
	}
	ymaps.geocode(firstGeoObject.geometry.getCoordinates(), {

	}).then(function(res) {
		var backwardGeoObject = res.geoObjects.get(0);
		var addressDetails = backwardGeoObject.properties.get('metaDataProperty.GeocoderMetaData.AddressDetails');
		 yandexCity = addressDetails.Country.AdministrativeArea.SubAdministrativeArea.Locality.LocalityName.toLowerCase();
		if (addressDetails.Country.AdministrativeArea.SubAdministrativeArea.Locality.Thoroughfare !== undefined) {
			 yandexStreet = addressDetails.Country.AdministrativeArea.SubAdministrativeArea.Locality.Thoroughfare.ThoroughfareName.toLowerCase();
			if (addressDetails.Country.AdministrativeArea.SubAdministrativeArea.Locality.Thoroughfare.Premise !== undefined) {
				 yandexHouseDetails = addressDetails.Country.AdministrativeArea.SubAdministrativeArea.Locality.Thoroughfare.Premise.PremiseNumber;
			}
		}

		//Переделать этот код
		$.ajax({
			type: "POST",
			url: cli.getParam("baseurl") + "/ClientRequest/CheckForUnusualAddress",
			data: { city: yandexCity, street: yandexStreet, house: yandexHouseDetails, address: userCity + " " + userStreet + " " + userHouse + userHousing },
			success: function (msg) {
				console.log('Получен адрес',msg);
				console.log('Получен адрес Yandex: ', yandexCity, yandexStreet, yandexHouseDetails);
				if (msg.geomark != null && msg.geomark != "") {
					var coords = msg.geomark.split(",");
					firstGeoObject = new ymaps.Placemark([coords[0], coords[1]]);
					yandexCity = msg.city;
					yandexCity = msg.street;
					if (msg.house)
						yandexHouseDetails = msg.house;
				}

				myMap.geoObjects.add(firstGeoObject);
				myMap.setCenter(firstGeoObject.geometry.getCoordinates(), 17, {
					checkZoomRange: true
				});
				
				document.getElementById('yandexCityHidden').value = yandexCity;
				document.getElementById('yandexStreetHidden').value = yandexStreet;
				document.getElementById('yandexHouseHidden').value = yandexHouseDetails;
			}});
		
	});
}

function findAddressOnMap(searchQuery) {
	console.log("Запрос адреса: ", searchQuery);
	window.ymaps.geocode(searchQuery, {
		results: 1
	}).then(function(res) {
		var firstGeoObject = res.geoObjects.get(0);
		myMap.geoObjects.removeAll();
		checkAddress(firstGeoObject);
	});
}

var typeWatcher = function() {
	var timer = 0;
	console.log('tick');
	return function(ms) {
		var callback = function showAddressOnMap() {
			//Город получаем из списка
			//userCity = document.getElementById('clientRequest_City').value.toLowerCase();
			var gatCityObject = document.getElementById("clientRequest_City");
			var selectedCityText = gatCityObject.options[gatCityObject.selectedIndex].text;
			userCity = selectedCityText.toLowerCase();

			userStreet = document.getElementById('clientRequest_Street').value.toLowerCase();
			userHouse = document.getElementById('clientRequest_HouseNumber').value.toLowerCase();
			userHousing = document.getElementById('clientRequest_Housing').value.toLowerCase();

			var address = userCity + " " + userStreet + " " + userHouse + " " + userHousing;
			findAddressOnMap(address);
			
		}
		clearTimeout(timer);
		timer = setTimeout(callback, ms);
	};

}();

var updatePlanList = function() {
	var gatCityObject = document.getElementById("clientRequest_City");
	var selectedCityText = gatCityObject.options[gatCityObject.selectedIndex].text;
	userCity = selectedCityText.toLowerCase();
	// закрываем сообщение  
	$("#notification .hide").click();
	if (gatCityObject.options[gatCityObject.selectedIndex].getAttribute("title") != null) {
		//выводим новое
	cli.showError("<span id style='font-weight: 400;font-size: 20px;'>Для данного региона существует частный сектор, тарифы которого отличаются!</span>");
	}
	var gatPlanObject = document.getElementById("clientRequest_Plan");
	var firstShown = true;
	for (var i = 0; i < gatPlanObject.options.length; i++) {

		if (gatPlanObject.options[i].getAttribute("title") != null &&
			gatPlanObject.options[i].getAttribute("title").length > 0) {
			var splitedTitle = gatPlanObject.options[i].getAttribute("title").split(",");
			var showTag = splitedTitle.length > 0 ? false : true;
			for (var j = 0; j < splitedTitle.length; j++) {
				if (splitedTitle[j].toLowerCase() == userCity) {
					showTag = true;
				}
			}
			if (showTag) {
				if (firstShown) {
					gatPlanObject.options[i].setAttribute("selected", "selected");
					$("#clientRequest_Plan").val(gatPlanObject.options[i].value);
					firstShown = false;
				}
				gatPlanObject.options[i].removeAttribute("style");
			} else {
				gatPlanObject.options[i].setAttribute("style", "display:none;");
			}

		}
	} 
};
updatePlanList();