var configures;
var domError;
const chartId = '#main';
var chart;
window.onload = async () => {

    configures = getUrlParameters(window.location.href);

    console.log(configures);

    domError = document.getElementById("error");
	domError.innerHTML = "";
    

    var el = document.querySelector(chartId);
    if(el != undefined){
        el.style.backgroundColor = configures.background;
    }
    
    let url =  "Chart/Test";
    let res = await pingServer(url);
    if(!res){
        domError.innerHTML = "Server DAD";
    }else{

        
        
        
        if(configures.lib == 'apex'){
            url = 'Chart/GetOption';
            var option = await getOption(url);
            
            if(option != null){
                chart = new ApexCharts(
                    document.querySelector("#main"),
                    option
                );
                chart.render();
                resizeChart();
                
                
                await changeData();
                
                
                if(configures.autoRefresh && configures.period > 0){
                    setInterval(async () => {
                        await changeData();
                    }, configures.period * 1000);
                }
                
                
                
            }
            
        }else{
            
        }
        
    }
        
        


    

	window.addEventListener('resize', function () {
		resizeChart();
	});

    function resizeChart(){
        if (chart != undefined) {
			option.chart.width = window.innerWidth;
			option.chart.height = window.innerHeight;

			chart.updateOptions({
				chart: {
					height: window.innerHeight -30,
					width: window.innerWidth - 30
				}
			});
		}
    }

    async function changeData() {
        let url = 'Chart/GetData';
        var data = await getData(url);

        if(typeof(data.updateOptions) != "undefined" && data.updateOptions != null){
            try{

                chart.updateOptions(data.updateOptions);
            }catch(ex){
                console.error(ex);
            }
        }
      

        if(typeof(data.updateSeries) != "undefined" && data.updateSeries != null){
            try{

                chart.updateSeries(data.updateSeries);
            }catch(ex){
                console.error(ex);
            }
        }
    }
    

    function getUrlParameters(urlString){

        var url = new URL(urlString)
        var params = url.searchParams;
        let lib = params.get('lib');//which charts library
        let chart = params.get('chart');//chartname
        let background = params.get('bg');//background color
        let query = params.get('q');//query
        let apiAdr = params.get('api');//web api address
        let autoRefresh = params.get('auto');//auto refresh
        let period = -1;
        try{
            
            let val = params.get('period');//auto refresh period
            period = parseInt(val);
        }catch{
            period = -1;
        }
        apiAdr = url.protocol + url.hostname + ":" + url.port + "/" + "Chart/";

        return {
            host:url.hostname,
            port:url.protocol,
            protocol:url.protocol,
            lib:lib,
            apiAdr:apiAdr,
            chartName:chart,
            background:background == undefined ? '#ffffff':background,
            query:query,
            autoRefresh:(autoRefresh === "true"),
            period:period,
        };

    }



    async function pingServer(url){
        let result = false;
        await fetch(url, {
            method: 'GET',
            cache: 'no-cache',
        })
        .then(response => {
            return true;
        }).then(data=>{
            result = true;
        })
        .catch(error=>{


        });
        return result;
    }


    async function getOption(url){
        let data = {
            Chart:configures.chartName
        };

        let __option;
        await fetch(url, {
            method: 'POST',
            cache: 'no-cache',
            body: JSON.stringify(data),
        }).then(response =>{
            if(!response.ok){

                throw new Error('HTTP 请求错误 : $(response.status)');
            }
            console.log(response);
            return response.json();
        }).then(data => {
            console.log(data);
            __option = data;
            
        }).catch(error=>{
            console.error('There was a problem with the fetch operation:', error);
        });

        return __option;
    }

    async function getData(url){
        let data = {
            Chart:configures.chartName,
            Query:configures.query,
        };

        let __option;
        await fetch(url, {
            method: 'POST',
            cache: 'no-cache',
            body: JSON.stringify(data),
        }).then(response =>{
            if(!response.ok){

                throw new Error('HTTP 请求错误 : $(response.status)');
            }
            return response.json();
        }).then(data => {
            console.log(data);
            __option = data;
            
        }).catch(error=>{
            console.error('There was a problem with the fetch operation:', error);
        });

        return __option;
    }
}


