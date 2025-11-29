// Needs: collisionCheckUrl, dayId, warningBox

const startInput = document.getElementById("StartTimeString");
const durationInput = document.getElementById("DurationString");

let collisionDebounceTimer = null;

function checkCollision() {
    const start = startInput.value;
    const duration = durationInput.value;

    $.ajax({
        url: collisionCheckUrl,
        type: "get",
        data: {
            id: dayId,
            startTimeString: start,
            durationString: duration
        },
        success: data => {
            warningBox.querySelectorAll(".collision-warning").forEach(el => el.remove());

            if (data.collision === true) {

                // Create a new warning div
                const div = document.createElement("div");
                div.classList.add("collision-warning");

                div.innerHTML = `<strong>Time conflict!</strong><br>
                                Overlaps with: <strong>${data.name}</strong><br>
                                ${data.startTimeString} – ${data.endTimeString ?? "?"}`;

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

function debounceCollisionCheck() {
    clearTimeout(collisionDebounceTimer);
    collisionDebounceTimer = setTimeout(checkCollision, 400);
}


$(() => {
    if (dayId != null) {
        startInput.addEventListener("input", debounceCollisionCheck);
        durationInput.addEventListener("input", debounceCollisionCheck);
    }
});

