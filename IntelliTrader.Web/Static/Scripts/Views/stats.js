var table = null;
$(function () {
    table = $('#statsTable').DataTable({
        pageLength: 25,
        responsive: true,
        colReorder: true,
        stateSave: true,
        dom: 'Blrtip',
        buttons: [
            {
                extend: "colvis",
                text: "Columns"
            },
            "copy",
            "csv"
        ],
        order: [[0, "desc"]]
    });

    $('<div class="rules-analyzer"><button class="btn btn-success" onclick="showRulesAnalyzer();">Rules Analyzer</button></div>').insertAfter(".dt-buttons");
});

function showRulesAnalyzer() {
    window.location = "/Rules";
}