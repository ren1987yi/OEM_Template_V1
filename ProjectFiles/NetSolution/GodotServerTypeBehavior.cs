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



using Newtonsoft.Json;

using System.Linq;
using System.Threading.Tasks;
using GodotServerHost;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using NetCoreServer;
using System.Numerics;
using FTOptix.OPCUAClient;
using System.Reflection;
using System.Xml.Linq;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;
using FTOptix.EventLogger;

[CustomBehavior]
public class GodotServerTypeBehavior : BaseNetBehavior
{
    GodotServer _wsServer; // godot server
                           //HttpServer2 server = null;
                           //WebSocketServiceHost wsHost = null;

    PeriodicTask _task_push_data;//push the realtime data to godot
    PeriodicTask _task_clean;//clean the old events

    IUANode nodeData = null;
    IUANode nodeEvents = null;
    IUAVariable vError;

    NodeValueJsonConvert _nodeConvert; // convert the node data to json format
    public override void Start()
    {

        if (!Node.Enable)
        {
            return;
        }
        var v = Node.GetVariable("Error");
        vError = v;

        if (vError == null)
        {
            throw new NullReferenceException("No Error Variable");
        }


        try
        {
            nodeEvents = Node.GetObject("Events");

            var _rootpath = string.Empty;
            v = Node.GetVariable("RootPath");
            if (v != null)
            {
                _rootpath = v.Value;
            }
            var rUri = ResourceUri.FromProjectRelativePath(_rootpath);

            UInt16 _port = 8080;
            v = Node.GetVariable("Port");
            if (v != null)
            {
                _port = v.Value;
            }
            _wsServer = GodotServer.Builder(rUri.Uri, _port, string.Empty, () =>
            {
                return new GodotSession(_wsServer, nodeEvents);
            });


            nodeData = Node.GetObject("UpdateNodes");
            _nodeConvert = BuildDataPool(nodeData);

            _wsServer.Start();



            _task_push_data = new PeriodicTask(() => { UpdataStatus(); }, 100, Node);
            _task_push_data.Start();

            _task_clean = new PeriodicTask(() => { ClearEvents(); }, 5000, Node);
            _task_clean.Start();

        }
        catch(Exception e)
        {
            vError.Value = e.Message;
        }


    }









    public override void Stop()
    {
        // Insert code to be executed when the user-defined behavior is stopped
        if (_wsServer != null)
        {
            _wsServer.Stop();
        }


        if (_task_push_data != null)
        {
            _task_push_data.Cancel();
            _task_push_data.Dispose();
            _task_push_data = null;
        }


        if (_task_clean != null)
        {
            _task_clean.Cancel();
            _task_clean.Dispose();
            _task_clean = null;
        }
    }



    [ExportMethod]
    public void ChangeCamIndex(string viewid,int camidx)
    {

        if (_wsServer != null)
        {
            Log.Info("Godot Event", $"SendCameraIndex id={viewid} index={camidx}");
            _wsServer.SendPackage(SendPackageType.CameraCommand, new Package_CameraChange() { CamIndex = camidx }, $"{viewid}");
        }
    }



    [ExportMethod]
    public void ChangeSceneName(string viewid, string name)
    {
        if (_wsServer != null)
        {
            Log.Info("Godot Event", $"SendScene id={viewid} name={name}");
            _wsServer.SendPackage(SendPackageType.ChangeScene, new Package_SceneChange() { SceneName = name }, $"{viewid}");
            _wsServer.SendPackage(SendPackageType.ChangeScene, new Package_SceneChange() { SceneName = name }, "*");
        }
    }


    /// <summary>
    /// build the realtime update data pool
    /// </summary>
    /// <param name="root"></param>
    /// <returns></returns>
    private NodeValueJsonConvert BuildDataPool(IUANode root)
    {
        var c = new NodeValueJsonConvert(root);
        return c;


    }
    /// <summary>
    /// build the update data package and send to all godot client
    /// </summary>
    private void UpdataStatus()
    {
        if (this._wsServer != null)
        {

            var j = $@"
{{
""Type"":{(int)SendPackageType.UpdateData},
""Target"":""*"",
""Data"":{_nodeConvert.Serialize()}
}}
";

            //Log.Info("updata status", j);

            this._wsServer.MulticastText(j);
        }
    }
    private void ClearEvents()
    {
        var n = Node.GetObject("Events");
        var nowticks = DateTime.Now.Ticks;
        foreach (var item in n.Children)
        {
            var t = Convert.ToInt64(item.BrowseName);
            if (nowticks - t > 100000000)
            {

                item.Delete();
            }
        }
    }



    /// <summary>
    /// 根据节点的结构，序列化为json,key是节点的路径
    /// </summary>
    class NodeValueJsonConvert
    {
        List<Placeholder> placeholders = new List<Placeholder>();



        string format = string.Empty;

        public NodeValueJsonConvert(IUANode root)
        {
            BuildStringFormat(root);
        }

