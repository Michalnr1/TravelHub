//Requires route-functions.js, map-functions.js

//-------------------------------
//------------SET UP-------------
//-------------------------------

let map;
let involvesCheckout = previousAccommodation != null && (nextAccommodation == null || previousAccommodation.id != nextAccommodation.id);
let polylines = [];
let routeInfoWindow;
let draggedItem = null;
let isUpdating = false;

// Variables for PDF export
let directionsService;
let directionsRenderer;
let markers = [];
let routePath = null;
//let directionsResult = null;
//let totalRouteDistance = 0;
//let totalRouteDuration = 0;

async function initMap() {
    console.log('initMap() called');

    try {
        // Sprawdź czy Google Maps API jest załadowane
        if (typeof google === 'undefined' || typeof google.maps === 'undefined') {
            console.error('Google Maps API not loaded');
            throw new Error('Google Maps API not loaded');
        }

        console.log('Google Maps API version:', google.maps.version);

        // Request needed libraries.
        const { AdvancedMarkerElement } = await google.maps.importLibrary("marker");
        console.log('AdvancedMarkerElement loaded');

        // Sprawdź czy kontener mapy istnieje
        const mapElement = document.getElementById("map");
        if (!mapElement) {
            console.error('Map element not found');
            throw new Error('Map element not found');
        }

        // Initialize the map.
        map = new google.maps.Map(mapElement, {
            center: center,
            zoom: 12,
            mapTypeControl: true,
            streetViewControl: false,
            fullscreenControl: true,
            mapId: 'DEMO_MAP_ID'
        });
        console.log('Map initialized');

        // Oznacz mapę jako załadowaną
        mapElement.classList.add('loaded');

        // Initialize directions service and renderer
        directionsService = new google.maps.DirectionsService();
        directionsRenderer = new google.maps.DirectionsRenderer({
            map: map,
            suppressMarkers: true,
            preserveViewport: false,
            polylineOptions: {
                strokeColor: "#667eea",
                strokeOpacity: 0.8,
                strokeWeight: 5
            }
        });
        console.log('Directions service initialized');

        marker = new AdvancedMarkerElement({
            map,
        });
        infoWindow = new google.maps.InfoWindow({});
        routeInfoWindow = new google.maps.InfoWindow({});

        calibrateMap(map, points, center);
        console.log('Map calibrated');

        // Initialize markers
        markers = []; // Reset markers array
        points.forEach((point, i) => {
            let coords = { lat: parseFloat(point.latitude), lng: parseFloat(point.longitude) }

            let iconImage;
            if (isGroup) {
                iconImage = new google.maps.marker.PinElement({
                    background: '#dc3545',
                    borderColor: '#b02a37',
                    glyphColor: 'white'
                });
            } else {
                iconImage = new google.maps.marker.PinElement({
                    glyphText: (i + 1).toString(),
                    glyphColor: 'white',
                    background: '#dc3545',
                    borderColor: '#b02a37',
                });
            }

            const marker = new AdvancedMarkerElement({
                position: coords,
                map: map,
                title: point.name,
                content: iconImage.element,
            });

            let cardId = `spot-${point.id}`;

            if (i == 0 && previousAccommodation?.id == point.id) {
                cardId = cardId + "-prev";
                // Dla poprzedniego zakwaterowania użyj żółtego koloru
                const accommodationIcon = new google.maps.marker.PinElement({
                    background: '#ffc107', // Żółty
                    borderColor: '#d39e00',
                    glyphColor: '#212529'
                });
                marker.content = accommodationIcon.element;
            } else if (i == points.length - 1 && nextAccommodation?.id == point.id) {
                cardId = cardId + "-next";
                // Dla następnego zakwaterowania użyj pomarańczowego koloru
                const accommodationIcon = new google.maps.marker.PinElement({
                    background: '#fd7e14', // Pomarańczowy
                    borderColor: '#d56712',
                    glyphColor: 'white'
                });
                marker.content = accommodationIcon.element;
            }

            bindActivityListElems(map, cardId, marker, infoWindow);

            // Store marker for PDF export
            markers.push(marker);

            updateRouteInfoForPdf(getCurrentTravelMode(), false);
        });
        console.log('Markers initialized:', markers.length);

        // W initMap() - zmień obsługę przycisku:
        let routeButton = document.getElementById("route-walk-btn");
        if (routeButton) {
            routeButton.addEventListener('click', async (event) => {
                console.log('=== Find Route button clicked ===');

                // Pobierz aktualny tryb podróży z globalnych ustawień
                const travelMode = getCurrentTravelMode();
                console.log('Using travel mode:', travelMode);

                // Zmień tekst przycisku podczas ładowania
                const originalText = routeButton.innerHTML;
                routeButton.innerHTML = `<i class="fas fa-spinner fa-spin me-2"></i>Calculating ${getTravelModeText(travelMode)} Route...`;
                routeButton.disabled = true;

                try {
                    // 1. Wyczyść poprzednie trasy
                    clearPolylines();
                    clearTravelCards();
                    clearWarnings();

                    // 2. Wyznacz trasę dla widoku szczegółowego (stara logika)
                    let startTime = getStartDatetime();
                    let travelModes = getTravelModes();
                    let segments = getModalSegments(points, travelModes);
                    let legs = [];

                    for await (const s of segments) {
                        let route = await fetchBaseRoute(s);
                        if (!route || route.routes.length === 0) {
                            throw new Error('Unable to compute route for segment');
                        }
                        let segmentLegs = route.routes[0].legs;
                        segmentLegs.forEach((l, i) => {
                            l.desiredTravelMode = s.travelModes[i];
                        });
                        legs.push(...segmentLegs);
                    }

                    legs = await processLegs(legs, startTime);
                    await renderRoute(legs);

                    let endTime = activities[activities.length - 1].endTime;
                    showRouteSummary(startTime, endTime);
                    validatePlan(activities, startTime, endTime);

                    // 3. Wyznacz trasę dla PDF z aktualnym trybem
                    console.log('Calculating PDF route with mode:', travelMode);
                    await calculateRouteForPdf(travelMode);

                    console.log('Route calculation completed successfully');

                } catch (error) {
                    console.error('Error calculating route:', error);

                    // Pokaż szczegółowy błąd
                    let errorMessage = 'Error calculating route';
                    if (error.message.includes('ZERO_RESULTS')) {
                        errorMessage = 'No route found between these points. Try different travel mode.';
                    } else if (error.message.includes('OVER_QUERY_LIMIT')) {
                        errorMessage = 'API limit exceeded. Please try again later.';
                    } else {
                        errorMessage = error.message;
                    }

                    alert(errorMessage);

                } finally {
                    // Przywróć przycisk
                    routeButton.innerHTML = originalText;
                    routeButton.disabled = false;
                }
            });
        } else {
            console.warn('Route button not found');
        }

        bindGlobalTravelModeSelector();
        hideLastTravelCard();

        // Initialize buttons for PDF export
        setupPdfExportButton();

        console.log('initMap() completed successfully');

    } catch (error) {
        console.error('Error in initMap():', error);
        // Pokaż błąd użytkownikowi
        const mapElement = document.getElementById("map");
        if (mapElement) {
            mapElement.innerHTML = `
                <div class="alert alert-danger" style="height: 100%; display: flex; align-items: center; justify-content: center;">
                    <div>
                        <i class="fas fa-exclamation-triangle fa-2x mb-3"></i>
                        <h4>Error loading map</h4>
                        <p>${error.message}</p>
                        <button class="btn btn-primary mt-2" onclick="window.location.reload()">Reload Page</button>
                    </div>
                </div>
            `;
        }
    }
}

