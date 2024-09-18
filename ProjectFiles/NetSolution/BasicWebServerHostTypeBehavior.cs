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
using DotNetWebServer;
using System.Net;
[CustomBehavior]
public class BasicWebServerHostTypeBehavior : BaseNetBehavior
{

    WebApplication app;
    public override void Start()
    {
        // Insert code to be executed when the user-defined behavior is started
        if (!Node.Enable)
        {
            Log.Warning($"{Node.BrowseName} webserver not enable");
            return;
        }

        var _rootpath = Node.RootPath;
        var rUri = ResourceUri.FromProjectRelativePath(_rootpath);

        var _port = Node.Port;
        var _path = rUri.Uri;
        //_path = @"D:\code\web\form.io.test\";

        app = new WebApplication(IPAddress.Any, _port,"", _path);
        app.Start();





    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined behavior is stopped
        app.Stop();
    }

#region Auto-generated code, do not edit!
    protected new BasicWebServerHostType Node => (BasicWebServerHostType)base.Node;
#endregion
}
