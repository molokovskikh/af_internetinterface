use Internet;
update regions
set _OfficeAddress = IF(Region = 'Борисоглебск',
                        'Третьяковская улица д6Б',
						IF(Region = 'Белгород','улица Князя Трубецкого д26',_OfficeAddress)),
    _OfficeGeomark = IF(Region = 'Борисоглебск',
	                    '51.3663252,42.08180200000004',
						IF(Region = 'Белгород','50.592548,36.59665819999998',_OfficeGeomark));
