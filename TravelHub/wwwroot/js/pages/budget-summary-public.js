// NEEDS: dataUrl, tripId, tripCurrency

function buildCharts() {

    buildCategoryChart();

}
function getFilterValues() {
    const categoryFilter = document.getElementById("FilterByCategoryId");
    const estimatedCheckbox = document.getElementById("IncludeEstimated");

    return {
        __RequestVerificationToken: gettoken(),
        tripId: tripId,
        FilterByCategoryId: categoryFilter.options[categoryFilter.selectedIndex].value,
        includeEstimated: estimatedCheckbox.checked
    }
}

function updateDisplay(data) {

    setActiveCategoryFilter(data);

    setToplineMetrics(data);

    setCategoryTable(data);

    updateCategoryChartData(data);
    
    buildCharts();
}