// Funkcja pomocnicza do tekstu trybu
function getTravelModeText(mode) {
    switch (mode.toUpperCase()) {
        case 'DRIVING': return 'Driving';
        case 'TRANSIT': return 'Transit';
        default: return 'Walking';
    }
}

function bindGlobalTravelModeSelector() {
    document.querySelectorAll('input[name="global-travel-mode"]').forEach(radio => {
        radio.addEventListener('change', function () {
            const selectedMode = this.value;

            document
                .querySelectorAll('.travel-mode-radio-button[value="' + selectedMode + '"]')
                .forEach(r => {
                    r.checked = true;
                    r.dispatchEvent(new Event('change'));
                });
        });
    });
}

function setupPdfExportButton() {
    const exportBtn = document.getElementById('exportPdfBtn');
    if (!exportBtn) {
        console.warn('Export PDF button not found');
        return;
    }

    // Usuń istniejące event listenery
    const newExportBtn = exportBtn.cloneNode(true);
    exportBtn.parentNode.replaceChild(newExportBtn, exportBtn);

    // Dodaj nowy event listener
    newExportBtn.addEventListener('click', function (e) {
        e.preventDefault();

        const routeData = window.collectRouteDataForExport();
        console.log('Export clicked, route data:', routeData);

        // Użyj bezpośredniego URL zamiast @Url.Action
        let url = `/Days/ExportDayPdf/${dayId}`;

        if (routeData) {
            url += `?routeData=${encodeURIComponent(routeData)}`;
            console.log('Opening URL:', url);
            window.open(url, '_blank');
        } else {
            // Jeśli nie ma danych trasy, eksportuj tylko mapę z punktami
            if (confirm('No route calculated. Export map with points only?')) {
                console.log('Opening URL (no route):', url);
                window.open(url, '_blank');
            }
        }
    });

    console.log('PDF export button setup complete');
}

//-------------------------------
//--------ROUTING LOGIC----------
//-------------------------------

async function showRoute() {
    console.log('=== showRoute() called ===');

    if (points.length < 2) {
        alert('Need at least 2 points to calculate a route');
        return;
    }

    let startTime = getStartDatetime();
    let travelModes = getTravelModes();

    let segments = getModalSegments(points, travelModes);
    let legs = [];

    // 1. Wyznacz trasę dla szczegółowego widoku (stara logika)
    for await (const s of segments) {
        let route = await fetchBaseRoute(s);
        if (!route || route.routes.length === 0) {
            alert('Unable to compute route');
            return;
        }
        let segmentLegs = route.routes[0].legs;
        segmentLegs.forEach((l, i) => {
            l.desiredTravelMode = s.travelModes[i];
        });
        legs.push(...segmentLegs);
    }

    clearPolylines();
    clearTravelCards();
    clearWarnings();

    legs = await processLegs(legs, startTime);

    await renderRoute(legs);

    let endTime = activities[activities.length - 1].endTime;
    showRouteSummary(startTime, endTime);
    validatePlan(activities, startTime, endTime);

    // 2. Teraz wyznacz trasę dla PDF z aktualnym trybem podróży
    await calculateRouteForPdf(travelModes[0] || 'WALKING'); // Użyj pierwszego trybu lub WALKING
}

