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
    var builder = Formio.builder(document.getElementById('builder'), data, {
        builder: {
            custom: {
                title: 'Pre-Defined Fields',
                weight: 10,
                components: {
                    firstName: {
                        title: 'First Name',
                        key: 'firstName',
                        icon: 'terminal',
                        schema: {
                            label: 'First Name',
                            type: 'textfield',
                            key: 'firstName',
                            input: true
                        }
                    },
                    lastName: {
                        title: 'Last Name',
                        key: 'lastName',
                        icon: 'terminal',
                        schema: {
                            label: 'Last Name',
                            type: 'textfield',
                            key: 'lastName',
                            input: true
                        }
                    },
                    email: {
                        title: 'Email',
                        key: 'email',
                        icon: 'at',
                        schema: {
                            label: 'Email',
                            type: 'email',
                            key: 'email',
                            input: true
                        }
                    },
                    phoneNumber: {
                        title: 'Mobile Phone',
                        key: 'mobilePhone',
                        icon: 'phone-square',
                        schema: {
                            label: 'Mobile Phone',
                            type: 'phoneNumber',
                            key: 'mobilePhone',
                            input: true
                        }
                    }
                }
            }
        }
    });

    builder.then(function (form) {
        _form_ = form;
        // form.on("change", function (e) {
        //     console.log("builder");
        //     var jsonSchema = JSON.stringify(form.schema, null, 4);
        //     console.log(jsonSchema);
        //     // $("#builder").val(jsonSchema);

        // });
    });


}




async function save() {
    if (_form_ != null) {

        var jsonSchema = JSON.stringify(_form_.schema, null, 4);
        console.log(jsonSchema);
        //alert(jsonSchema);


        var url = uri.protocol + "//" + uri.host + "/api/saveschema";
        var _body = JSON.stringify({
            id: formId,
            name:formId,
            schema: jsonSchema
        });

        try{
            const response =  await fetch(url, {
                method: "POST",
                body:_body
    
            });

            const _text = await response.text();

            if(_text == _body){
                
            }
            else{
                alert(_text);
            }
        }catch(error){
            console.log('Request Failed', error);
        }
     
    }
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