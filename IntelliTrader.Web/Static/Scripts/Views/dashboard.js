var table = null;
$(function () {
    table = $('#tradingPairsTable').DataTable({
        ajax: {
            url: "TradingPairs",
            type: "POST",
            dataSrc: ""
        },
        columns: [
            {
                className: 'control',
                orderable: false,
                data: null,
                defaultContent: ''
            },
            {
                name: "Name",
                data: "Name",
                render: function (data, type, row, meta) {
                    return '<a href="https://www.tradingview.com/chart/?symbol=' + row.TradingViewName + '" target = "_blank" class="btn btn-outline-info btn-sm">' + data + '</a>';
                },
                visible: false
            },
            {
                name: "FormattedName",
                data: "Name",
                render: function (data, type, row, meta) {
                    var element = '<div style="width: 120px"><a href="https://www.tradingview.com/chart/?symbol=' + row.TradingViewName + '" target = "_blank" class="btn btn-outline-info btn-sm">' + data + '</a>';
                    if (row.DCA > 0) {
                        element += '&nbsp;&nbsp;<span class="badge badge-primary" title="DCA level">' + row.DCA + '</span>';
                    }
                    element += '</div>';
                    return element;
                }
            },
            {
                name: "DCA",
                data: "DCA",
                visible: false
            },
            {
                name: "Margin",
                data: "Margin",
                render: function (data, type, row, meta) {
                    var element = "";
                    if (parseFloat(data) >= 0) {
                        element = '<span class="text-success"><strong>' + data + '</strong></span>';
                    }
                    else {
                        element = '<span class="text-warning"><strong>' + data + '</strong></span>';
                    }
                    if (row.IsTrailingSell) {
                        element += ' <i class="fas fa-bolt text-info" title="Trailing"></i>';
                    }
                    if (row.IsTrailingBuy) {
                        element += ' <i class="fas fa-bolt text-primary" title="Trailing"></i>';
                    }
                    return element;
                }
            },
            {
                name: "Target",
                data: "Target"
            },
            {
                name: "CurrentRating",
                data: "CurrentRating",
                render: function (data, type, row, meta) {
                    var element = "";
                    if (parseFloat(data) >= parseFloat(row.BoughtRating)) {
                        element = '<span class="text-success">' + data + '</span>';
                    }
                    else {
                        element = '<span class="text-warning">' + data + '</span>';
                    }
                    return element;
                }
            },
            {
                name: "BoughtRating",
                data: "BoughtRating"
            },
            {
                name: "Age",
                data: "Age"
            },
            {
                name: "Amount",
                data: "Amount"
            },
            {
                name: "CurrentCost",
                data: "CurrentCost"
            },
            {
                name: "Cost",
                data: "Cost"
            },
            {
                name: "CurrentPrice",
                data: "CurrentPrice"
            },
            {
                name: "BoughtPrice",
                data: "BoughtPrice"
            },
            {
                name: "CurrentSpread",
                data: "CurrentSpread"
            },
            {
                name: "SignalRule",
                data: "SignalRule"
            },
            {
                Name: "TradingRules",
                data: "TradingRules"
            },
            {
                name: "OrderDates",
                data: "OrderDates",
                visible: false
            },
            {
                name: "OrderIds",
                data: "OrderIds",
                visible: false
            }
        ],
        order: [[4, "desc"]],
        responsive: {
            details: {
                type: "column"
            }
        },
        paging: false,
        colReorder: true,
        stateSave: true,
        dom: 'Bfrtp',
        buttons: [
            {
                extend: "colvis",
                text: "Columns"
            },
            "copy",
            "csv",
            {
                text: 'Log',
                action: function (e, dt, node, config) {
                    $('#logEntries').collapse('toggle');
                }
            }
        ],
        footerCallback: function (row, data, start, end, display) {
            $(this.api().column("Name:name").footer()).html("Total: " + this.api().column("Name:name").data().length);
            $(this.api().column("FormattedName:name").footer()).html("Total: " + this.api().column("FormattedName:name").data().length);
            $(this.api().column("Margin:name").footer()).html("Avg: " + this.api().column("Margin:name").data().average().toFixed(2));
            $(this.api().column("Cost:name").footer()).html("Total: " + this.api().column("Cost:name").data().sum().toFixed(8));
            $(this.api().column("CurrentCost:name").footer()).html("Total: " + this.api().column("CurrentCost:name").data().sum().toFixed(8));
            $(this.api().column("Age:name").footer()).html("Avg: " + this.api().column("Age:name").data().average().toFixed(2));
            $(this.api().column("CurrentRating:name").footer()).html("Avg: " + this.api().column("CurrentRating:name").data().average().toFixed(3));
            $(this.api().column("BoughtRating:name").footer()).html("Avg: " + this.api().column("BoughtRating:name").data().average().toFixed(3));
        }
    });

    $('#tradingPairsTable tbody').on('click', 'td:not(:first-child)', function (ev) {
        if (ev.target.tagName === "A")
            return;
        var tr = $(this).closest('tr');
        var row = table.row(tr);
        if (row.child.isShown()) {
            hideRow(row);
        }
        else {
            showRow(row);
        }
    });

    setInterval(function () {
        refreshTable();
    }, 5000);

    document.addEventListener("visibilitychange", function () {
        refreshTable();
    }, false);
});