// Nowa funkcja do wyznaczania trasy dla PDF
async function calculateRouteForPdf(travelMode = null) {
    // Jeśli nie podano trybu, użyj aktualnego
    if (!travelMode) {
        travelMode = getCurrentTravelMode();
    }

    console.log('=== calculateRouteForPdf called with mode:', travelMode, '===');

    if (markers.length < 2) {
        console.log('Not enough markers for route calculation');
        updateRouteInfoForPdf(travelMode, false);
        return null;
    }

    try {
        // Konwertuj na Google TravelMode
        const googleTravelMode = convertToGoogleTravelMode(travelMode);
        console.log('Converted travel mode:', googleTravelMode);

        const request = {
            origin: markers[0].position,
            destination: markers[markers.length - 1].position,
            waypoints: markers.length > 2 ? markers.slice(1, -1).map(marker => ({
                location: marker.position,
                stopover: true
            })) : [],
            travelMode: googleTravelMode,
            optimizeWaypoints: true,
            provideRouteAlternatives: false
        };

        // Dla transportu publicznego dodaj czas
        if (googleTravelMode === google.maps.TravelMode.TRANSIT) {
            const startTime = getStartDatetime();
            request.transitOptions = {
                departureTime: startTime.toJSDate()
            };
            console.log('Added transit departure time');
        }

        console.log('Route request:', request);

        // Wyznacz trasę
        const result = await new Promise((resolve, reject) => {
            directionsService.route(request, (result, status) => {
                console.log('Directions service response:', status);
                if (status === 'OK') {
                    resolve(result);
                } else {
                    reject(new Error(`Directions service failed: ${status}`));
                }
            });
        });

        // Zapisz wynik
        directionsResult = result;

        // Oblicz sumy
        if (result.routes && result.routes[0] && result.routes[0].legs) {
            totalRouteDistance = result.routes[0].legs.reduce((sum, leg) => sum + (leg.distance?.value || 0), 0);
            totalRouteDuration = result.routes[0].legs.reduce((sum, leg) => sum + (leg.duration?.value || 0), 0);

            console.log('Route calculated:', {
                distance: totalRouteDistance,
                duration: totalRouteDuration,
                legs: result.routes[0].legs.length
            });

            // Wyświetl trasę na mapie (niebieska linia)
            directionsRenderer.setDirections(result);

            // Dopasuj mapę do całej trasy
            fitMapToRoute(result.routes[0]);

            // Zaktualizuj informacje dla PDF
            updateRouteInfoForPdf(travelMode, true);

            return result;
        } else {
            throw new Error('Invalid route result structure');
        }

    } catch (error) {
        console.error('Error calculating PDF route:', error);

        // Fallback: spróbuj z WALKING jeśli obecny tryb nie działa
        if (travelMode !== 'WALKING') {
            console.log('Trying fallback with WALKING mode');

            // Pokaż warning
            displayWarning(`Could not calculate ${travelMode} route. Using walking as fallback.`);

            // Spróbuj z WALKING
            try {
                return await calculateRouteForPdf('WALKING');
            } catch (fallbackError) {
                console.error('Fallback also failed:', fallbackError);
            }
        }

        // Jeśli wszystko zawiedzie, zaktualizuj info bez trasy
        updateRouteInfoForPdf(travelMode, false);
        return null;
    }
}

// Funkcja konwersji trybu podróży
function convertToGoogleTravelMode(mode) {
    switch (mode.toUpperCase()) {
        case 'DRIVING':
            return google.maps.TravelMode.DRIVING;
        case 'TRANSIT':
            return google.maps.TravelMode.TRANSIT;
        default:
            return google.maps.TravelMode.WALKING;
    }
}

function calculateAndDisplayFullRoute() {
    if (points.length < 2) return;

    const travelModes = getTravelModes();

    // Użyj position z AdvancedMarkerElement
    const request = {
        origin: markers[0].position, // Zmienione z getPosition() na position
        destination: markers[markers.length - 1].position, // Zmienione z getPosition() na position
        waypoints: markers.slice(1, -1).map(marker => ({
            location: marker.position, // Zmienione z getPosition() na position
            stopover: true
        })),
        travelMode: google.maps.TravelMode.WALKING, // Default mode for full route
        optimizeWaypoints: false,
        provideRouteAlternatives: false
    };

    console.log('Calculating full route with request:', request);

    directionsService.route(request, (result, status) => {
        if (status === 'OK') {
            directionsResult = result;

            // Calculate totals
            totalRouteDistance = 0;
            totalRouteDuration = 0;

            result.routes[0].legs.forEach(leg => {
                totalRouteDistance += leg.distance.value;
                totalRouteDuration += leg.duration.value;
            });

            console.log('Route calculated:', {
                distance: totalRouteDistance,
                duration: totalRouteDuration,
                legs: result.routes[0].legs.length
            });

            // Display route with a different style
            directionsRenderer.setDirections(result);
            routePath = directionsRenderer.getDirections();

            // Fit map to show entire route
            fitMapToRoute(result.routes[0]);

            // Update route info for PDF
            updateRouteInfoForPdf();
        } else {
            console.error('Directions request failed:', status);
        }
    });
}

function fitMapToRoute(route) {
    const bounds = new google.maps.LatLngBounds();

    // Extend bounds to include all route points
    route.legs.forEach(leg => {
        leg.steps.forEach(step => {
            bounds.extend(step.start_location);
            bounds.extend(step.end_location);
        });
    });

    // Also include all markers (użyj position zamiast getPosition())
    markers.forEach(marker => {
        bounds.extend(marker.position); // Zmienione z getPosition() na position
    });

    map.fitBounds(bounds, {
        padding: 50,
        maxZoom: 15
    });
}

