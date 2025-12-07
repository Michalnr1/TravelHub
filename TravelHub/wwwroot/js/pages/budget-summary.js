// NEEDS: dataUrl, tripId, tripCurrency

function buildCharts() {

    buildCategoryChart();

    buildPersonChart();
}
function getFilterValues() {
    const personFilter = document.getElementById("FilterByPersonId");
    const categoryFilter = document.getElementById("FilterByCategoryId");
    const transfersCheckbox = document.getElementById("IncludeTransfers");
    const estimatedCheckbox = document.getElementById("IncludeEstimated");

    return {
        __RequestVerificationToken: gettoken(),
        tripId: tripId,
        FilterByPersonId: personFilter.options[personFilter.selectedIndex].value,
        FilterByCategoryId: categoryFilter.options[categoryFilter.selectedIndex].value,
        includeTransfers: transfersCheckbox.checked,
        includeEstimated: estimatedCheckbox.checked
    }
}

function updateDisplay(data) {

    setActivePersonFilter(data);

    setActiveCategoryFilter(data);

    setToplineMetrics(data);

    setCategoryTable(data);
    
    setPersonTable(data);
    
    updateCategoryChartData(data);
    
    updatePersonChartData(data);
    
    buildCharts();
}