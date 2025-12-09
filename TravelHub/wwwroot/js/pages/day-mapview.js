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

async function initMap() {
    // Request needed libraries.
    const { AdvancedMarkerElement } = await google.maps.importLibrary("marker");

    // Initialize the map.
    const mapElement = document.getElementById("map");
    map = new google.maps.Map(mapElement, {
        center: center,
        zoom: 12,
        mapTypeControl: true,
        streetViewControl: false,
        fullscreenControl: true,
        mapId: 'DEMO_MAP_ID'
    });

    // Oznacz mapę jako załadowaną
    mapElement.classList.add('loaded');

    infoWindow = new google.maps.InfoWindow({});
    routeInfoWindow = new google.maps.InfoWindow({});

    calibrateMap(map, points, center);

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
        point.iconImage = iconImage;
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
    });

    let walkRouteBtn = document.getElementById("route-walk-btn");
    walkRouteBtn.addEventListener('click', (event) => { showRoute() });

    bindGlobalTravelModeSelector();
    hideLastTravelCard();

    updateRouteInfoBox();
}

function bindGlobalTravelModeSelector() {
    document.querySelectorAll('input[name="global-travel-mode"]').forEach(radio => {
        radio.addEventListener('change', function () {
            const selectedMode = this.value;

            document
                .querySelectorAll('.travel-mode-radio[value="' + selectedMode + '"]')
                .forEach(r => {
                    r.checked = true;
                    r.dispatchEvent(new Event('change'));
                });
        });
    });
}

//-------------------------------
//--------ROUTING LOGIC----------
//-------------------------------

async function showRoute() {

    if (points.length < 2) {
        alert('Need at least 2 points to calculate a route');
        return;
    }

    const routeButton = document.getElementById("route-walk-btn");
    const originalText = routeButton.innerHTML;
    routeButton.innerHTML = `<i class="fas fa-spinner fa-spin me-2"></i>Calculating Route...`;
    routeButton.disabled = true;

    let startTime = getStartDatetime();
    let travelModes = getTravelModes();

    console.log(points);
    console.log(travelModes);

    let segments = getModalSegments(points, travelModes);
    let legs = [];

    console.log(segments);

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

    console.log(legs);

    clearPolylines();
    clearTravelCards();
    clearWarnings();

    legs = await processLegs(legs, startTime);

    await renderRoute(legs);

    let endTime = activities[activities.length - 1].endTime;
    showRouteSummary(startTime, endTime);
    validatePlan(activities, startTime, endTime);

    updateRouteInfoBox(legs, startTime, endTime);

    routeButton.innerHTML = originalText;
    routeButton.disabled = false;
}

function updateRouteInfoBox(legs = null, startTime = null, endTime = null) {

    // Pobierz lub utwórz kontener informacji
    let routeInfo = document.getElementById('route-info');
    if (!routeInfo) {
        routeInfo = document.createElement('div');
        routeInfo.id = 'route-info';
        routeInfo.className = 'alert alert-info mt-3';
        document.querySelector('.card-body').appendChild(routeInfo);
    }

    let html = `<strong><i class="fas fa-route me-2"></i>`;

    if (legs != null) {
        // Formatuj dystans i czas
        const distances = getTotalDistance(legs);        
        const travelTimes = getTotalTravelTime(legs);

        html += `Route Details: </strong>
                <div class="mt-2">
                    <div><i class="fas fa-clock me-2"></i>Total Itinerary Duration: ${durationString(endTime.diff(startTime, ["hours", "minutes"])) }</div>
                    <div><i class="fas fa-road me-2"></i>Total Distance: ${distances.overall} km</div>`

        if (distances.walking > 0 && distances.walking < distances.overall) {
            html += `<div><i class="fas fa-walking me-2"></i>Walking Distance: ${distances.walking} km</div>`
        }

        html += `<div><i class="fas fa-clock me-2"></i>Total Travel Time: ${durationString(travelTimes.overall)} </div>`

        if (travelTimes.walking.as('seconds') > 0 && travelTimes.walking < travelTimes.overall) {
            html += `<div><i class="fas fa-walking me-2"></i>Walking Time: ${durationString(travelTimes.walking)}</div>`
        }

        html += `<div><i class="fas fa-map-marker-alt me-2"></i>Stops: ${points.length}</div>
                </div>`;

        // Dodaj ostrzeżenie jeśli dystans jest nierealny dla trybu
        if (parseFloat(distances.walking) > 100) {
            html += `<div class="alert alert-warning mt-2">
                        <i class="fas fa-exclamation-triangle me-2"></i>
                        Very long distance for walking. Consider changing to driving mode.
                     </div>`;
        }

    } else {
        html += `No Route Calculated</strong>
                <div class="mt-2">
                    <div><i class="fas fa-map-marker-alt me-2"></i>Stops: ${points.length}</div>
                    <div class="alert alert-warning mt-2">
                        <i class="fas fa-exclamation-triangle me-2"></i>
                        No route calculated. Click "Find Route" to calculate a route for PDF export.
                    </div>
                </div>`;
    }

    routeInfo.innerHTML = html;
}