function refreshTable() {
    if (!document.hidden && $(".additional-details").length == 0 && $(".dtr-details").length == 0) {
        table.ajax.reload(null, false);
    }
}

function showRow(row) {
    row.child(format(row.data())).show();
    $(row.node()).addClass('shown');
}

function hideRow(row) {
    row.child.hide();
    $(row.node()).removeClass('shown');
}

function format(data) {
    var details = $($("#rowDetails").html());
    details.find("#pair").val(data.Name);
    details.find("#amount").attr("value", data.Amount).attr("min", 0);

    var swapPairContainer = details.find("#swapPairContainer");
    if (data.SwapPair) {
        swapPairContainer.show();
        details.find("#swapPair").text(data.SwapPair);
    } else {
        swapPairContainer.hide();
    }

    details.find("#signalRule").text(data.SignalRule);
    details.find("#tradingRules").text(data.TradingRules.join(", "));
    details.find("#orderDates").text(data.OrderDates.join(", "));
    details.find("#orderIds").text(data.OrderIds.join(", "));
    details.find("#lastBuyMargin").text(data.LastBuyMargin);
    return details.html();
}

function showSettings(e) {
    var pair = $(e).closest(".row-details").find("#pair").val();
    var tr = $(e).closest('tr').prev();
    var row = table.row(tr);
    var config = row.data().Config;
    $("#modalTitle").text(pair + " Settings");
    $("#modalContent").html("<pre>" + JSON.stringify(config, null, 4) + "</pre>");
    $("#modal").modal('show');
}

function sellPair(e) {
    var pair = $(e).closest(".row-details").find("#pair").val();
    var amount = $(e).parent().find("#amount").val();
    if (confirm("Sell " + amount + " " + pair + "?")) {
        $.post("Sell", { pair: pair, amount: amount }, function (data) {
            var tr = $(e).closest('tr').prev();
            var row = table.row(tr);
            hideRow(row);
            refreshTable();
        }).fail(function (data) {
            alert("Error selling " + pair);
        });
    }
}

function buyPair(e) {
    var pair = $(e).closest(".row-details").find("#pair").val();
    var amount = $(e).parent().find("#amount").val();
    if (confirm("Buy " + amount + " " + pair + "?")) {
        $.post("Buy", { pair: pair, amount: amount }, function (data) {
            var tr = $(e).closest('tr').prev();
            var row = table.row(tr);
            hideRow(row);
            refreshTable();
        }).fail(function (data) {
            alert("Error buying " + pair);
        });
    }
}

function swapPair(e) {
    var pair = $(e).closest(".row-details").find("#pair").val();
    var swap = prompt("Enter a pair to swap " + pair + " for");
    if (swap) {
        $.post("Swap", { pair: pair, swap: swap }, function (data) {
            var tr = $(e).closest('tr').prev();
            var row = table.row(tr);
            hideRow(row);
            refreshTable();
        }).fail(function (data) {
            alert("Error swapping " + pair);
        });
    }
}