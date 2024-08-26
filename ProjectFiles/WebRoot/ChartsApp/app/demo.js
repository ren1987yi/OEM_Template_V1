window.onload = () => {

	

	// console.log(looseJsonParse("{a:(4-1), b:function(){}, c:new Date()}"));
	// console.log(looseJsonParse(a));



	// 基于准备好的dom，初始化echarts实例




	var domError = document.getElementById("error");
	domError.innerHTML = "";
	// 指定图表的配置项和数据
	
	// //   console.log(JSON.stringify(fff));
	// //   // 使用刚指定的配置项和数据显示图表。
	// myChart.setOption(option);
	var option = null;
	var chart = null;
     option = {
        series: [{
          name: "Desktops",
          data: [10, 41, 35, 51, 49, 62, 69, 91, 148]
      }],
        chart: {
        height: 350,
        type: 'line',
        zoom: {
          enabled: false
        }
      },
      dataLabels: {
        enabled: false
      },
      stroke: {
        curve: 'straight'
      },
      title: {
        text: '',
        align: 'left'
      },
      grid: {
        row: {
          colors: ['#f3f3f3', 'transparent'], // takes an array which will be repeated on columns
          opacity: 0.5
        },
      },
      xaxis: {
        categories: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep'],
      }
      };

    option.chart.height = window.innerHeight - 30;
    option.chart.width = window.innerWidth - 20;



	///set background color
	document.body.style.backgroundColor = "#ffffff";

    chart = new ApexCharts(
        document.querySelector("#main"),
        option
    );
    chart.render();
    changeData();



	window.addEventListener('resize', function () {
		if (chart != undefined) {
			option.chart.width = window.innerWidth;
			option.chart.height = window.innerHeight;

			chart.updateOptions({
				chart: {
					height: window.innerHeight,
					width: window.innerWidth
				}
			});
		}
	})

    function changeData() {
        var newData = []
        for(var i=0;i<9;i++){
            newData.push(Math.floor(Math.random() * 100))
        }
        chart.updateSeries([
            {
                
                data:newData
            }

        ]);
	}

    var interval = 5000;
    setInterval(() => {
		changeData();
	}, interval);


  

};








