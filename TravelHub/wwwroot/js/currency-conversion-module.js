$(() => {
    function setExchangeRate(selectedOption) {
        var rate = selectedOption.data('rate');
        if (rate) {
            $exchangeRateInput.val(parseFloat(rate).toFixed(6));
        } else {
            fetchExchangeRate(selectedOption.val(), tripCurrency, result => { $exchangeRateInput.val(result); });
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