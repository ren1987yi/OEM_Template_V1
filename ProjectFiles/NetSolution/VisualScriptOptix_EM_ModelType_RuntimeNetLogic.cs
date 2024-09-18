#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.SQLiteStore;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.WebUI;
using FTOptix.Store;
using FTOptix.RAEtherNetIP;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.CommunicationDriver;
using FTOptix.Core;
#endregion

public class VisualScriptOptix_EM_ModelType_RuntimeNetLogic : BaseNetLogic
{
    RemoteVariableSynchronizer synchronizer;

    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        synchronizer = new RemoteVariableSynchronizer();

        var v = Owner.GetVariable("State");
        synchronizer.Add(v);

        var nIO = Owner.GetAlias("Interface");
        v = nIO.GetVariable("Command");
        synchronizer.Add(v);

        v = nIO.GetVariable("Response");
        synchronizer.Add(v);

        var nifs = Owner.GetObject("InputFloats");
        v = nifs.GetVariable("V1");
        synchronizer.Add(v);
        v = nifs.GetVariable("V2");
        synchronizer.Add(v);
        v = nifs.GetVariable("V3");
        synchronizer.Add(v);
        v = nifs.GetVariable("V4");
        synchronizer.Add(v);
        v = nifs.GetVariable("V5");
        synchronizer.Add(v);
        v = nifs.GetVariable("V6");
        synchronizer.Add(v);
        v = nifs.GetVariable("V7");
        synchronizer.Add(v);
        v = nifs.GetVariable("V8");
        synchronizer.Add(v);
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped

        if(synchronizer != null)
        {

            synchronizer.Dispose();
            synchronizer = null;
        }
    }
}
