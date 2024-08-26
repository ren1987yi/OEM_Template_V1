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

using System.Collections.Generic;
using GCOptixToolkit;
using Microsoft.Extensions.DependencyInjection;
using OptixWebServer.Charts;

[CustomBehavior]
public class ChartServerTypeBehavior : BaseNetBehavior
{
    ChartSeverHost host;
    string RootPath;
    UInt16 Port;
    public override void Start()
    {

        Node.TestVariable.VariableChange += TestVariable_VariableChange;

        // Insert code to be executed when the user-defined behavior is started
        var root = Node.GetVariableValue<string>("RootPath");

        var uri = ResourceUri.FromProjectRelativePath(root);
        RootPath = uri.Uri;
        RootPath = @"D:\Work\Optix\OEM_Template_V1\ProjectFiles\WebRoot\ChartsApp";
        Port = Node.GetVariableValue<UInt16>("Port");
        var cfgs = BuildConfigre();

        Log.Info("Chart Server", $"Root:{RootPath} ; Port:{Port}");

        host = new ChartSeverHost(Port, RootPath, null, cfgs);
  

        //有的时候，tcp端口被排除了
        host.Start();
    }

    private void TestVariable_VariableChange(object sender, VariableChangeEventArgs e)
    {
        Test();
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined behavior is stopped
        host.Stop();
        Node.TestVariable.VariableChange -= TestVariable_VariableChange;
    }

    [ExportMethod]
    public void Test()
    {
        var _ = BuildConfigre();
    }

    private ChartCollectionConfigure BuildConfigre()
    {
        var cfg= new ChartCollectionConfigure();

        foreach(var item in Node.Charts.Children.OfType<ChartConfigureType>())
        {
            var c = new ChartConfigure();
            c.Name = item.BrowseName;
            c.Option = item.Option;

            if (!item.IsActive)
            {

                var logicObject = item.GetByType<NetLogicObject>();

                c.funcGetData = (e) =>
                {
                    var method = item.GetDataMethod;
                    logicObject.ExecuteMethodNoPermissions(method, new object[] {e }, out var result);
                    if(result != null)
                    {
                        return result[0] as string;
                    }else
                    {
                        return string.Empty;
                    }
                };
            }
            else
            {
                c.funcGetData = (e) =>
                {
                    return item.Data;
                };
            }

          


            cfg.ChartConfigures.Add(c);
        }


        return cfg;
    }







#region Auto-generated code, do not edit!
    protected new ChartServerType Node => (ChartServerType)base.Node;
#endregion
}




namespace OptixWebServer.Charts
{
    public class ChartSeverHost
    {
        WebApplication app;
        readonly ChartCollectionConfigure chartsConfigre;
        readonly int port;
        readonly string rootpath;

        public ChartSeverHost(int port, string rootpath, string controllerPrefix, ChartCollectionConfigure configure)
        {
            this.chartsConfigre = configure;
            this.port = port;
            this.rootpath = rootpath;
            var builder = WebApplication.CreateBuilder();
            builder.Port = port;
            builder.Services.AddSingleton(chartsConfigre);

            app = builder.Build();
            app.DocumentRootPath = rootpath;
            app.MapController(controllerPrefix);

         
            app.UseStaticFiles(
                new Dictionary<string, string>()
                {
                { "Cross-Origin-Opener-Policy", "same-origin" }
                ,{"Cross-Origin-Embedder-Policy", "require-corp"}
                    //,{"Cross-Origin-Embedder-Policy", "credentialless"}
                }
            );

      


        }


        public void Start()
        {
            app.Run();
        }

        public void Stop()
        {
            app.Stop();
        }
    }



    public class ChartCollectionConfigure
    {
        private List<ChartConfigure> chartConfigures = new List<ChartConfigure>();

        public List<ChartConfigure> ChartConfigures
        {
            get { return chartConfigures; }
            set { chartConfigures = value; }
        }


    }

    public class ChartConfigure
    {
        public string Name { get; set; }
        public Func<string, string> funcGetData { get; set; }
        public string Option { get; set; }

        

    }

    public class ChartController : HttpController
    {
        readonly ChartCollectionConfigure configure;

        public ChartController(ChartCollectionConfigure cfg)
        {
            configure = cfg;
        }

        [HttpGet]
        public IResult Test()
        {

            return new Result("ok");
        }

        [HttpPost]
        public IResult GetOption()
        {
            var chartName = string.Empty;
            if (this.Request.HasEntityBody)
            {
                var txt = this.Request.GetBodyString();
                var dto = JsonConvert.DeserializeObject<RequestDto>(txt);
                Console.WriteLine($"Cmd:GetOption , Chart:{dto.Chart}");
                chartName = dto.Chart;
            }

            var cfg = configure.ChartConfigures.Where(c => c.Name == chartName).FirstOrDefault();
            if (cfg != null && cfg.Option != null)
            {


                return new Result(cfg.Option);
            }
            else
            {
                return new Result("{}");
            }
        }

        Random rnd = new Random();
        [HttpPost]
        public IResult GetData()
        {
            var chartName = string.Empty;
            var query = string.Empty;
            if (this.Request.HasEntityBody)
            {
                var txt = this.Request.GetBodyString();
                var dto = JsonConvert.DeserializeObject<RequestDto>(txt);
                Console.WriteLine($"Cmd:GetData , Chart:{dto.Chart}");

                chartName = dto.Chart;
                query = dto.Query;
            }


            var cfg = configure.ChartConfigures.Where(c => c.Name == chartName).FirstOrDefault();
            if (cfg != null && cfg.funcGetData != null)
            {

                var result = cfg.funcGetData.Invoke(query);
                var r = new Result(result);
                r.ContentType = "application/json";
                return r;
            }
            else
            {
                var r = new Result("{}");
                r.ContentType = "application/json";
                return r;
            }

        }

    }



    public class ChartDataDto
    {
        public object updateOptions { get; set; }
        public object updateSeries { get; set; }
    }

    public class RequestDto
    {
        public string Chart { get; set; }
        public string Query { get; set; }
    }
}

