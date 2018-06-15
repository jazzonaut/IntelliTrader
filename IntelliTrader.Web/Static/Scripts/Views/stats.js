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
});