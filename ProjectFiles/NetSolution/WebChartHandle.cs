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
using GCOptixToolkit;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;
using FTOptix.EventLogger;
#endregion

public class WebChartHandle : BaseNetLogic
{
    protected IUAVariable vActive;
    protected IUAVariable vPeriod;
    protected IUAVariable vData;
    PeriodicTask taskRefresh;
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        //if(Owner == null || Owner.GetType() == typeof(ChartConfigureType))
        //{
        //    return;
        //}
        vActive = Owner.GetVariable("IsActive");
        vPeriod = Owner.GetVariable("Period");
        vData= Owner.GetVariable("Data");
        if (vActive == null || vPeriod == null || vData == null)
        {
            return;
        }

        if ((bool)vActive.Value)
        {
            taskRefresh = new PeriodicTask(ActiveRefreshData, vPeriod.Value, LogicObject);
            taskRefresh.Start();
            Log.Info("WebChartHandle", "timer on");
        }else
        {
            Log.Info("WebChartHandle", "timer off");
        }

    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
        if(taskRefresh != null)
        {

            taskRefresh.Cancel();
            taskRefresh.Dispose();
            taskRefresh = null;
        }
    }


    public virtual void GetData(string query,out string result)
    {
        throw new NotImplementedException();
    }

    void ActiveRefreshData()
    {
        GetData(string.Empty,out var result);
        vData.Value = result;
    }
}
