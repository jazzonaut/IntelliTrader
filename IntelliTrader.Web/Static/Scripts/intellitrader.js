$(function () {
    if (window.isAuthenticated) {
        setInterval(function () {
            updateStatus();
        }, 5000);
        setStatus("none");
        updateStatus();
        document.addEventListener("visibilitychange", function () {
            updateStatus();
        }, false);
    }
});

function updateStatus() {
    if (document.hidden)
        return;
    setStatus("refreshing");
    $.get("/Status", function (data) {
        var accountBalance = $("#accountBalance");
        accountBalance.text(data.Balance.toFixed(8));
        var globalRating = $("#globalRating");
        globalRating.text(data.GlobalRating);
        if (parseFloat(data.GlobalRating) > 0) {
            globalRating.removeClass("text-warning");
            globalRating.addClass("text-success");
        }
        else {
            globalRating.removeClass("text-success");
            globalRating.addClass("text-warning");
        }
        var trailingBuys = $("#trailingBuys");
        trailingBuys.text(data.TrailingBuys.length);
        trailingBuys.attr("title", "Buys:\r\n" + data.TrailingBuys.join("\r\n"));
        var trailingSells = $("#trailingSells");
        trailingSells.text(data.TrailingSells.length);
        trailingSells.attr("title", "Sells:\r\n" + data.TrailingSells.join("\r\n"));
        var trailingSignals = $("#trailingSignals");
        trailingSignals.text(data.TrailingSignals.length);
        trailingSignals.attr("title", "Signals:\r\n" + data.TrailingSignals.join("\r\n"));
        var healthChecks = $("#healthChecks");
        if (data.TradingSuspended) {
            healthChecks.removeClass("badge-success");
            healthChecks.addClass("badge-danger");
            healthChecks.text("OFF");
        }
        else {
            healthChecks.removeClass("badge-danger");
            healthChecks.addClass("badge-success");
            healthChecks.text("ON");
        }
        data.HealthChecks.sort(function (a, b) { return a.Name > b.Name; });
        healthChecks.attr("title", data.HealthChecks.map(function (check) { return check.Name + ": " + new Date(check.LastUpdated).toTimeString().split(' ')[0] + (check.Failed ? " (Failed)" : " (OK)"); }).join("\r\n"));
        var logEntries = $("#logEntries");
        logEntries.html(data.LogEntries.join("<br />"));
    }).fail(function (data) {
        setStatus("error");
    });
}

function setStatus(status) {
    if (status == "refreshing") {
        $("#statusRefreshIcon").stop().fadeIn(700).fadeOut(700);
        $("#statusWarningIcon").stop().hide();
    }
    else if (status == "error") {
        $("#statusRefreshIcon").stop().hide();
        $("#statusWarningIcon").stop().show();
    }
    else {
        $("#statusRefreshIcon").stop().hide();
        $("#statusWarningIcon").stop().hide();
    }
}

jQuery.fn.dataTable.Api.register('average()', function () {
    var data = this.flatten();
    var sum = data.reduce(function (a, b) {
        return (a * 1) + (b * 1); // cast values in-case they are strings
    }, 0);
    return sum / data.length;
});

jQuery.fn.dataTable.Api.register('sum()', function () {
    var data = this.flatten();
    var sum = data.reduce(function (a, b) {
        return (a * 1) + (b * 1); // cast values in-case they are strings
    }, 0);
    return sum;
});