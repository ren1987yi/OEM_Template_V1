var mm = Convert.ToInt32(query);
var rnd = new Random();
var ss = new List<int>();
var ss2 = new List<int>();
for (var i = 0; i < 9; i++)
{
    ss.Add(rnd.Next(mm));
    ss2.Add(rnd.Next(mm)+300);
}

var data = new ChartDataDto();
data.updateOptions = null;
data.updateSeries = new object[]
{
                new {
                    data = ss
                },   new {
                    data = ss2
                }
};
return JsonConvert.SerializeObject(data);