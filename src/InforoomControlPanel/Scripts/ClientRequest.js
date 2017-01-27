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
	console.log('tick'); 
	return function(ms) {
		var callback = function showAddressOnMap() {
			var userCity,userStreet,userHouse ="";
			var skillsSelect = document.getElementById("RegionDropDown"); 
			if (skillsSelect.options[skillsSelect.selectedIndex]
				&& skillsSelect.options[skillsSelect.selectedIndex].attributes
				&& skillsSelect.options[skillsSelect.selectedIndex].attributes.yandexName
				&& skillsSelect.options[skillsSelect.selectedIndex].attributes.yandexName.value){				
			var selectedText = skillsSelect.options[skillsSelect.selectedIndex].text;
			userCity = selectedText.toLowerCase();	
			}
			skillsSelect = document.getElementById("StreetDropDown");
			if (skillsSelect.options[skillsSelect.selectedIndex]
				&& skillsSelect.options[skillsSelect.selectedIndex].attributes
				&& skillsSelect.options[skillsSelect.selectedIndex].attributes.yandexName
				&& skillsSelect.options[skillsSelect.selectedIndex].attributes.yandexName.value) {
			selectedText = skillsSelect.options[skillsSelect.selectedIndex].attributes.yandexName.value;
			userStreet = selectedText.toLowerCase();	
			}
			skillsSelect = document.getElementById("HouseDropDown");
			if(skillsSelect.options[skillsSelect.selectedIndex]){				
			selectedText = skillsSelect.options[skillsSelect.selectedIndex].text;
			userHouse = selectedText.toLowerCase(); 
			}
			var address = userCity + " " + userStreet + " " + userHouse;	 
			if(address!=null && userCity+userStreet+userHouse+userHousing!=""){ 
				findAddressOnMap(address);
			}
			
			
		}
		clearTimeout(timer);
		timer = setTimeout(callback, ms);
	};
}();