function getStartDatetime() {
    let [startHours, startMinutes] = document.getElementById('start-time').value.split(':');
    let time = dayDate.set({ hour: parseInt(startHours), minute: parseInt(startMinutes) });
    return clampToAllowedRange(time);
}

function getTravelModes() {
    const container = document.getElementById('activitiesContainer');
    const travelDivs = Array.from(container.querySelectorAll('div[id^="travel-"]'));
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
    let i = 0;
    for (let leg of legs) {

        let travelId = `travel-${points[i].id}`;
        let travelDiv = document.querySelector(`#${travelId} .travel-info`);

        travelDiv.innerHTML = ""; // Clear previous content
        const durationMillis = getTransportDuration(points[i], leg);
        if (leg.isFixed) {
            addEntryToTravelCard(
                travelDiv,
                `...${durationStringMillis(durationMillis)} ${transportTypeToMode(leg.fixedTypeFrom)}... (Overriden)`
            );
            const polyline = new Polyline({ map, path: leg.path, strokeColor: leg.desiredTravelMode == 'DRIVING' ? 'yellow' : 'black' });
            setPolylineEventBindings(polyline, travelDiv);
            polylines.push(polyline);
        }
        else if (leg.type === 'Transit') {
            await renderTransitSteps(leg, travelDiv);
        } else {
            const mode = leg.desiredTravelMode == 'TRANSIT' ? 'walking' : leg.desiredTravelMode.toLowerCase();
            addEntryToTravelCard(
                travelDiv,
                `...${durationStringMillis(durationMillis)} ${mode}...`
            );

            const polyline = new Polyline({ map, path: leg.path, strokeColor: leg.desiredTravelMode == 'DRIVING' ? 'yellow' : 'black' });
            setPolylineEventBindings(polyline, travelDiv);
            polylines.push(polyline);
        }

        i++;
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
        routeInfoWindow.setContent(`Total itinerary duration: ${durationString(duration)}`);
        routeInfoWindow.setPosition(pos);
        routeInfoWindow.open(map);
    }
}

//----------------------------
//--ROUTING HELPER FUNCTIONS--
//----------------------------

function getTotalTravelTime(legs) {
    let time = luxon.Duration.fromMillis(0);
    let walkingTime = luxon.Duration.fromMillis(0);
    for (let leg of legs) {
        time = time.plus(parseInt(leg.durationMillis));
        if (leg.desiredTravelMode == 'WALKING') {
            walkingTime = walkingTime.plus(parseInt(leg.durationMillis))
        } else if (leg.desiredTravelMode == 'TRANSIT') {
            for (let step of leg.steps) {
                if (step.travelMode == "WALKING") {
                    walkingTime = walkingTime.plus(parseInt(step.staticDurationMillis))
                }
            }
        }
    }
    return { overall: time, walking: walkingTime };
}

function getTotalDistance(legs) {
    let total = 0;
    let walking = 0;

    for (let leg of legs) {
        total += leg.distanceMeters;
        if (leg.desiredTravelMode === "WALKING") {
            walking += leg.distanceMeters;
        }
        else if (leg.desiredTravelMode === "TRANSIT" && leg.steps) {
            for (let step of leg.steps) {
                if (step.travelMode === "WALKING") {
                    walking += step.distanceMeters;
                }
            }
        }
    }
    return { overall: (total / 1000).toFixed(1), walking: (walking / 1000).toFixed(1) };
}

function getTransportDuration(point, leg) {
    if (hasFixedDuration(point)) {
        leg.isFixed = true;
        leg.fixedTypeFrom = point.fixedTypeFrom;
        return point.fixedDurationFrom * 3600 * 1000;
    } else {
        leg.isFixed = false;
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
                point.iconImage.glyphText = `${spotCounter + 1}`;
                spotCounter++;
            }

        }
        points = newPoints;

        let i = 0;
        for (let point of points) {
            let travelDiv = document.getElementById("travel-" + point.id);
            travelDiv.getElementsByClassName("to-spot")[0].innerHTML = i < points.length - 1 ? points[i + 1].name : "";
            i++;
        }

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
            point.iconImage.glyphText = `${spotCounter + 1}`;
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

    activityItems.forEach((activity, i) => {
        const activityId = activity.dataset.activityId;

        // Only spots have travel selectors
        const travelDiv = document.getElementById('travel-' + activityId);

        if (!travelDiv) return;
        travelDiv.getElementsByClassName("to-spot")[0].innerHTML = i < points.length - 1 ? points[i + 1].name : "";

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
    const activityItems = Array.from(container.querySelectorAll('div[id^="travel-"]'));
    activityItems[activityItems.length - 1].hidden = true;
}