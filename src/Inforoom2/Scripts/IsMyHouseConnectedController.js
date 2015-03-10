console.log("IsMyHouseConnectedController.js");
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


function checkAddress(firstGeoObject, geoObjects) {
	console.log("geoobjects", geoObjects.get(0));
	var i = 0;
	while (geoObjects.get(i)) {
		backwardGeoObject = geoObjects.get(i++);
		console.log(i, backwardGeoObject);
		addressDetails = backwardGeoObject.properties.get('metaDataProperty.GeocoderMetaData.AddressDetails');
		var city = addressDetails.Country.AdministrativeArea.SubAdministrativeArea.Locality.LocalityName.toLowerCase();
		if (addressDetails.Country.AdministrativeArea.SubAdministrativeArea.Locality.Thoroughfare !== undefined) {
			var street = addressDetails.Country.AdministrativeArea.SubAdministrativeArea.Locality.Thoroughfare.ThoroughfareName.toLowerCase();
			if (addressDetails.Country.AdministrativeArea.SubAdministrativeArea.Locality.Thoroughfare.Premise !== undefined) {
				var house = addressDetails.Country.AdministrativeArea.SubAdministrativeArea.Locality.Thoroughfare.Premise.PremiseNumber;
			}
		}
		console.log(city, street, house);
	}
	//запрос адреса в формате яндекса
	if (!firstGeoObject) {
		console.log("Яндекс: Адрес введен некорректно");
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
		
		var address = $('.rounded option:selected').text() + " " + $("#streetId").val() + " " + $("#houseId").val();
		//Переделать этот код
		$.ajax({
			type: "POST",
			url: cli.getParam("baseurl") + "/ClientRequest/CheckForUnusualAddress",
			data: { city: yandexCity, street: yandexStreet, house: $("#houseId").val(), address: address },
			success: function (msg) {
				console.log('Получен адрес', msg);
				console.log('Получен адрес Yandex: ', yandexCity, yandexStreet, yandexHouseDetails);
				if (msg.geomark != null && msg.geomark != "") {
					var coords = msg.geomark.split(",");
					firstGeoObject = new ymaps.Placemark([coords[0], coords[1]]);
					yandexCity = msg.city;
					yandexStreet = msg.street;
					if(msg.house)
						yandexHouseDetails = msg.house;
				}
				myMap.geoObjects.removeAll();
				myMap.geoObjects.add(firstGeoObject);
				myMap.setCenter(firstGeoObject.geometry.getCoordinates(), 17, {
					checkZoomRange: true
				});

				document.getElementById('yandexCityHidden').value = yandexCity;
				document.getElementById('yandexStreetHidden').value = yandexStreet;
				document.getElementById('yandexHouseHidden').value = yandexHouseDetails;
			}
		});
	});
}

function findAddressOnMap(searchQuery) {
	console.log("Запрос адреса: ", searchQuery);
	window.ymaps.geocode(searchQuery, {
		results: 1
	}).then(function(res) {
		var firstGeoObject = res.geoObjects.get(0);
		checkAddress(firstGeoObject, res.geoObjects);
	});
}

var typeWatcher = function() {
	var timer = 0;
	return function(ms) {
		var callback = function showAddressOnMap() {
			var address = $('.rounded option:selected').text() + " " + $("#streetId").val() + " " + $("#houseId").val();
			findAddressOnMap(address);
			
		}
		clearTimeout(timer);
		timer = setTimeout(callback, ms);
	};
}();
