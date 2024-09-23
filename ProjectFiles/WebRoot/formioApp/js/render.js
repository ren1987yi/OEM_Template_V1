var _form_ = null;
var searchParams = null;
var uri = null;
var formId = null;
window.onload = function () {

    var url = window.location.href;
    uri = new URL(url);

    // searchParams = new URLSearchParams(url);
    for (const p of uri.searchParams) {
        console.log(p);
    }

    if (!uri.searchParams.has("id") == null) {
        alert("form id not exists");
        return;
    }
    formId = uri.searchParams.get("id");


    load(formId);
    


}



function initForm(data){
    var fff = Formio.createForm(document.getElementById('formio'), data);

    fff.then(function (form) {
        _form_ = form;
        // form.on("change", function (e) {
        //     console.log("builder");
        //     var jsonSchema = JSON.stringify(form.schema, null, 4);
        //     console.log(jsonSchema);
        //     // $("#builder").val(jsonSchema);

        // });
    });


}



async function load(id){
    var url = uri.protocol + "//" + uri.host + "/api/getschema?id=" + id;
    try{
        const response =  await fetch(url, {
            method: "GET",
        });

        const _json = await response.json();
        initForm(_json);
        return _json;
    }catch(error){
        console.log('Request Failed', error);
        initForm({});
        return null;
    }
}