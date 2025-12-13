function fetchExchangeRate(from, to, onSuccess, onError) {
    $.ajax({
        url: "/Expenses/ExchangeRate",
        type: "get",
        data: {
            from: from,
            to: to
        },
        success: onSuccess,
        error: onError
    });
}