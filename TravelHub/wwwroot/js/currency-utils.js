function fetchExchangeRate(from, to, onSuccess) {
    $.ajax({
        url: "/Expenses/ExchangeRate",
        type: "get",
        data: {
            from: from,
            to: to
        },
        success: onSuccess
    });
}