function updateRouteInfoForPdf(travelMode = null, hasRoute = false) {
    // Jeśli nie podano trybu, pobierz aktualny
    if (!travelMode) {
        travelMode = getCurrentTravelMode();
    }

    console.log('updateRouteInfoForPdf:', { mode: travelMode, hasRoute: hasRoute });

    // Store route data for PDF export
    window.routeDataForExport = window.collectRouteDataForExport();

    console.log('Route data for export available:', !!window.routeDataForExport);

    // Pobierz lub utwórz kontener informacji
    let routeInfo = document.getElementById('route-info');
    if (!routeInfo) {
        routeInfo = document.createElement('div');
        routeInfo.id = 'route-info';
        routeInfo.className = 'alert alert-info mt-3';
        document.querySelector('.card-body').appendChild(routeInfo);
    }

    const modeInfo = getTravelModeInfo(travelMode);

    let html = `<strong><i class="fas fa-route me-2"></i>`;

    if (hasRoute) {
        // Formatuj dystans i czas
        const distanceKm = totalRouteDistance ? (totalRouteDistance / 1000).toFixed(1) : '0';
        const durationMin = totalRouteDuration ? Math.round(totalRouteDuration / 60) : 0;

        // Format czasu
        const hours = Math.floor(durationMin / 60);
        const minutes = durationMin % 60;
        let durationFormatted = '';

        if (hours > 0) {
            durationFormatted = `${hours}h ${minutes}m`;
        } else {
            durationFormatted = `${minutes}m`;
        }

        html += `Route Calculated for PDF</strong>
                <div class="mt-2">
                    <div><i class="fas fa-${modeInfo.icon} me-2"></i>Travel Mode: ${modeInfo.text}</div>
                    <div><i class="fas fa-road me-2"></i>Total Distance: ${distanceKm} km</div>
                    <div><i class="fas fa-clock me-2"></i>Total Time: ${durationFormatted} (${durationMin} minutes)</div>
                    <div><i class="fas fa-map-marker-alt me-2"></i>Stops: ${markers.length}</div>
                </div>`;

        // Dodaj ostrzeżenie jeśli dystans jest nierealny dla trybu
        if (parseFloat(distanceKm) > 100 && travelMode === 'WALKING') {
            html += `<div class="alert alert-warning mt-2">
                        <i class="fas fa-exclamation-triangle me-2"></i>
                        Very long distance for walking. Consider changing to driving mode.
                     </div>`;
        }

        if (window.routeDataForExport) {
            html += `<div class="alert alert-success mt-2">
                        <i class="fas fa-check-circle me-2"></i>
                        Route data is ready for PDF export. Click the "Export to PDF" button.
                     </div>`;
        }

    } else {
        html += `No Route Calculated</strong>
                <div class="mt-2">
                    <div><i class="fas fa-${modeInfo.icon} me-2"></i>Selected Mode: ${modeInfo.text}</div>
                    <div><i class="fas fa-map-marker-alt me-2"></i>Stops: ${markers.length}</div>
                    <div class="alert alert-warning mt-2">
                        <i class="fas fa-exclamation-triangle me-2"></i>
                        No route calculated. Click "Find Route" to calculate a route for PDF export.
                    </div>
                </div>`;
    }

    routeInfo.innerHTML = html;
}

// Funkcja pomocnicza dla informacji o trybie
function getTravelModeInfo(mode) {
    switch (mode.toUpperCase()) {
        case 'DRIVING':
            return { icon: 'car', text: 'Driving' };
        case 'TRANSIT':
            return { icon: 'bus', text: 'Public Transit' };
        default:
            return { icon: 'walking', text: 'Walking' };
    }
}

// Dodaj funkcję pomocniczą do pobrania aktualnego trybu
function getCurrentTravelMode() {
    const checkedRadio = document.querySelector('input[name="global-travel-mode"]:checked');
    return checkedRadio ? checkedRadio.value : 'WALKING';
}

// Function to collect route data for PDF export
window.collectRouteDataForExport = function () {
    console.log('=== collectRouteDataForExport called ===');

    try {
        // Sprawdź czy mamy directionsResult
        if (!directionsResult || !directionsResult.routes || directionsResult.routes.length === 0) {
            console.log('No directions result available');

            // Spróbuj stworzyć podstawowe dane z markerów
            if (markers && markers.length > 0) {
                const basicData = {
                    Waypoints: markers.map((marker, index) => ({
                        Latitude: marker.position.lat,
                        Longitude: marker.position.lng,
                        Name: marker.title || `Point ${index + 1}`,
                        Order: index + 1
                    })),
                    Segments: [],
                    TotalDistance: totalRouteDistance || 0,
                    TotalDuration: totalRouteDuration || 0,
                    HasRoute: false
                };
                console.log('Returning basic data with', basicData.Waypoints.length, 'waypoints');
                return JSON.stringify(basicData);
            }

            return null;
        }

        const route = directionsResult.routes[0];

        // Sprawdź czy trasa ma legs
        if (!route.legs || route.legs.length === 0) {
            console.log('Route has no legs');
            return null;
        }

        const routeData = {
            Waypoints: markers.map((marker, index) => ({
                Latitude: marker.position.lat,
                Longitude: marker.position.lng,
                Name: marker.title || `Point ${index + 1}`,
                Order: index + 1
            })),
            Segments: route.legs.map((leg, index) => ({
                From: index,
                To: index + 1,
                Distance: leg.distance?.value || 0,
                Duration: leg.duration?.value || 0,
                Polyline: leg.steps ? encodePolyline(leg.steps) : ''
            })),
            TotalDistance: totalRouteDistance || 0,
            TotalDuration: totalRouteDuration || 0,
            HasRoute: true,
            TravelMode: getCurrentTravelMode()
        };

        console.log('Successfully collected route data:', {
            waypoints: routeData.Waypoints.length,
            segments: routeData.Segments.length,
            distance: routeData.TotalDistance,
            duration: routeData.TotalDuration,
            mode: routeData.TravelMode
        });

        return JSON.stringify(routeData);

    } catch (error) {
        console.error('Error in collectRouteDataForExport:', error);
        return null;
    }
};

function encodePolyline(steps) {
    // Simplified polyline encoding
    const points = [];
    steps.forEach(step => {
        points.push({
            lat: step.start_location.lat(),
            lng: step.start_location.lng()
        });
        points.push({
            lat: step.end_location.lat(),
            lng: step.end_location.lng()
        });
    });

    // Return a simple string representation
    return points.map(p => `${p.lat.toFixed(6)},${p.lng.toFixed(6)}`).join('|');
}