        private void BuildStringFormat(IUANode root)
        {
            placeholders.Clear();
            EnumNode(root, string.Empty);

            var pps = placeholders.Select(p => @$"""{p.Name}"":{{{p.Index}}}");
            format = "{{" + string.Join(",", pps) + "}}";

            Log.Info(format);
        }

        private string PathCombin(string perfix, string value)
        {
            if (string.IsNullOrWhiteSpace(perfix))
            {
                return value;
            }
            else
            {
                return perfix + "/" + value;
            }
        }
        private void EnumNode(IUANode root, string perfix = "")
        {
            foreach (var c in root.Children)
            {
                if (c is Alias)
                {
                    var _node = root.GetAlias(c.BrowseName);
                    if (_node != null)
                    {
                        EnumNode(_node, PathCombin(perfix, c.BrowseName));
                    }
                    else
                    {
                        continue;
                    }
                }
                else if (c is IUAVariable)
                {


                    var vv = (IUAVariable)c;
                    if (vv.DataType == OpcUa.DataTypes.Boolean
                        || vv.DataType == OpcUa.DataTypes.Int16
                        || vv.DataType == OpcUa.DataTypes.Int32
                        || vv.DataType == OpcUa.DataTypes.UInt16
                        || vv.DataType == OpcUa.DataTypes.UInt32
                        || vv.DataType == OpcUa.DataTypes.Float
                        || vv.DataType == OpcUa.DataTypes.Double
                        || vv.DataType == OpcUa.DataTypes.Byte
                        || vv.DataType == OpcUa.DataTypes.SByte
                        )
                    {


                        var p = new Placeholder()
                        {
                            Name = PathCombin(perfix, c.BrowseName),
                            Index = placeholders.Count,
                            uVar = c as IUAVariable
                        };
                        placeholders.Add(p);
                    }
                }
                else if (c is IUANode)
                {
                    if (c != null)
                    {
                        EnumNode(c, PathCombin(perfix, c.BrowseName));
                    }
                }
                else if (c is IUAObject)
                {
                    if (c != null)
                    {
                        EnumNode(c, PathCombin(perfix, c.BrowseName));
                    }
                }
                else
                {
                    throw new Exception($"{c.BrowseName}'s type out of support,type is {c.GetType().FullName}");
                }
            }
        }

        public string Serialize()
        {


            var vals = placeholders.Select(c => c.uVar.Value.Value).ToArray();
            return string.Format(format, vals);

        }



        struct Placeholder
        {
            public string Name { get; set; }
            public int Index { get; set; }
            public IUAVariable uVar { get; set; }
        }
    }




    /// <summary>
    /// this is the implement websocket handler for godot
    /// </summary>
    class GodotSession : WsSession
    {
        readonly IUANode nEvents;
        public GodotSession(WsServer server, IUANode nevents) : base(server)
        {
            this.nEvents = nevents;
        }

        public override void OnWsConnected(HttpRequest request)
        {


            Console.WriteLine($"Chat WebSocket session with Id {Id} connected!");

            // Send invite message
            string message = "Hello from WebSocket chat! Please send a message or '!' to disconnect the client!";
            SendTextAsync(message);
        }

        public override void OnWsDisconnected()
        {
            Console.WriteLine($"Chat WebSocket session with Id {Id} disconnected!");
        }

        public override void OnWsReceived(byte[] buffer, long offset, long size)
        {
            string message = System.Text.Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            Console.WriteLine("Incoming: " + message);
            Log.Info("Incoming: " + message);

            if (nEvents != null)
            {
                var v = InformationModel.MakeVariable(DateTime.Now.Ticks.ToString(), OpcUa.DataTypes.String);
                v.Value = message;
                nEvents.Add(v);
            }

            // Multicast message to all connected sessions
            //((WsServer)Server).MulticastText(message);

            // If the buffer starts with '!' the disconnect the current session
            //if (message == "!")
            //    Close(0);
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat WebSocket session caught an error with code {error}");
        }
    }



    #region Auto-generated code, do not edit!
    protected new GodotServerType Node => (GodotServerType)base.Node;
#endregion
}




namespace GodotServerHost
{
    public class ReciveDto
    {
        public string ClientId { get; set; }
        public RecivePackageType Type { get; set; }
        public string SubType { get; set; }
        public string SourceObject { get; set; }
        public string[] Args { get; set; }
    }
    public enum RecivePackageType
    {
        None = 0,
        Event,
    }

    public enum SendPackageType
    {
        None = 0,
        UpdateData = 1,
        CameraCommand = 2,
        ChangeScene = 3,
        TrackPos = 4
    }


    public class GodotServer : WsServer
    {
        readonly Func<WsSession> initSession;
        public GodotServer(IPAddress address, int port, Func<WsSession> fInit) : base(address, port)
        {
            initSession = fInit;
        }

        protected override TcpSession CreateSession()
        {
            //return new GodotSession(this); 
            if (initSession != null)
            {

                return initSession.Invoke();
            }
            else
            {
                throw new NotImplementedException();
            }

        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat WebSocket server caught an error with code {error}");
        }


        public static GodotServer Builder(string root, int port, string router, Func<WsSession> fInit)
        {
            var server = new GodotServer(IPAddress.Any, port, fInit);

            server.AddStaticContent(root, "","*.*",null,null);
            return server;
        }

        public void SendPackage(SendPackageType type, object data, string target = "*")
        {
            var d = new
            {
                Type = type,
                Target = target,
                Data = data
            };
            var j = JsonConvert.SerializeObject(d);
            this.MulticastText(j);
        }

    }


    public class Package_CameraChange
    {
        public int CamIndex { get; set; }

    }


    public class Package_SceneChange
    {
        public string SceneName { get; set; }
    }

}

