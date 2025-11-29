function calibrateMap(map, points, center) {
    if (points.length <= 1) {
        map.setCenter(center);
        map.setZoom(14);
    } else {
        map.fitBounds(getMapBounds(points), 20);
    }
}

function getMapBounds(points) {
    const bounds = new google.maps.LatLngBounds();
    for (let i = 0; i < points.length; i++) {
        let coords = { lat: parseFloat(points[i].latitude), lng: parseFloat(points[i].longitude) };
        bounds.extend(coords);
    }
    return bounds;
}

function bindActivityListElems(map, activityCardId, marker, infoWindow) {
    marker.addListener("click", () => {
        if (infoWindow.isOpen && infoWindow.anchor == marker) {
            infoWindow.close();
        } else if (infoWindow.anchor != marker) {
            infoWindow.close();
            infoWindow.setContent(marker.title);
            infoWindow.open(map, marker);
        } else {
            infoWindow.setContent(marker.title);
            infoWindow.open(map, marker);
        }
    });

    let listElem = document.getElementById(activityCardId);
    listElem.addEventListener("mouseenter", (event) => {
        infoWindow.close();
        infoWindow.setContent(marker.title);
        infoWindow.open(map, marker);
    });

    listElem.addEventListener("mouseleave", (event) => {
        infoWindow.close();
    });

    marker.addEventListener('mouseenter', (event) => {
        listElem.classList.add("selected-item");
    });

    marker.addEventListener('mouseleave', (event) => {
        listElem.classList.remove("selected-item");
    });
}

function updateInfoWindow(infoWindow, content, center) {
    infoWindow.setContent(content);
    infoWindow.setPosition(center);
    infoWindow.open({
        map,
        anchor: marker,
        shouldFocus: false,
    });
}

function addAutocompleteWidget(widgetId, center, radius, cardId, map) {
    const placeAutocomplete =
        new google.maps.places.PlaceAutocompleteElement();
    placeAutocomplete.id = widgetId;
    placeAutocomplete.locationBias = { center: center, radius: radius };
    const card = document.getElementById(cardId);
    card.appendChild(placeAutocomplete);
    map.controls[google.maps.ControlPosition.TOP_LEFT].push(card);
    return placeAutocomplete;
}

function bindMarkerToInfoWindow(map, marker, infoWindow) {
    marker.addListener("click", () => {
        if (infoWindow.isOpen) {
            infoWindow.close();
        } else {
            infoWindow.open(map, marker);
        }
    });
}