function getStartDatetime() {
    let [startHours, startMinutes] = document.getElementById('start-time').value.split(':');
    let time = dayDate.set({ hour: parseInt(startHours), minute: parseInt(startMinutes) });
    return clampToAllowedRange(time);
}

function getTravelModes() {
    const container = document.getElementById('activitiesContainer');
    const travelDivs = Array.from(container.querySelectorAll('[id^="travel-"]'));
    return travelDivs.slice(0, travelDivs.length - 1)
        .map(div => {
            const checked = div.querySelector('input[type="radio"]:checked');
            return checked ? checked.value : "WALKING";
        });
}

function getModalSegments(points, travelModes) {
    segments = []
    let segment = { points: [points[0]], travelModes: [] };
    let basicTravelMode = getBasicTravelMode(travelModes[0]);
    for (let i = 0; i < travelModes.length; i++) {
        if (getBasicTravelMode(travelModes[i]) != basicTravelMode) {
            basicTravelMode = getBasicTravelMode(travelModes[i]);
            segments.push(segment);
            segment = { points: [points[i]], travelModes: [] };
        }
        segment.points.push(points[i + 1]);
        segment.travelModes.push(travelModes[i]);
    }
    segments.push(segment);
    return segments;
}

function getBasicTravelMode(travelMode) {
    return travelMode == "TRANSIT" ? "WALKING" : travelMode;
}

async function fetchBaseRoute(segment) {
    const intermediates = segment.points.slice(1, -1).map(p => ({
        lat: parseFloat(p.latitude),
        lng: parseFloat(p.longitude)
    }));
    return await getRoute(segment.points[0], segment.points[segment.points.length - 1], intermediates, getBasicTravelMode(segment.travelModes[0]));
}

async function processLegs(legs, time) {
    const threshold = parseInt(document.getElementById('transit-threshold').value) * 60 * 1000;
    let spotNum = 0;
    const newLegs = [];
    for (let i = 0; i < activities.length; i++) {
        if (activities[i].type === 'Spot') {
            if (spotNum > 0) {
                const leg = legs[spotNum - 1];
                let newLeg = leg;
                if (leg.desiredTravelMode == "TRANSIT" && !hasFixedDuration(points[spotNum - 1]) && leg.durationMillis > threshold) {
                    newLeg = await getTransitLeg(leg, time);
                    newLeg.type = 'Transit';
                } else {
                    newLeg.type = 'Simple';
                }
                const durationMillis = getTransportDuration(points[spotNum - 1], newLeg);
                time = time.plus({ milliseconds: parseInt(durationMillis) });
                newLegs.push(newLeg);
            }
            spotNum++;
        }

        activities[i].arrTime = time;
        time = addActivityDuration(time, activities[i]);
        activities[i].endTime = time;
    }

    return newLegs;
}

async function getTransitLeg(leg, time) {
    const transitRoute = await getRoute(
        { latitude: leg.startLocation.lat, longitude: leg.startLocation.lng },
        { latitude: leg.endLocation.lat, longitude: leg.endLocation.lng },
        [],
        'TRANSIT',
        time
    );
    return transitRoute.routes[0].legs[0];
}

async function renderRoute(legs) {
    const { Polyline } = await google.maps.importLibrary("maps");
    for (let i = 0; i < legs.length; i++) {
        const leg = legs[i];

        let travelId = `travel-${points[i].id}`;
        let travelDiv = document.querySelector(`#${travelId} .travel-info`);

        travelDiv.innerHTML = ""; // Clear previous content

        if (leg.type === 'Transit') {
            await renderTransitSteps(leg, travelDiv);
        } else {
            const durationMillis = getTransportDuration(points[i], leg);
            addEntryToTravelCard(
                travelDiv,
                `...${durationStringMillis(durationMillis)} ${leg.desiredTravelMode.toLowerCase()}...`
            );

            const polyline = new Polyline({ map, path: leg.path, strokeColor: leg.desiredTravelMode == 'DRIVING' ? 'yellow' : 'black' });
            setPolylineEventBindings(polyline, travelDiv);
            polylines.push(polyline);
        }
    }
}

async function renderTransitSteps(leg, card) {
    const { Polyline } = await google.maps.importLibrary("maps");

    let partialDurationMillis = 0;

    leg.steps.forEach(step => {
        if (step.travelMode === 'WALKING') {
            const polyline = new Polyline({ map, path: step.path });
            polylines.push(polyline);
            setPolylineEventBindings(polyline, card);

            partialDurationMillis += step.staticDurationMillis;
        } else if (step.travelMode === 'TRANSIT') {
            const polyline = new Polyline({ map, path: step.path, strokeColor: 'red' });
            polylines.push(polyline);

            if (partialDurationMillis > 0) {
                addEntryToTravelCard(card, `...${durationStringMillis(partialDurationMillis)} walking...<br>`);
                partialDurationMillis = 0;
            }

            addTransitEntryToTravelCard(card, step, step.transitDetails.transitLine.vehicle.iconURL);
            setPolylineEventBindings(polyline, card);
        }
    });
    if (partialDurationMillis > 0) {
        addEntryToTravelCard(card, `...${durationStringMillis(partialDurationMillis)} walking...<br>`);
    }
}

