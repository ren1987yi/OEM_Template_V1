function getQueryString(url_string, name) {
    //const url_string = "https://www.baidu.com/t.html?name=mick&age=20"; // window.location.href
    const url = new URL(url_string);
    return url.searchParams.get(name);
}

function getUrlParameter(urlString){
    var url = new URL(urlString);
    var params = url.searchParams;
    var actname = params.get('act');
    var actions = url.pathname.split('/');

    var acts = [];
    for (let index = 0; index < actions.length - 1; index++) {
        const el = actions[index];
        acts.push(el);
    }

    acts.push(actname);
    acts.push('data');

    var _act = acts.join('/');


    acts.length = 0;
    for (let index = 0; index < actions.length - 1; index++) {
        const el = actions[index];
        acts.push(el);
    }

    //acts.push(actname);
    acts.push('test');

    var _test = acts.join('/');


    acts.length = 0;
    for (let index = 0; index < actions.length - 1; index++) {
        const el = actions[index];
        acts.push(el);
    }
    acts.push(actname);
    acts.push('datareal');

    var _ws = acts.join('/');

    var apiurl = url.protocol + '//' + url.host + _act;
    var testurl = url.protocol + '//' +url.host + _test;
    var wsurl = "ws://" + url.host + _ws;
    var chartType = params.get('c');
    var chartId = params.get('id');
    var mode = params.get('m');
    var session = params.get('session');
    if(chartType == undefined){
        chartType = '';
    }
    if(chartId == undefined){
        chartId = '';
    }

    if(mode == undefined){
        mode = 'fake';
    }

    return {
        apiurl:apiurl,
        testurl:testurl,
        wsurl:wsurl,
        chart:chartType,
        id :chartId,
        mode:mode,
        session: session
    }



}

function looseJsonParse(obj) {
    return Function('"use strict";return (' + obj + ")")();
}

function StringToObject(obj) {
    return eval(`(${obj})`);
}

function b64_to_utf8(str) {
    return decodeURIComponent(escape(window.atob(str)));
}

function isNull(str) {
    if (str == "") return true;
    if (str == null) return true;
    var regu = "^[ ]+$";
    var re = new RegExp(regu);
    return re.test(str);
}

//HTML标签转义（  < -----> &lt;）

function html2Escape(sHtml) {

    return sHtml.replace(/[<>&"]/g, function (c) {

        return { '<': '&lt;', '>': '&gt;', '&': '&amp;', '"': '&quot;' }[c];

    });

}

//HTML标签反转义（  &lt; ----> < ）

function escape2Html(str) {

    var arrEntities = { 'lt': '<', 'gt': '>', 'nbsp': ' ', 'amp': '&', 'quot': '"' };

    return str.replace(/&(lt|gt|nbsp|amp|quot);/ig, function (all, t) {

        return arrEntities[t];

    });

}




function base64_to_object(val){
    try {
        if(!isNull(val)){

            let blob = b64_to_utf8(val);
            let obj = looseJsonParse(blob);
            return obj;
        }else{
            return null;
        }
    } catch (error) {
        console.error(error);
        return null;
    }
}