#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.SQLiteStore;
using FTOptix.HMIProject;
using FTOptix.NativeUI;
using FTOptix.CoreBase;
using FTOptix.WebUI;
using FTOptix.Recipe;
using FTOptix.Store;
using FTOptix.System;
using FTOptix.NetLogic;
using FTOptix.Retentivity;
using FTOptix.UI;
using FTOptix.Alarm;
using FTOptix.Core;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;
using FTOptix.EventLogger;
#endregion

public class ChartGeneral_RuntimeNetLogic : BaseNetLogic
{

    IUAVariable vHost, vChartName, vBgColor, vQuery;


    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }



}
