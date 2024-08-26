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
#endregion


using LiteWebServer;
using LiteWebServer.Controller;
using LiteWebServer.Server;
using Newtonsoft.Json;

using System.Linq;
using System.Threading.Tasks;
using Godot;
using System.Collections.Generic;


[CustomBehavior]
public class GodotServerTypeBehavior : BaseNetBehavior
{
    WebApplication app;
    //HttpServer2 server = null;
    //WebSocketServiceHost wsHost = null;
    PeriodicTask taskCycle = null;
    PeriodicTask taskClear = null;
    IUANode nodeData = null;
    IUANode nodeEvents = null;
    public override void Start()
    {
        // Insert code to be executed when the user-defined behavior is started
        var uri = ResourceUri.FromProjectRelativePath("WebRoot\\GodotApp");
        var docroot = uri.Uri;
        nodeData = Node.GetAlias("DataPool");
        nodeEvents = Node.GetObject("Events");

        taskCycle = new PeriodicTask(TaskCycle_Handle, 50, Node);
        taskClear = new PeriodicTask(TaskClearEvens_Handle, 5000, Node);

        var builder = WebApplication.CreateBuilder();
        builder.Port = 49000;

        app = builder.Build();
        app.DocumentRootPath = docroot;
        app.MapController("GodotServerTypeBehavior");
        app.UseStaticFiles(
            new Dictionary<string, string>()
            {
                { "Cross-Origin-Opener-Policy", "same-origin" }
                ,{"Cross-Origin-Embedder-Policy", "require-corp"}
                //,{"Cross-Origin-Embedder-Policy", "credentialless"}
            }
        );

        app.AddWebsocketController<CommController>("/comm", (e) =>
        {
            e.nodeEvents = nodeEvents;
        });

        //有的时候，tcp端口被排除了
        app.Run();


        taskCycle.Start();
        taskClear.Start();
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined behavior is stopped
        taskCycle.Cancel();
        taskCycle = null;
        taskClear.Cancel();
        taskClear = null;
        app.Stop();
        app = null;
    }


    private void TaskCycle_Handle()
    {
        var d = new
        {
            Type = SendPackageType.UpdateData,
            Target = "*",
            Data = from v in nodeData.Children.OfType<IUAVariable>()
                   select new { n = v.BrowseName, v = v.Value.Value }
        };
        app.Broadcast("/comm", JsonConvert.SerializeObject(d));

    }


    private void TaskClearEvens_Handle()
    {
        var n = DateTime.Now;
        lock (nodeEvents)
        {
            foreach (var v in nodeEvents.Children.OfType<UAVariable>())
            {
                var s = v.BrowseName;
                var times = s.Split("_");
                var datetime = new DateTime(
                    Convert.ToInt32(times[0])
                    , Convert.ToInt32(times[1])
                    , Convert.ToInt32(times[2])
                    , Convert.ToInt32(times[3])
                    , Convert.ToInt32(times[4])
                    , Convert.ToInt32(times[5])
                    , Convert.ToInt32(times[6])
                    );
                //var datetime = Convert.ToDateTime(s);
                var ss = n - datetime;
                if (ss.Milliseconds > 10000)
                {
                    v.Delete();
                }
            }
        }
    }




    class CommController : WebSocketController
    {
        public IUANode nodeEvents = null;
        protected override void OnMessage(MessageEventArgs e)
        {

            //base.OnMessage(e);
            //Send("optix Pong");
            if (e.IsText)
            {
                Log.Info("websocket recvice data" , e.Data);
                var dto = JsonConvert.DeserializeObject<ReciveDto>(e.Data);

                if (dto != null)
                {
                    switch (dto.Type)
                    {
                        case RecivePackageType.None:
                            break;
                        case RecivePackageType.Event:
                            if (nodeEvents != null)
                            {
                                var msg = e.Data;
                                Log.Info("godot event",msg);
                                var v = InformationModel.MakeVariable(DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_fff"), OpcUa.DataTypes.String);
                                v.Value = msg;
                                nodeEvents.Add(v);

                            }
                            break;
                        default:
                            break;

                    }
                }
            }
        }
    }


    #region Auto-generated code, do not edit!
    protected new GodotServerType Node => (GodotServerType)base.Node;
#endregion
}



namespace Godot
{


    public class ReciveDto
    {
        public string ClientId { get; set; }
        public RecivePackageType Type { get; set; }
        public string SubType { get; set; }
        public string SourceObject { get; set; }
        public object Args { get; set; }
    }
    public enum RecivePackageType
    {
        None = 0,
        Event,
    }

    public enum SendPackageType
    {
        None = 0,
        UpdateData,
    }
}
