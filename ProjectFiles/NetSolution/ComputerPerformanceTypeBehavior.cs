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

[CustomBehavior]
public class ComputerPerformanceTypeBehavior : BaseNetBehavior
{

    PeriodicTask _task;

    public override void Start()
    {
        // Insert code to be executed when the user-defined behavior is started
        _task = new PeriodicTask(CollectPerformance, 5000, Node);
        _task.Start();
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined behavior is stopped
        if(_task != null)
        {

            _task.Cancel();
            _task.Dispose();
        }
    }

    Random rnd = new Random();
    private void CollectPerformance()
    {
        //TODO emulator
        Node.CPU.SetVariableValue("Speed", 5f + rnd.NextSingle() * 5);
        Node.CPU.SetVariableValue("Utilization",(5f + rnd.NextSingle()*5f) );

        Node.Memory.SetVariableValue("Available", 8f);
        Node.Memory.SetVariableValue("InUse", 1.5f + rnd.NextSingle());

        Node.Disk.SetVariableValue("Capacity", 500f);
        Node.Disk.SetVariableValue("InUse", 301.5f);

    }

#region Auto-generated code, do not edit!
    protected new ComputerPerformanceType Node => (ComputerPerformanceType)base.Node;
#endregion
}
