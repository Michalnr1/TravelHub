// Needs: collisionCheckUrl, dayId

const startInput = document.getElementById("StartTimeString");
const durationInput = document.getElementById("DurationString");
const warningBox = document.getElementById("warning-box");

let debounceTimer = null;

function checkCollision() {
    const start = startInput.value;
    const duration = durationInput.value;

    if (!start) {
        warningBox.classList.add("d-none");
        return;
    }

    $.ajax({
        url: collisionCheckUrl,
        type: "get",
        data: {
            id: dayId,
            startTimeString: start,
            durationString: duration
        },
        success: data => {
            if (data.collision === true) {
                warningBox.innerHTML =
                    `<strong>Time conflict!</strong><br>
                         Overlaps with: <strong>${data.name}</strong><br>
                         ${data.startTimeString} – ${data.endTimeString ?? "?"}`;
                warningBox.classList.remove("d-none");
            } else {
                warningBox.classList.add("d-none");
            }
        }
    });
}

function debounceCollisionCheck() {
    clearTimeout(debounceTimer);
    debounceTimer = setTimeout(checkCollision, 400);
}


$(() => {
    if (dayId != null) {
        startInput.addEventListener("input", debounceCollisionCheck);
        durationInput.addEventListener("input", debounceCollisionCheck);
    }
});

