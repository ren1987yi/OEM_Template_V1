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
using FTOptix.RAEtherNetIP;
using FTOptix.System;
using FTOptix.Retentivity;
using FTOptix.UI;
using FTOptix.Alarm;
using FTOptix.CommunicationDriver;
using FTOptix.EventLogger;
using FTOptix.Core;
#endregion

public class EChartSSR_RuntimeNetLogic : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        var task = new LongRunningTask(() => {
         
            if (!EChartSSR.Render.Instance.Inited)
            {
                //var uri = ResourceUri.FromProjectRelativePath("jsLibrary\\");
                //var folder = uri.Uri;
                //EChartSSR.Render.Instance.Init(folder);

                Log.Info("EChartSSR", "init nok");

            }
            else
            {
                Log.Info("EChartSSR", "init ok");
            }

        }, LogicObject);

        task.Start();
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }
}
