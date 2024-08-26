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
using Newtonsoft.Json;
using OptixWebServer.Charts;
using System.Collections.Generic;
#endregion

public class c14_RuntimeNetLogic : WebChartHandle
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        base.Start();
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
        base.Stop();
    }

    Random rnd = new Random();
    public override void GetData(string query, out string result)
    {
        Log.Info(Owner.BrowseName);
        var ss = new List<int>();
        for (var i = 0; i < 5; i++)
        {
            ss.Add(rnd.Next(100));
        }

        var data = new ChartDataDto();
        data.updateOptions = null;
        data.updateSeries = ss;
        result = JsonConvert.SerializeObject(data);
    }
}