function validatePlan(activities, startTime, endTime) {
    if (involvesCheckout && startTime > decimalToDateTime(startTime, previousAccommodation.checkOutTime)) {
        displayWarning(`The starting time you set is after the checkout time!`);
    }
    let duration = endTime.diff(startTime)
    if (duration > luxon.Duration.fromObject({ hours: 16 })) {
        displayWarning(`The itinerary for this day takes ${duration.toFormat("hh 'h' mm 'm'")}, are you sure?`);
    }
    for (let i = 1; i < activities.length; i++) {
        if (activities[i].startTime != null && activities[i].startTime != 0 && activities[i].arrTime > decimalToDateTime(startTime, activities[i].startTime)) {
            displayWarning(`You set the starting time for ${activities[i].name} as ${decimalToDateTime(startTime, activities[i].startTime).toLocaleString(luxon.DateTime.TIME_24_SIMPLE)}, but with this plan you would arrive at ${activities[i].arrTime.toLocaleString(luxon.DateTime.TIME_24_SIMPLE)}`);
            break;
        }
    }
}

function showRouteSummary(startTime, endTime) {
    routeInfoWindow.close();

    if (polylines.length > 0) {
        const midPolyline = polylines[Math.round((polylines.length - 1) / 2)];
        const path = midPolyline.getPath().getArray();
        const pos = path[Math.round((path.length - 1) / 2)];

        const duration = endTime.diff(startTime, ["hours", "minutes"]);
        routeInfoWindow.setContent(`Total travel time: ${durationString(duration)}`);
        routeInfoWindow.setPosition(pos);
        routeInfoWindow.open(map);
    }
}

//----------------------------
//--ROUTING HELPER FUNCTIONS--
//----------------------------

function getTransportDuration(point, leg) {
    if (hasFixedDuration(point)) {
        return point.fixedDurationFrom * 3600 * 1000;
    }
    return leg.durationMillis;
}

function hasFixedDuration(point) {
    return Object.hasOwn(point, 'fixedDurationFrom') && point.fixedDurationFrom != null;
}

function addActivityDuration(time, activity) {
    if (activity.startTime > 0) {
        //Not sure about this
        let activityStartTime = decimalToDateTime(time, activity.startTime);
        if (activityStartTime > time) {
            return activityStartTime.plus({
                hours: Math.floor(parseFloat(activity.duration)),
                minutes: Math.round(60 * (parseFloat(activity.duration) % 1))
            });
        } else {
            return time.plus({
                hours: Math.floor(parseFloat(activity.duration)),
                minutes: Math.round(60 * (parseFloat(activity.duration) % 1))
            });
        }
    }
    return time.plus({
        hours: Math.floor(parseFloat(activity.duration)),
        minutes: Math.round(60 * (parseFloat(activity.duration) % 1))
    });
}

function addTransitEntryToTravelCard(card, step, imgUrl) {
    let span = document.createElement("span");
    addIconToTravelCard(span, step.transitDetails.transitLine.vehicle.iconURL);
    let lineNumberSpan = document.createElement('span');
    lineNumberSpan.innerHTML = step.transitDetails.transitLine.shortName;
    lineNumberSpan.classList.add('transit-line');
    span.appendChild(lineNumberSpan);
    span.append(step.transitDetails.departureStop.name + " ");
    let depHourSpan = document.createElement("span");
    depHourSpan.innerHTML = step.transitDetails.departureTime.toLocaleTimeString(navigator.language, {
        hour: '2-digit',
        minute: '2-digit'
    });
    depHourSpan.classList.add('hour');
    span.appendChild(depHourSpan);
    span.append(" ---> " + step.transitDetails.arrivalStop.name + " ");
    let arrHourSpan = document.createElement("span");
    arrHourSpan.innerHTML = step.transitDetails.arrivalTime.toLocaleTimeString(navigator.language, {
        hour: '2-digit',
        minute: '2-digit'
    });
    arrHourSpan.classList.add('hour');
    span.appendChild(arrHourSpan);
    span.append(', ' + durationStringMillis(parseInt(step.staticDurationMillis)));
    span.append(document.createElement("br"))
    card.appendChild(span);
}

function addEntryToTravelCard(card, text) {
    let span = document.createElement("span");
    span.innerHTML = text;
    card.appendChild(span);
}

function addIconToTravelCard(card, url) {
    let img = document.createElement('img');
    //img.width = 32;
    //img.height = 32;
    img.src = url;
    card.appendChild(img);
}

function setPolylineEventBindings(polyline, card) {
    polyline.addListener("mouseover", (event) => {
        card.classList.add("selected-item");
    });
    polyline.addListener("mouseout", (event) => {
        card.classList.remove("selected-item");
    });
    card.addEventListener("mouseenter", (event) => {
        polyline.setOptions({ strokeOpacity: 0.5 });
    });
    card.addEventListener("mouseleave", (event) => {
        polyline.setOptions({ strokeOpacity: 1 });
    });
}

function displayWarning(message) {
    const container = document.getElementById("warning-container");

    const div = document.createElement("div");
    div.classList.add("route-warning");

    const text = document.createElement("span");
    text.innerHTML = `<strong>Warning:</strong> ${message}`;

    const closeBtn = document.createElement("button");
    closeBtn.innerHTML = "&times;";
    closeBtn.addEventListener("click", () => div.remove());

    div.appendChild(text);
    div.appendChild(closeBtn);

    container.appendChild(div);
}

function clearPolylines() {
    polylines.forEach((polyline) => { polyline.setMap(null) });
    polylines = [];

    // Also clear directions renderer
    directionsRenderer.setDirections({ routes: [] });
}

function clearTravelCards() {
    const cards = document.getElementsByClassName('travel-info');

    for (const card of cards) {
        card.innerHTML = "";
    }
}

function clearWarnings() {
    var warnings = document.getElementsByClassName('route-warning');

    while (warnings[0]) {
        warnings[0].parentNode.removeChild(warnings[0]);
    }
}

//-------------------------------
//----------REORDERING-----------
//-------------------------------

