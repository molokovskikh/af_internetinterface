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
	myMap.disableRuler();
}


function checkAddress(firstGeoObject) {
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

		console.log('Получен адрес: ', yandexCity, yandexStreet, yandexHouseDetails);
		myMap.geoObjects.add(firstGeoObject);
		myMap.setCenter(firstGeoObject.geometry.getCoordinates(), 17, {
			checkZoomRange: true
		});
			document.getElementById('yandexCityHidden').value = yandexCity;
			document.getElementById('yandexStreetHidden').value = yandexStreet;
			document.getElementById('yandexHouseHidden').value = yandexHouseDetails;
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
	return function(ms) {
		var callback = function showAddressOnMap() {
			var address = $('.rounded option:selected').text() +" "+ document.getElementById('addressArea').value.toLowerCase();
			findAddressOnMap(address);
			
		}
		clearTimeout(timer);
		timer = setTimeout(callback, ms);
	};
}();
