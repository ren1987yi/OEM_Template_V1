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

using System.Collections.Generic;
using GCOptixToolkit;
using Microsoft.Extensions.DependencyInjection;
using OptixWebServer.Charts;
using CSScriptLib;
using DotNetWebServer;
using System.Net;

[CustomBehavior]
public class ChartServerTypeBehavior : BaseNetBehavior
{
    ChartSeverHost host;
    string RootPath;
    UInt16 Port;
    ChartCollectionConfigure cfgs;
    IUAVariable vError;
    public override void Start()
    {
        if (!Node.Enable)
        {
            return;
        }


        vError = Node.GetVariable("Error");
        try
        {
            var _longtask = new LongRunningTask(() => { 
                Node.TestVariable.VariableChange += TestVariable_VariableChange;

                // Insert code to be executed when the user-defined behavior is started
                var root = Node.GetVariableValue<string>("RootPath");

                var uri = ResourceUri.FromProjectRelativePath(root);
                RootPath = uri.Uri;
                //RootPath = @"D:\Work\Optix\OEM_Template_V1\ProjectFiles\WebRoot\ChartsApp";
                Port = Node.GetVariableValue<UInt16>("Port");
                cfgs = BuildConfigre();

                Log.Info("Chart Server", $"Root:{RootPath} ; Port:{Port}");


            
                host = new ChartSeverHost(Port, RootPath, "", cfgs);


                //有的时候，tcp端口被排除了
                host.Start();
            }, Node);
            _longtask.Start();
        }
        catch (Exception ex)
        {
            vError.Value = ex.Message;
        }
    
    }

  

    public override void Stop()
    {
        // Insert code to be executed when the user-defined behavior is stopped
        if(host != null)
        {

            host.Stop();
            Node.TestVariable.VariableChange -= TestVariable_VariableChange;
        }
    }

    [ExportMethod]
    public void Test()
    {

        cfgs = BuildConfigre();
    }


    private void TestVariable_VariableChange(object sender, VariableChangeEventArgs e)
    {
        if (host != null)
        {

            host.Stop();
        }




        // Insert code to be executed when the user-defined behavior is started
        var root = Node.GetVariableValue<string>("RootPath");

        var uri = ResourceUri.FromProjectRelativePath(root);
        RootPath = uri.Uri;
        //RootPath = @"D:\Work\Optix\OEM_Template_V1\ProjectFiles\WebRoot\ChartsApp";
        Port = Node.GetVariableValue<UInt16>("Port");
        cfgs = BuildConfigre();

        Log.Info("Chart Server", $"Root:{RootPath} ; Port:{Port}");

        host = new ChartSeverHost(Port, RootPath, null, cfgs);


        //有的时候，tcp端口被排除了
        host.Start();
    }


