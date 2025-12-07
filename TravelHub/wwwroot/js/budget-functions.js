function buildCategoryChart() {
    var categoryCtx = document.getElementById('categoryChart');

    if (!categoryCtx) return;

    var labels = JSON.parse(categoryCtx.dataset.labels);
    var values = JSON.parse(categoryCtx.dataset.values);
    var colors = JSON.parse(categoryCtx.dataset.colors);
    console.log(categoryChart);
    if (categoryChart) categoryChart.destroy();
    categoryChart = new Chart(categoryCtx, {
        type: 'doughnut',
        data: {
            labels: labels,
            datasets: [{
                data: values,
                backgroundColor: colors,
                borderWidth: 2,
                borderColor: '#fff'
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: {
                    position: 'bottom',
                },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            var label = context.label || '';
                            var value = context.raw || 0;
                            var total = context.dataset.data.reduce((a, b) => a + b, 0);
                            var percentage = Math.round((value / total) * 100);
                            return label + ': ' + value.toFixed(2) + ` ${tripCurrency} (` + percentage + '%)';
                        }
                    }
                }
            }
        }
    });
}

function buildPersonChart() {
    var personCtx = document.getElementById('personChart');
    if (!personCtx) return;
    var labels = JSON.parse(personCtx.dataset.labels);
    var values = JSON.parse(personCtx.dataset.values);
    if (personChart) personChart.destroy();
    personChart = new Chart(personCtx, {
        type: 'pie',
        data: {
            labels: labels,
            datasets: [{
                data: values,
                backgroundColor: [
                    '#4e73df', '#1cc88a', '#36b9cc', '#f6c23e', '#e74a3b',
                    '#858796', '#5a5c69', '#6f42c1', '#e83e8c', '#fd7e14'
                ],
                borderWidth: 2,
                borderColor: '#fff'
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: {
                    position: 'bottom',
                },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            var label = context.label || '';
                            var value = context.raw || 0;
                            var total = context.dataset.data.reduce((a, b) => a + b, 0);
                            var percentage = Math.round((value / total) * 100);
                            return label + ': ' + value.toFixed(2) + ` ${tripCurrency} (` + percentage + '%)';
                        }
                    }
                }
            }
        }
    });
}

function setActivePersonFilter(data) {
    const filtersActiveContainer = document.getElementById("filters-active");
    let personActive = document.getElementById("person-active");

    if (data.filterByPersonName) {
        if (!personActive) {
            const temp = document.createElement("div");
            temp.innerHTML = `<span class="badge bg-primary me-2" id="person-active"></span>`;
            personActive = temp.firstElementChild;
            const strongEl = filtersActiveContainer.querySelector("strong");
            strongEl.insertAdjacentElement("afterend", personActive);
        }
        personActive.innerHTML = `Person: ${data.filterByPersonName}`;
    } else if (personActive) {
        personActive.parentElement.removeChild(personActive);
    }
}

function setActiveCategoryFilter(data) {
    const filtersActiveContainer = document.getElementById("filters-active");
    let categoryActive = document.getElementById("category-active");
    if (data.filterByCategoryName) {
        if (!categoryActive) {
            const temp = document.createElement("div");
            temp.innerHTML = `<span class="badge bg-primary me-2" id="category-active"></span>`;
            categoryActive = temp.firstElementChild;
            const strongEl = filtersActiveContainer.querySelector("strong");
            strongEl.insertAdjacentElement("afterend", categoryActive);
        }
        categoryActive.innerHTML = `Category: ${data.filterByCategoryName}`;
    } else if (categoryActive) {
        categoryActive.parentElement.removeChild(categoryActive);
    }
}

function setToplineMetrics(data) {
    const actualTotal = document.getElementById("actual-total");
    if (actualTotal)
        actualTotal.innerHTML = `${data.totalActualExpenses.toFixed(2)} ${data.tripCurrencyString}`;

    const estimatedTotal = document.getElementById("estimated-total");
    if (estimatedTotal)
        estimatedTotal.innerHTML = `${data.totalEstimatedExpenses.toFixed(2)} ${data.tripCurrencyString}`;

    const transfersTotal = document.getElementById("transfers-total");
    if (transfersTotal)
        transfersTotal.innerHTML = `${data.totalTransfers.toFixed(2)} ${data.tripCurrencyString}`;

    const balanceContainer = document.getElementById("balance-container");
    const balanceTotal = document.getElementById("balance-total");
    if (balanceTotal) {
        balanceContainer.classList.remove("bg-danger");
        balanceContainer.classList.remove("bg-success");
        if (data.balance < 0) {
            balanceContainer.classList.add("bg-success");
        } else {
            balanceContainer.classList.add("bg-danger");
        }
        balanceTotal.innerHTML = `${Math.abs(data.balance).toFixed(2)} ${data.tripCurrencyString}`;
    }

}