document.addEventListener('DOMContentLoaded', function () {
    document.getElementById('order-apply').disabled = true;

    const container = document.getElementById('activitiesContainer');
    if (!container) return;

    // Add event listeners for drag and drop
    const items = container.querySelectorAll('.activity-item');
    let ia, ib;
    if (previousAccommodation != null) {
        ia = 1;
    } else {
        ia = 0;
    }
    if (nextAccommodation != null) {
        ib = items.length - 1;
    } else {
        ib = items.length;
    }
    Array.from(items).slice(ia, ib).forEach(item => {
        item.addEventListener('dragstart', handleDragStart);
        item.addEventListener('dragover', handleDragOver);
        item.addEventListener('dragenter', handleDragEnter);
        item.addEventListener('dragleave', handleDragLeave);
        item.addEventListener('drop', handleDrop);
        item.addEventListener('dragend', handleDragEnd);

        // Prevent text selection while dragging
        item.addEventListener('mousedown', function (e) {
            if (e.target.closest('.drag-handle') || e.target.classList.contains('drag-handle')) {
                e.preventDefault();
            }
        });
    });

    function handleDragStart(e) {
        if (isUpdating) {
            e.preventDefault();
            return;
        }

        draggedItem = this;
        e.dataTransfer.effectAllowed = 'move';
        e.dataTransfer.setData('text/html', this.innerHTML);

        // Add visual feedback
        this.style.opacity = '0.4';
        this.classList.add('dragging');
    }

    function handleDragOver(e) {
        if (e.preventDefault) {
            e.preventDefault();
        }
        e.dataTransfer.dropEffect = 'move';
        return false;
    }

    function handleDragEnter(e) {
        this.classList.add('drag-over');
    }

    function handleDragLeave(e) {
        this.classList.remove('drag-over');
    }

    function handleDrop(e) {
        if (e.stopPropagation) {
            e.stopPropagation();
        }

        if (draggedItem !== this && draggedItem) {
            // Swap elements in DOM
            const allItems = Array.from(container.querySelectorAll('.activity-item'));
            const draggedIndex = allItems.indexOf(draggedItem);
            const targetIndex = allItems.indexOf(this);

            if (draggedIndex < targetIndex) {
                this.parentNode.insertBefore(draggedItem, this.nextSibling);
            } else {
                this.parentNode.insertBefore(draggedItem, this);
            }

            // Update order numbers and send to server
            updateActivityOrder();
        }

        this.classList.remove('drag-over');
        return false;
    }

    function handleDragEnd(e) {
        items.forEach(item => {
            item.classList.remove('drag-over');
            item.classList.remove('dragging');
            item.style.opacity = '';
        });
        draggedItem = null;
    }

    function updateActivityOrder() {

        clearPolylines();
        clearTravelCards();
        clearWarnings();
        realignTravelSelectors();
        routeInfoWindow.close();

        const items = container.querySelectorAll('.activity-item');

        activities = Array.from(items).map((item, idx) => {
            const activityId = parseInt(item.dataset.activityId);
            const activity = activities.find(a => a.id === activityId);
            activity.order = idx + 1;
            return activity;
        });

        newPoints = []
        let spotCounter = 0
        for (let i = 0; i < items.length; i++) {
            const activityId = parseInt(items[i].dataset.activityId);
            point = points.find(p => p.id === activityId);
            if (point) {
                newPoints.push(point);
                newPoints[spotCounter].order = spotCounter + 1;
                point.glyphLabel.innerText = spotCounter + 1
                spotCounter++;
            }
        }
        points = newPoints;

        // recompute fixed durations
        fillFixedDurations(points);
        document.getElementById('order-apply').disabled = false;

    }

    // Add touch support for mobile devices
    if ('ontouchstart' in window) {
        let touchStartY = 0;
        let touchCurrentY = 0;
        let touchDraggedItem = null;

        items.forEach(item => {
            item.addEventListener('touchstart', function (e) {
                touchStartY = e.touches[0].clientY;
                touchDraggedItem = this;
                this.classList.add('touch-dragging');
            });

            item.addEventListener('touchmove', function (e) {
                if (!touchDraggedItem) return;

                e.preventDefault();
                touchCurrentY = e.touches[0].clientY;

                const deltaY = touchCurrentY - touchStartY;
                if (Math.abs(deltaY) > 10) {
                    touchDraggedItem.style.transform = `translateY(${deltaY}px)`;
                }
            });

            item.addEventListener('touchend', function (e) {
                if (!touchDraggedItem) return;

                const allItems = Array.from(container.querySelectorAll('.activity-item'));
                const draggedIndex = allItems.indexOf(touchDraggedItem);

                // Calculate new position based on movement
                const deltaY = touchCurrentY - touchStartY;
                const itemHeight = touchDraggedItem.offsetHeight;
                const positionsMoved = Math.round(deltaY / itemHeight);

                let newIndex = draggedIndex + positionsMoved;
                newIndex = Math.max(0, Math.min(newIndex, allItems.length - 1));

                if (newIndex !== draggedIndex) {
                    if (newIndex > draggedIndex) {
                        allItems[newIndex].parentNode.insertBefore(touchDraggedItem, allItems[newIndex].nextSibling);
                    } else {
                        allItems[newIndex].parentNode.insertBefore(touchDraggedItem, allItems[newIndex]);
                    }

                    updateActivityOrder();
                }

                touchDraggedItem.style.transform = '';
                touchDraggedItem.classList.remove('touch-dragging');
                touchDraggedItem = null;
            });
        });
    }
});