    private ChartCollectionConfigure BuildConfigre()
    {
        var cfg= new ChartCollectionConfigure();

        foreach(var item in Node.Charts.Children.OfType<ChartConfigureType>())
        {
            var c = new ChartConfigure();
            c.Name = item.BrowseName;
            c.GetOption = () => { return item.OptionVariable.Value; };

           

            if(item.ScriptEnable)
            {
                var scriptpath = item.ScriptPath.Uri;
                Log.Info("chart script path:",scriptpath);
                if (System.IO.File.Exists(scriptpath))
                {
                    item.ActualScriptPath = scriptpath;
                    
                    var code = System.IO.File.ReadAllText(scriptpath);
                    item.ActualScript = code;
                    var s = ScriptHelper.Builder(code);
                    c.scriptExecuter = s;
                    c.ScriptPath = scriptpath;
                    if (s != null)
                    {
                        c.funcGetData = (e) =>
                        {
                            return c.scriptExecuter.Exec(item,e);
                        };
                    }

                    item.SaveTriggerVariable.VariableChange += (s,e) => { 
                        SaveScript(item.ActualScriptPath,item.ActualScript);
                        ReloadScript(c, item);
                    };

                }
            }
            else
            {
                if (!item.IsActive)
                {

                    var logicObject = item.GetByType<NetLogicObject>();

                    c.funcGetData = (e) =>
                    {
                        var method = item.GetDataMethod;
                        logicObject.ExecuteMethodNoPermissions(method, new object[] { e }, out var result);
                        if (result != null)
                        {
                            return result[0] as string;
                        }
                        else
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
            }

            cfg.ChartConfigures.Add(c);
        }

        
        return cfg;
    }

    private void SaveScript(string path,string code)
    {
        //throw new NotImplementedException();
        System.IO.File.WriteAllText(path, code);
    }

    private void ReloadScript(ChartConfigure cfg, ChartConfigureType item)
    {
        //throw new NotImplementedException();
        var scriptpath = cfg.ScriptPath;
        var code = System.IO.File.ReadAllText(scriptpath);
        item.ActualScript = code;
        var s = ScriptHelper.Builder(code);
        cfg.scriptExecuter = s;

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
            
            
            app = new WebApplication(IPAddress.Any, port, "", rootpath);
            app.UseStaticFile();
            app.Services.AddSingleton(chartsConfigre);
            
            app.MapController(new string[] { "OptixWebServer.Charts" });

         
          

      


        }


        public void Start()
        {
            app.Start();
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
        public Func<string> GetOption { get; set; }

        public IScriptExecuter scriptExecuter { get; set; }
        public string ScriptPath { get; set; }
    }

    public class ChartController : HttpController
    {
        readonly ChartCollectionConfigure configure;

        public ChartController(ChartCollectionConfigure cfg)
        {
            configure = cfg;
        }

        [HttpMethod(HttpMethodType.GET)]
        public IResult Test()
        {

            return new TextResult("ok");
        }

        [HttpMethod(HttpMethodType.POST)]
        public IResult GetOption()
        {
            var chartName = string.Empty;
            if (!string.IsNullOrWhiteSpace(this.Request.Body))
            {

                var txt = this.Request.Body;
                var dto = JsonConvert.DeserializeObject<RequestDto>(txt);
                Console.WriteLine($"Cmd:GetOption , Chart:{dto.Chart}");
                chartName = dto.Chart;
            }

            var cfg = configure.ChartConfigures.Where(c => c.Name == chartName).FirstOrDefault();
            if (cfg != null && cfg.GetOption() != null)
            {


                return new TextResult(cfg.GetOption());
            }
            else
            {
                return new TextResult("{}");
            }
        }

        Random rnd = new Random();
        [HttpMethod(HttpMethodType.POST)]
        public IResult GetData()
        {
            var chartName = string.Empty;
            var query = string.Empty;
            if (!string.IsNullOrWhiteSpace(this.Request.Body))
            {
                var txt = this.Request.Body;
                var dto = JsonConvert.DeserializeObject<RequestDto>(txt);
                Console.WriteLine($"Cmd:GetData , Chart:{dto.Chart}");

                chartName = dto.Chart;
                query = dto.Query;
            }


            var cfg = configure.ChartConfigures.Where(c => c.Name == chartName).FirstOrDefault();
            if (cfg != null && cfg.funcGetData != null)
            {

                var result = cfg.funcGetData.Invoke(query);
                var r = new JsonResult(result);

                return r;
            }
            else
            {
                var a = new { };
                var r = new JsonResult(a);
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




    public interface IScriptExecuter
    {
        string Exec(IUANode Owner,string query);
    }

    public class ScriptHelper
    {
        
        public static IScriptExecuter Builder(string code)
        {
            try
            {

                var _code = BuildCode(code);
                Log.Info("Chart Script", _code);
                var script = CSScript.Evaluator
                 .ReferenceAssemblyByName("System")
                 .ReferenceAssemblyByNamespace("ExternalScript")
                 .ReferenceAssemblyByNamespace("UAManagedCore")
                 .ReferenceAssemblyByNamespace("FTOptix.UI")
                 .ReferenceAssemblyByNamespace("Newtonsoft.Json")
                 .ReferenceAssemblyByNamespace("System.Collections.Generic;")
                 .ReferenceAssemblyByNamespace("OptixWebServer.Charts")
                 .ReferenceDomainAssemblies()
                            .LoadCode<OptixWebServer.Charts.IScriptExecuter>(_code);
                return script;
            }catch (Exception ex)
            {
                Log.Info("chart script error:",ex.Message);
                return null;
            }
        }

        private static string BuildCode(string code)
        {

            var r = $@"using System;
using ExternalScript;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using Newtonsoft.Json;
using System.Collections.Generic;
using OptixWebServer.Charts;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;
using FTOptix.EventLogger;
public class Script : OptixWebServer.Charts.IScriptExecuter
{{
    public string Exec(IUANode Owner,string query)
    {{
        {code}
    }}
}}";

            return r;
        }

    }

}

