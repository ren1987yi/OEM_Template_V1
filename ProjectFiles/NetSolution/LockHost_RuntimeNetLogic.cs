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
#endregion

public class LockHost_RuntimeNetLogic : BaseNetLogic
{

    PeriodicTask _task1s;

    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        _task1s = new PeriodicTask(Task_Handle, 1000, LogicObject);
        _task1s.Start();
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
        _task1s.Cancel();
    }

    public void Task_Handle()
    {
        var m = Owner.GetAlias("ActiveSession");
        if(m == null )
        {
            Owner.SetVariableValue("ActiveSession",NodeId.Empty);
        }
        else
        {
            if(!m.IsValid || m.NodeId == NodeId.Empty)
            {
                Owner.SetVariableValue("ActiveSession", NodeId.Empty);
            }
        }
    }
}
