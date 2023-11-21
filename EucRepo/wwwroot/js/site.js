// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Functions for use in DataTables
function RenderTrueFalseAsCheckbox(data, type) {
    if (data == null) {
        return ""
    }
    if (type === 'display') {
        let checked = data.toString() === "true" ? "checked='checked'" : "";
        return '<input type="checkbox" disabled="disabled" ' + checked + '>';
    } else {
        return data.toString() === "true" ? "Yes" : "No";
    }
}

function getKeyByValue(object, value) { //used to show current name from ID in main table
    return Object.keys(object).find(key => object[key] === value);
}

function getDtSelectOptionsFromEnum(object) { //used to build select options from Enum for editor view
    let ae = [];
    for (let key in object) {
        ae.push({"label": key, "value": object[key]});
    }
    return ae;
}

function getDtSelectOptionsFromStatusOptions(object) { //used to build select options from status for editor view
    let ae = [];
    for (let key in object) {
        let thisId = parseInt(object[key].split("|")[0]);
        let thisValue = object[key].split("|")[1];
        ae.push({"label": thisValue, "value": thisId});
    }
    return ae;
}
