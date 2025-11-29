const MIN_CHARS = 3;
const DEBOUNCE_MS = 300;

let debounceTimer;

async function getAutocompleteResults(query, resultsDiv, input) {
    $.ajax({
        url: "/Trips/Airports",
        type: "get",
        data: {
            query: query
        },
        success: data => {
            showResults(data, resultsDiv, input);
        }
    });
}

function addAutocompleteFunctionality(input, resultsDiv) {
    input.addEventListener("input", () => {
        clearTimeout(debounceTimer);
        const query = input.value.trim();

        if (query.length < MIN_CHARS) {
            resultsDiv.innerHTML = "";
            return;
        }

        debounceTimer = setTimeout(async _ => getAutocompleteResults(query, resultsDiv, input), DEBOUNCE_MS);
    });

    document.addEventListener("click", (e) => {
        if (!e.target.closest(".autocomplete-container")) {
            resultsDiv.innerHTML = "";
        }
    });
}

function showResults(results, resultsDiv, input) {
    resultsDiv.innerHTML = "";
    resultsDiv.hidden = false;

    if (!results || results.length === 0) {
        resultsDiv.innerHTML = "<div class='autocomplete-item'>No results</div>";
        return;
    }

    results.forEach(airport => {
        const div = document.createElement("div");
        div.className = "autocomplete-item";
        div.innerHTML = `
                    <strong>${airport.airportName}</strong>
                    <span>${airport.airportCode}</span>
                `;

        div.addEventListener("click", () => {
            input.value = airport.airportCode;
            resultsDiv.innerHTML = "";
            resultsDiv.hidden = true;
        });

        resultsDiv.appendChild(div);
    });
}