var editors = {};

$(function () {
    $(".config-container").each(function (idx, element) {
        var json = $(element).find(".config-value").text();
        var container = $(element).find(".config-editor");
        var configName = container.data("config");
        try {
            var options = {
                mode: 'code',
                modes: ['code', 'tree', 'form', 'text', 'view'],
                onModeChange: function (newMode, oldMode) {
                    if (newMode == "code") {
                        editor.editor.setOptions({ maxLines: Infinity });
                    }
                }
            };
            var editor = new JSONEditor(container[0], options);
            editor.editor.setOptions({ maxLines: Infinity });
            editor.setText(json);
            editors[configName] = editor;
        } catch (ex) {
            container.html('<span class="text-danger">Editing not available - invalid json</span>');
        }
    });
});

function refreshAccount() {
    if (confirm("Refresh Account?")) {
        $.get("/RefreshAccount", function (data) {

        }).fail(function (data) {
            alert("Unable to refresh account");
        });
    }
}

function restartServices() {
    if (confirm("Restart Services?")) {
        $.get("/RestartServices", function (data) {

        }).fail(function (data) {
            //alert("Unable to restart services");
        });
    }
}

function logout() {
    if (confirm("Log out?")) {
        window.location.href = "/Logout";
    }
}

function saveConfig(e) {
    var tab = $(e).closest(".tab-pane");
    var configName = tab.find(".config-editor").data("config");
    var editor = editors[configName];
    var json = JSON.stringify(editor.get(), null, 2);
    var configStatus = tab.find("#saveConfigStatus");
    configStatus.text("Saving...");
    $.post("/SaveConfig", { name: configName, definition: json }, function (data) {
        configStatus.text("Saved");
        setTimeout(function () {
            configStatus.text("");
        }, 1000);
    }).fail(function (data) {
        alert("Unable to save configuration");
    });
}