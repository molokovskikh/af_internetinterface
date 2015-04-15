$(document).ready(function() {ymaps.ready(init);});

function init() {
		var officeRegion = cli.getParam("office_region");
		var officeAddress = cli.getParam("office_address");
		var officeGeoX = cli.getParam("office_geoX");
		var officeGeoY = cli.getParam("office_geoY");

		var myMap = new ymaps.Map("yandexMap", {
				center: [officeGeoX, officeGeoY],
				zoom: 16,
				controls: ['zoomControl']
		});

		var myPlacemark = new ymaps.Placemark([officeGeoX, officeGeoY], {
				hintContent: officeRegion,
				balloonContent: officeAddress
		});

		myMap.geoObjects.add(myPlacemark);
}
