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
    map = new google.maps.Map(document.getElementById("map"), {
        center: center,
        mapId: 'DEMO_MAP_ID'
    });
    marker = new AdvancedMarkerElement({
        map,
    });
    infoWindow = new google.maps.InfoWindow({});
    routeInfoWindow = new google.maps.InfoWindow({});

    const bounds = getMapBounds(points);

    if (points.length <= 1) {
        map.setCenter(center);
        map.setZoom(14);
    } else {
        map.fitBounds(bounds, 20);
    }

    points.forEach((point, i) => {
        let coords = { lat: parseFloat(point.latitude), lng: parseFloat(point.longitude) }

        if (isGroup) {
            var iconImage = new google.maps.marker.PinElement({});
        } else {
            point.glyphLabel = document.createElement("div");
            point.glyphLabel.style = 'color: white; font-size: 17px;';
            point.glyphLabel.innerText = i + 1;
            var iconImage = new google.maps.marker.PinElement({
                glyph: point.glyphLabel,
            });
        }
        const marker = new AdvancedMarkerElement({
            position: coords,
            map: map,
            title: point.name,
            content: iconImage.element,
        });

        // Add a click listener for each marker, and set up the info window.
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

        let cardId = `spot-${point.id}`;

        if (i == 0 && previousAccommodation?.id == point.id) {
            cardId = cardId + "-prev";
        } else if (i == points.length - 1 && nextAccommodation?.id == point.id) {
            cardId = cardId + "-next";
        }

        let listElem = document.getElementById(cardId);
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

    });

    let walkRouteBtn = document.getElementById("route-walk-btn");
    walkRouteBtn.addEventListener('click', (event) => { showRoute() });

    bindGlobalTravelModeSelector();
    hideLastTravelCard();
}



function getStartDatetime() {
    let [startHours, startMinutes] = document.getElementById('start-time').value.split(':');
    let time = dayDate.set({ hour: parseInt(startHours), minute: parseInt(startMinutes) });
    return clampToAllowedRange(time);
}

async function showRoute() {
    if (points.length < 2) return;

    let startTime = getStartDatetime();
    let travelModes = getTravelModes();

    let segments = getModalSegments(points, travelModes);
    let legs = [];

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

function getTransportDuration(point, leg) {
    if (hasFixedDuration(point)) {
        return point.fixedDurationFrom * 3600 * 1000;
    }
    return leg.durationMillis;
}
function hasFixedDuration(point) {
    return Object.hasOwn(point, 'fixedDurationFrom') && point.fixedDurationFrom != null;
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

function setAllTravelModes(mode) {
    document
        .querySelectorAll('.travel-mode-radio-button[value="' + mode + '"]')
        .forEach(radio => {
            radio.checked = true;
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

function getTravelModes() {
    const container = document.getElementById('activitiesContainer');
    const travelDivs = Array.from(container.querySelectorAll('[id^="travel-"]'));
    return travelDivs.slice(0, travelDivs.length - 1)
        .map(div => {
            const checked = div.querySelector('input[type="radio"]:checked');
            return checked ? checked.value : "WALKING";
        });
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

function showRouteSummary(startTime, endTime) {
    routeInfoWindow.close();

    const midPolyline = polylines[Math.round((polylines.length - 1) / 2)];
    const path = midPolyline.getPath().getArray();
    const pos = path[Math.round((path.length - 1) / 2)];

    const duration = endTime.diff(startTime, ["hours", "minutes"]);
    routeInfoWindow.setContent(durationString(duration));
    routeInfoWindow.setPosition(pos);
    routeInfoWindow.open(map);
}

function clearPolylines() {
    polylines.forEach((polyline) => { polyline.setMap(null) });
    polylines = [];
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

function clearTravelCards() {
    const cards = document.getElementsByClassName('travel-info');

    for (const card of cards) {
        card.innerHTML = "";
    }
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

function clearWarnings() {
    var warnings = document.getElementsByClassName('route-warning');

    while (warnings[0]) {
        warnings[0].parentNode.removeChild(warnings[0]);
    }
}



//REORDERING ACTIVITIES

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
    if (ib < items.length) newPoints.push(points[ib]);

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