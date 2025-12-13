//Needs defined $selected, $exchangeRateInput, and tripCurrency

$(() => {
    function setExchangeRate(selectedOption) {
        errorContainer = document.getElementById('exchange-error');
        if (errorContainer) errorContainer.innerHTML = "";
        var rate = selectedOption.data('rate');
        if (rate) {
            $exchangeRateInput.val(parseFloat(rate).toFixed(6));
        } else {
            fetchExchangeRate(selectedOption.val(), tripCurrency,
                result => { $exchangeRateInput.val(result); },
                result => {
                    errorContainer = document.getElementById('exchange-error');
                    if (errorContainer) errorContainer.innerHTML = "Failed to fetch exchange rate";
                });
        }
    }

    // Ustaw domyślny kurs po załadowaniu strony tylko jeśli waluta jest wybrana
    var $initialSelectedOption = $currencySelect.find('option:selected');
    if ($initialSelectedOption.length > 0 && $initialSelectedOption.val() !== '') {
        if ($exchangeRateInput.val() === '' || $exchangeRateInput.val() === '0') {
            setExchangeRate($initialSelectedOption);
        }
    }

    // Obsługa zmiany waluty
    $currencySelect.on('change', function () {
        var $selected = $(this).find('option:selected');
        if ($selected.val() !== '') {
            setExchangeRate($selected);
        }
    });
});