function getOptimizedActivityOrders(travelMode) {
    fixedFirst = document.getElementById("firstFixed").checked;
    fixedLast = document.getElementById("lastFixed").checked;

    if (points.length - (previousAccommodation ? 1 : 0) - (nextAccommodation ? 1 : 0) < 2) {
        return;
    }

    fetch(routeOptimizationUrl + '?' + new URLSearchParams({
        id: dayId,
        travelMode: travelMode,
        fixedFirst: fixedFirst ? points[0].id : null,
        fixedLast: fixedLast ? points[points.length - 1].id : null
    }).toString(), {
        method: 'GET',
    })
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            displayOptimizedActivityOrder(data);
        })
        .catch(error => {
            console.error('Error updating order:', error);
            // Optional: show error message to user and revert order
        });
}

function displayOptimizedActivityOrder(data) {
    const container = document.getElementById('activitiesContainer');

    // Sort API data by order just to be safe
    const sorted = [...data].sort((a, b) => a.order - b.order);

    // Build lookup for fast access
    const orderMap = new Map();
    sorted.forEach(o => orderMap.set(o.activityId, o.order));

    // --- REORDER DOM ---
    const items = Array.from(container.querySelectorAll('.activity-item'));

    if (previousAccommodation != null) {
        ia = 1;
    } else {
        ia = 0;
    }
    if (nextAccommodation != null) {
        ib = items.length - 1;
    } else {
        ib = items.length;
    }
    let itemsToBeSorted = items.slice(ia, ib);
    itemsToBeSorted.sort((a, b) => {
        const idA = parseInt(a.dataset.activityId);
        const idB = parseInt(b.dataset.activityId);
        return orderMap.get(idA) - orderMap.get(idB);
    });
    if (ia > 0) itemsToBeSorted.splice(0, 0, items[0]);
    if (ib < items.length) itemsToBeSorted.push(items[ib]);

    // Re-append in correct order
    itemsToBeSorted.forEach(item => container.appendChild(item));

    // --- UPDATE ACTIVITIES ARRAY ---
    let activitiesToBeSorted = activities.slice(ia, nextAccommodation ? activities.length - 1 : activities.length);
    activitiesToBeSorted.sort((a, b) => orderMap.get(a.id) - orderMap.get(b.id));
    activitiesToBeSorted.forEach(a => a.order = orderMap.get(a.id));
    if (ia > 0) activitiesToBeSorted.splice(0, 0, activities[0]);
    if (ib < items.length) activitiesToBeSorted.push(activities[activities.length - 1]);
    activities = activitiesToBeSorted;

    // --- UPDATE POINTS (spots only) ---
    let newPoints = [];
    let spotCounter = ia;
    if (ia > 0) newPoints.push(points[0]);

    itemsToBeSorted.slice(ia, ib).forEach(item => {
        const id = parseInt(item.dataset.activityId);
        const point = points.find(p => p.id === id);

        if (point) {
            newPoints.push(point);
            newPoints[spotCounter].order = spotCounter + 1;
            point.glyphLabel.innerText = spotCounter + 1;
            spotCounter++;
        }
    });
    if (ib < items.length) newPoints.push(points[points.length - 1]);
    points = newPoints;
    // --- RESET MAP & ROUTE DISPLAY ---
    clearPolylines();
    clearTravelCards();
    clearWarnings();
    realignTravelSelectors();
    routeInfoWindow.close();

    // recompute fixed durations based on new order
    fillFixedDurations(points);

    // Disable apply button since this is already a preview
    document.getElementById('order-apply').disabled = false;
}

function applyActivityOrder() {
    if (isUpdating) return;

    const items = document.getElementById('activitiesContainer').querySelectorAll('.activity-item');
    const orderData = [];

    items.forEach((item, index) => {
        const activityId = item.getAttribute('data-activity-id');
        const newOrder = index + 1;

        // Update displayed order
        // const orderDisplay = item.querySelector('.order-display');
        // if (orderDisplay) {
        //     orderDisplay.textContent = newOrder;
        // }

        orderData.push({
            activityId: parseInt(activityId),
            order: newOrder
        });
    });

    // Show loading indicator
    const loadingIndicator = document.getElementById('loadingIndicator');
    if (loadingIndicator) {
        loadingIndicator.style.display = 'block';
    }
    document.getElementById('order-apply').disabled = true;
    isUpdating = true;

    // Send update to server
    fetch(updateActivityOrderUrl, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        },
        body: JSON.stringify({
            dayId: dayId,
            activities: orderData
        })
    })
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            if (data.success) {
                console.log('Order updated successfully');
            } else {
                console.error('Failed to update order:', data.message);
                // Optional: show error message to user
            }
        })
        .catch(error => {
            console.error('Error updating order:', error);
            // Optional: show error message to user and revert order
        })
        .finally(() => {
            // Hide loading indicator
            if (loadingIndicator) {
                loadingIndicator.style.display = 'none';
            }
            isUpdating = false;
        });
}

function realignTravelSelectors() {
    const container = document.getElementById('activitiesContainer');

    // Get all activity items in their CURRENT order
    const activityItems = Array.from(container.querySelectorAll('.activity-item'));

    activityItems.forEach(activity => {
        const activityId = activity.dataset.activityId;

        // Only spots have travel selectors
        const travelDiv = document.getElementById('travel-' + activityId);

        if (!travelDiv) return;

        travelDiv.hidden = false;

        // If the travel div is not directly after the activity, move it
        if (activity.nextElementSibling !== travelDiv) {
            container.insertBefore(travelDiv, activity.nextElementSibling);
        }
    });

    hideLastTravelCard();
}

function hideLastTravelCard() {
    const container = document.getElementById('activitiesContainer');
    const activityItems = Array.from(container.querySelectorAll('.activity-item'));
    const finalId = activityItems[activityItems.length - 1].dataset.activityId;
    const finalTravelDiv = document.getElementById('travel-' + finalId);
    if (!finalTravelDiv) return;
    finalTravelDiv.hidden = true;
}

// Initialize when Google Maps API is loaded
//window.initMap = initMap;