function setCategoryTable(data) {
    const categoryTableContainer = document.getElementById("category-table-container");
    const categoryTableEmpty = document.getElementById("category-table-empty");
    if (data.categorySummaries.length > 0) {
        categoryTableContainer.hidden = false;
        categoryTableEmpty.hidden = true;

        document.getElementById("category-actual-total").innerHTML =
            `${data.totalActualExpenses.toFixed(2)}`;
        document.getElementById("category-estimated-total").innerHTML =
            `${data.totalEstimatedExpenses.toFixed(2)}`;

        const categoryBalance = document.getElementById("category-balance");
        categoryBalance.classList.remove("text-danger");
        categoryBalance.classList.remove("text-success");
        if (data.balance < 0) {
            categoryBalance.classList.add("text-success");
        } else {
            categoryBalance.classList.add("text-danger");
        }
        categoryBalance.innerHTML = `${Math.abs(data.balance).toFixed(2)}`;

        const categoryTable = document.getElementById('category-table');
        let html = "";
        for (let catSum of data.categorySummaries) {
            html += `<tr>
                        <td>
                            <i class="fas fa-circle me-2" style="color: ${catSum.categoryColor}"></i>
                            ${catSum.categoryName}
                        </td>
                        <td class="text-end">${catSum.actualExpenses.toFixed(2)}</td>
                        <td class="text-end">${catSum.estimatedExpenses.toFixed(2)}</td>
                        <td class="text-end ${catSum.balance >= 0 ? "text-danger" : "text-success"}">
                            ${Math.abs(catSum.balance).toFixed(2)}    
                        </td>
                        <td class="text-end">${catSum.percentageOfTotal.toFixed(1)}%</td>
                    </tr>`;
        }
        categoryTable.innerHTML = html;
    } else {
        categoryTableContainer.hidden = true;
        categoryTableEmpty.hidden = false;
    }
}

function setPersonTable(data) {
    const personTableContainer = document.getElementById("person-table-container");
    const personTableEmpty = document.getElementById("person-table-empty");
    if (data.personSummaries.length > 0) {
        personTableContainer.hidden = false;
        personTableEmpty.hidden = true;
        document.getElementById("person-actual-total").innerHTML =
            data.totalActualExpenses.toFixed(2);
        document.getElementById("person-estimated-total").innerHTML =
            data.totalEstimatedExpenses.toFixed(2);
        document.getElementById("person-transfers-total").innerHTML =
            data.totalTransfers.toFixed(2);
        document.getElementById("person-total-total").innerHTML =
            data.personSummaries.reduce((ps, a) => ps + a.total, 0).toFixed(1);
        const personTable = document.getElementById("person-table");
        let html = "";
        for (let p of data.personSummaries) {
            html += `
            <tr>
                <td>${p.personName}</td>
                <td class="text-end">${p.actualExpenses.toFixed(2)}</td>
                <td class="text-end">${p.estimatedExpenses.toFixed(2)}</td>
                <td class="text-end">${p.transfers.toFixed(2)}</td>
                <td class="text-end fw-bold">${p.total.toFixed(2)}</td>
                <td class="text-end">${p.percentageOfTotal.toFixed(1)}%</td>
            </tr>`;
        }
        personTable.innerHTML = html;
    } else {
        personTableContainer.hidden = true;
        personTableEmpty.hidden = false;
    }

}


function updateCategoryChartData(data) {
    let canvas = document.getElementById("categoryChart");
    if (!canvas) return;

    if (!data.categorySummaries || data.categorySummaries.length === 0) {
        canvas.dataset.labels = "[]";
        canvas.dataset.values = "[]";
        canvas.dataset.colors = "[]";
    } else {
        canvas.dataset.labels = JSON.stringify(data.categorySummaries.map(c => c.categoryName));
        canvas.dataset.values = JSON.stringify(data.categorySummaries.map(c => c.total));
        canvas.dataset.colors = JSON.stringify(data.categorySummaries.map(c => c.categoryColor));
    }
}

function updatePersonChartData(data) {
    let canvas = document.getElementById("personChart");
    if (!canvas) return;

    if (!data.personSummaries || data.personSummaries.length === 0) {
        canvas.dataset.labels = "[]";
        canvas.dataset.values = "[]";
        canvas.dataset.colors = "[]";
    } else {
        canvas.dataset.labels = JSON.stringify(data.personSummaries.map(c => c.personName));
        canvas.dataset.values = JSON.stringify(data.personSummaries.map(c => c.total));
    }
}

function refreshData() {
    const filters = getFilterValues();
    fetchData(filters);
}

function fetchData(filters) {
    $.ajax({
        url: dataUrl,
        type: "post",
        data: filters,
        success: updateDisplay
    });
}
