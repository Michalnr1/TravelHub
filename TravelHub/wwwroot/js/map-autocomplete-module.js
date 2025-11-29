//Requires map-functions.js
//Needs defined lat, lng, center, spotName
// distanceCheckUrl, tripId, warningBox
//Needs declared map

async function initMap() {
    // Request needed libraries.
    const { Place } = await google.maps.importLibrary("places");
    const { AdvancedMarkerElement } = await google.maps.importLibrary("marker");

    // Initialize the map.
    map = new google.maps.Map(document.getElementById("map"), {
        center: center,
        zoom: 12,
        mapId: 'DEMO_MAP_ID'
    });

    const placeAutocomplete = addAutocompleteWidget("start", center, 5000, "card", map);

    marker = new AdvancedMarkerElement({
        map,
    });
    infoWindow = new google.maps.InfoWindow({});
    bindMarkerToInfoWindow(map, marker, infoWindow);

    // Set initial marker position if coordinates exist
    if (spotName != '') {
        marker.position = center;
        let content =
            '<div id="infowindow-content">' +
            '<span id="place-displayname" class="title">' +
            spotName +
            '</span><br />' +
            '<span id="place-coords">' +
            lat + ' ' + lng +
            '</span>' +
            '</div>';
        updateInfoWindow(infoWindow, content, center);
    }

    placeAutocomplete.addEventListener("gmp-select", async ({ placePrediction }) => {
        const place = placePrediction.toPlace();
        await place.fetchFields({
            fields: ["displayName", "formattedAddress", "location"],
        });
        if (place.viewport) {
            map.fitBounds(place.viewport);
        } else {
            map.setCenter(place.location);
            map.setZoom(13);
        }

        let content =
            '<div id="infowindow-content">' +
            '<span id="place-displayname" class="title">' +
            place.displayName +
            '</span><br />' +
            '<span id="place-address">' +
            place.formattedAddress +
            '</span><br />' +
            '<span id="place-coords">' +
            place.location.lat() + ' ' + place.location.lng() +
            '</span>' +
            '</div>';

        updateInfoWindow(infoWindow, content, place.location);
        marker.position = place.location;

        document.getElementById("name-input").value = place.displayName;
        document.getElementById("lat-input").value = Math.round(place.location.lat() * 10000) / 10000;
        document.getElementById("lon-input").value = Math.round(place.location.lng() * 10000) / 10000;

        debounceDistanceCheck();
    });

    map.addListener("center_changed", async (event) => {
        placeAutocomplete.locationBias = { center: map.center, radius: 5000 };
    });

    map.addListener("click", async (mapsMouseEvent) => {
        if (mapsMouseEvent.placeId) {
            let place = new Place({
                id: mapsMouseEvent.placeId
            });
            mapsMouseEvent.stop();
            await place.fetchFields({ fields: ['displayName'] });
            document.getElementById("name-input").value = place.displayName;
            let content =
                '<div id="infowindow-content">' +
                '<span id="place-displayname" class="title">' +
                place.displayName +
                '</span><br />' +
                '<span id="place-coords">' +
                mapsMouseEvent.latLng.toJSON().lat + ' ' + mapsMouseEvent.latLng.toJSON().lng +
                '</span>' +
                '</div>';
            updateInfoWindow(infoWindow, content, mapsMouseEvent.latLng);
        } else {
            let content =
                '<span id="place-coords">' +
                JSON.stringify(mapsMouseEvent.latLng.toJSON().lat, null, 2) +
                ", " +
                JSON.stringify(mapsMouseEvent.latLng.toJSON().lng, null, 2) +
                '</span>' +
                '</div>';
            updateInfoWindow(infoWindow, content, mapsMouseEvent.latLng);
        }

        marker.position = mapsMouseEvent.latLng.toJSON();
        document.getElementById("lat-input").value = Math.round(mapsMouseEvent.latLng.toJSON().lat * 10000) / 10000;
        document.getElementById("lon-input").value = Math.round(mapsMouseEvent.latLng.toJSON().lng * 10000) / 10000;
        debounceDistanceCheck();
    });
}

//-------------------------------
//-------DISTANCE WARNINGS-------
//-------------------------------


const latInput = document.getElementById("lat-input");
const lngInput = document.getElementById("lon-input");

let distanceDebounceTimer = null;

function checkDistance() {
    const lat = latInput.value;
    const lng = lngInput.value;

    $.ajax({
        url: distanceCheckUrl,
        type: "get",
        data: {
            id: tripId,
            lat: lat,
            lng: lng
        },
        success: data => {
            warningBox.querySelectorAll(".distance-warning").forEach(el => el.remove());

            if (data > 500) {

                // Create a new warning div
                const div = document.createElement("div");
                div.classList.add("distance-warning");

                div.innerHTML = `This Spot is <strong> ${Math.round(data)} km </strong> away from the nearest one in this trip. Are you sure?<br>`;

                // Add to the warning box
                warningBox.appendChild(div);

                warningBox.classList.remove("d-none");

            } else {
                // Hide only if no warnings of any kind remain
                if (warningBox.children.length === 0) {
                    warningBox.classList.add("d-none");
                }
            }
        }
    });
}

function debounceDistanceCheck() {
    clearTimeout(distanceDebounceTimer);
    distanceDebounceTimer = setTimeout(checkDistance, 400);
}


$(() => {
    if (dayId != null) {
        latInput.addEventListener("input", debounceDistanceCheck);
        lngInput.addEventListener("input", debounceDistanceCheck);
    }
});

