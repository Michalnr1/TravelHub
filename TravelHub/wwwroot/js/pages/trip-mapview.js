async function initMap() {
    // Request needed libraries.
    const { Place } = await google.maps.importLibrary("places");
    const { AdvancedMarkerElement } = await google.maps.importLibrary("marker");

    // Initialize the map.
    map = new google.maps.Map(document.getElementById("map"), {
        center: center,
        mapId: 'DEMO_MAP_ID'
    });
    marker = new AdvancedMarkerElement({
        map,
    });
    infoWindow = new google.maps.InfoWindow({});

    calibrateMap(map, points, center);

    points.forEach((point, i) => {
        let coords = { lat: parseFloat(point.latitude), lng: parseFloat(point.longitude) }

        const marker = new AdvancedMarkerElement({
            position: coords,
            map: map,
            title: point.name,
        });

        bindActivityListElems(map, "spot-" + i, marker, infoWindow);
    });
}