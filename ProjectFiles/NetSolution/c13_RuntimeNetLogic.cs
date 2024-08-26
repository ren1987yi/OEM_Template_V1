#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.NativeUI;
using FTOptix.CoreBase;
using FTOptix.WebUI;
using FTOptix.Recipe;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.System;
using FTOptix.Retentivity;
using FTOptix.UI;
using FTOptix.Alarm;
using FTOptix.Core;
using OptixWebServer.Charts;
using System.Collections.Generic;
using Newtonsoft.Json;
#endregion

public class c13_RuntimeNetLogic : WebChartHandle
{
    public override void Start()
    {
        base.Start();
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
        base.Stop();
    }
    Random rnd = new Random();
    public override void GetData(string query,out string result)
    {
        Log.Info(Owner.BrowseName);
        var mm = Convert.ToInt32(query);

        var ss = new List<int>();
        for (var i = 0; i < 9; i++)
        {
            ss.Add(rnd.Next(mm));
        }

        var data = new ChartDataDto();
        data.updateOptions = null;
        data.updateSeries = new object[]
        {
                new {
                    data = ss
                }
        };
        result = JsonConvert.SerializeObject(data);
    }
}
