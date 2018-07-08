var table = null;
$(function () {
    table = $('#marketPairsTable').DataTable({
        ajax: {
            url: "MarketPairs",
            data: function (d) {
                var signalNames = $('#signalsFilter').find(":selected").map(function () {
                    return $.trim($(this).text());
                }).get();

                if (signalNames.length) {
                    d.signalsFilter = signalNames;
                } else {
                    d.signalsFilter = [];
                }
            },
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
                    var outlineStyle = row.HasTradingPair ? "info" : "secondary";
                    var element = '<div style="width: 120px"><a href="https://www.tradingview.com/chart/?symbol=' + row.TradingViewName + '" target = "_blank" class="btn btn-outline-' + outlineStyle + ' btn-sm">' + data + '</a>';
                    if (row.SignalRules.length) {
                        element += '&nbsp;<i class="fas fa-bolt text-info" title="Trailing"></i>';
                    }
                    element += '</div>';
                    return element;
                }
            },
            {
                name: "Rating",
                data: "RatingList",
                type: "multi-value",
                render: function (data, type, row, meta) {
                    return data.map(function (item) { return '<div class="signal-details"><span class="signal-name">' + item.Name + '</span><span class="signal-value ' + (item.Rating != null && item.Rating >= 0 ? 'text-success' : 'text-warning') + '">' + (item.Rating != null ? item.Rating.toFixed(3) : "N/A") + '</span></div>'; }).join("");
                }
            },
            {
                name: "RatingChange",
                data: "RatingChangeList",
                type: "multi-value",
                render: function (data, type, row, meta) {
                    return data.map(function (item) { return '<div class="signal-details"><span class="signal-name">' + item.Name + '</span><span class="signal-value ' + (item.RatingChange != null && item.RatingChange >= 0 ? 'text-success' : 'text-warning') + '">' + (item.RatingChange != null ? item.RatingChange.toFixed(2) : "N/A") + '</span></div>'; }).join("");
                },
                visible: false
            },
            {
                name: "Price",
                data: "Price",
                render: function (data, type, row, meta) {
                    return data;
                }
            },
            {
                name: "PriceChange",
                data: "PriceChangeList",
                type: "multi-value",
                render: function (data, type, row, meta) {
                    return data.map(function (item) { return '<div class="signal-details"><span class="signal-name">' + item.Name + '</span><span class="signal-value ' + (item.PriceChange != null && item.PriceChange >= 0 ? 'text-success' : 'text-warning') + '">' + (item.PriceChange != null ? item.PriceChange.toFixed(2) : "N/A") + '</span></div>'; }).join("");
                }
            },
            {
                name: "Spread",
                data: "Spread",
                render: function (data, type, row, meta) {
                    return data;
                }
            },
            {
                name: "Arbitrage",
                data: "ArbitrageList",
                type: "multi-value",
                render: function (data, type, row, meta) {
                    return data.map(function (item) { return '<div class="signal-details"><span class="signal-name">' + item.Name + '</span><span class="signal-value">' + (item.Arbitrage != null ? item.Arbitrage : "N/A") + '</span></div>'; }).join("");
                }
            },
            {
                name: "Volume",
                data: "VolumeList",
                type: "multi-value",
                render: function (data, type, row, meta) {
                    return data.map(function (item) { return '<div class="signal-details"><span class="signal-name">' + item.Name + '</span><span class="signal-value">' + (item.Volume != null ? item.Volume : "N/A") + '</span></div>'; }).join("");
                }
            },
            {
                name: "VolumeChange",
                data: "VolumeChangeList",
                type: "multi-value",
                render: function (data, type, row, meta) {
                    return data.map(function (item) { return '<div class="signal-details"><span class="signal-name">' + item.Name + '</span><span class="signal-value ' + (item.VolumeChange != null && item.VolumeChange >= 0 ? 'text-success' : 'text-warning') + '">' + (item.VolumeChange != null ? item.VolumeChange.toFixed(2) : "N/A") + '</span></div>'; }).join("");
                },
                visible: false
            },
            {
                name: "Volatility",
                data: "VolatilityList",
                type: "multi-value",
                render: function (data, type, row, meta) {
                    return data.map(function (item) { return '<div class="signal-details"><span class="signal-name">' + item.Name + '</span><span class="signal-value">' + (item.Volatility != null ? item.Volatility.toFixed(2) : "N/A") + '</span></div>'; }).join("");
                }
            },
            {
                name: "TradingRules",
                data: "Config",
                render: function (data, type, row, meta) {
                    return data.Rules.join("<br/>");
                }
            },
            {
                name: "SignalRules",
                data: "SignalRules",
                render: function (data, type, row, meta) {
                    return data.join("<br/>");
                }
            }
        ],
        order: [[2, "desc"]],
        responsive: {
            details: {
                type: "column"
            }
        },
        pageLength: 25,
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
        ]
    });

    table.search();

    $.fn.dataTable.ext.type.order['multi-value-pre'] = function (d) {
        return getMultiValueAvg(d);
    };

    $.fn.dataTable.ext.search.push(
        function (settings, searchData, dataIndex) {
            var show = true;
            $(".filter").each(function (idx, element) {
                var filter = $(element);
                var value = parseFloat(filter.val());
                var valueIndex = filter.data("index");
                if (value && valueIndex) {
                    var visibleData = [];
                    for (var idx in searchData) {
                        if (settings.aoColumns.filter(function (col) { return col.idx == idx; })[0].bVisible) {
                            visibleData.push(searchData[idx]);
                        }
                    }

                    var data = visibleData[valueIndex + 1];
                    if (!isNaN(data)) {
                        if (parseFloat(data) < value) {
                            show = false;
                        }
                    } else {
                        var sum = getMultiValueAvg(data);
                        if (sum < value) {
                            show = false;
                        }
                    }
                }
            });
            return show;
        }
    );

    $('#marketPairsTable thead th:not(.control)').each(function (i) {
        var title = $('#marketPairsTable thead th').eq($(this).index()).text();
        if (title != "Name" && title != "Trading Rules" && title != "Trailing Rules" && title != "Signal Rules") {
            $(this).prepend('<input type="text" class="filter" onclick="return filterClicked(event);" placeholder="Min ' + title + '" data-index="' + i + '" />');
        }
    });

    $(table.table().container()).on('keyup', 'thead input', function () {
        table.draw();
    });

    $('#marketPairsTable tbody').on('click', 'td:not(:first-child)', function (ev) {
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

    function getMultiValueAvg(element) {
        var total = 0;
        var sum = 0;
        $(element).find(".signal-value").each(function (idx, element) {
            var value = parseFloat(element.innerText);
            if (!value) value = 0;
            sum += value;
            total++;
        });
        return sum / total;
    }

    setInterval(function () {
        refreshTable();
    }, 5000);

    document.addEventListener("visibilitychange", function () {
        refreshTable();
    }, false);

    $.get("/SignalNames", function (data) {
        $('<div class="signals-filter"><select id="signalsFilter" multiple="multiple"></div>').insertAfter(".dt-buttons");
        var signalsFilter = $("#signalsFilter");
        for (var i = 0; i < data.length; i++) {
            var signalName = data[i];
            signalsFilter.append('<option selected="selected">' + signalName + '</option>');
        }
        signalsFilter.multiselect({
            buttonText: function () {
                return "Signals";
            },
            optionClass: function () {
                return "signal-filter-option";
            },
            onChange: function () {
                refreshTable();
            }
        });
    });
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
    details.find("#signalRules").text(data.SignalRules.join(", "));
    details.find("#tradingRules").text(data.Config.Rules.join(", "));
    return details.html();
}

function filterClicked(e) {
    e.preventDefault();
    e.stopPropagation();
    return false;
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

function buyPairDefault(e) {
    var pair = $(e).closest(".row-details").find("#pair").val();
    var amount = $(e).parent().find("#amount").val();
    if (confirm("Buy " + pair + " with default settings?")) {
        $.post("BuyDefault", { pair: pair }, function (data) {
            var tr = $(e).closest('tr').prev();
            var row = table.row(tr);
            hideRow(row);
            refreshTable();
        }).fail(function (data) {
            alert("Error buying " + pair);
        });
    }
}