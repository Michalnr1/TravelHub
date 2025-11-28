function clampToAllowedRange(dt) {
    const now = luxon.DateTime.now();
    const minDate = now.minus({ days: 7 });
    const maxDate = now.plus({ days: 100 });

    if (dt >= minDate && dt <= maxDate) {
        return dt;
    }
    const targetWeekday = dt.weekday;
    let adjusted = now;
    while (adjusted.weekday !== targetWeekday) {
        adjusted = adjusted.plus({ days: 1 });
    }
    adjusted = adjusted.set({ hour: dt.hour, minute: dt.minute, second: 0 });
    return adjusted;
}
function getMapBounds(points) {

    const bounds = new google.maps.LatLngBounds();

    for (let i = 0; i < points.length; i++) {
        let coords = { lat: parseFloat(points[i].latitude), lng: parseFloat(points[i].longitude) };

        bounds.extend(coords);
    }

    return bounds;
}

async function getRoute(origin, destination, intermediates = [], travelMode = 'WALKING', departure_time = '', fields = ['durationMillis', 'distanceMeters', 'path', 'legs']) {
    const { Route } = await google.maps.importLibrary("routes");
    const request = {
        origin: { lat: parseFloat(origin.latitude), lng: parseFloat(origin.longitude) },
        intermediates: intermediates,
        destination: { lat: parseFloat(destination.latitude), lng: parseFloat(destination.longitude) },
        fields: fields,
        travelMode: travelMode,
    };
    if (travelMode == 'TRANSIT' && departure_time != '') {
        request.departureTime = departure_time.toJSDate();
    }
    const route = await Route.computeRoutes(request);
    return route;
}

function durationString(duration) {
    if (duration.hours > 0) {
        return duration.toFormat("hh 'h' mm 'm'")
    } else {
        return duration.toFormat("mm 'm'")
    }

}

function durationStringMillis(durationMillis) {
    durationMinutes = Math.round(durationMillis / 60 / 1000);
    if (durationMinutes < 60) {
        return durationMinutes + " min";
    } else {
        return Math.round(durationMinutes / 60) + " h " + (durationMinutes % 60) + " min";
    }
}

function decimalToDateTime(refTime, decimal) {
    return refTime.set({ hours: Math.floor(decimal), minutes: Math.ceil(60 * (decimal % 1)) });
}

function fillFixedDurations(points) {
    points.forEach((point, i) => {
        if (i < points.length - 1) {
            point.fixedDurationFrom = null;
            for (let j = 0; j < point.transportsFrom.length; j++) {
                if (point.transportsFrom[j].toSpotId == points[i + 1].id) {
                    point.fixedDurationFrom = parseFloat(point.transportsFrom[j].duration);
                    break;
                }
            }
        }
    });
}