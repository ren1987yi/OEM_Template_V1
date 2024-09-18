#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.SQLiteStore;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.Store;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Core;
#endregion
using VisualScriptOptix;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Net.Http;
using GCOptixToolkit;
using FTOptix.WebUI;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;
using System.Collections.Generic;
using System.Linq;

[CustomBehavior]
public class VisualScriptHostTypeBehavior : BaseNetBehavior
{
    VisualScriptHostOptix _host;

    NodeService _nodeService;

    public override void Start()
    {
        // Insert code to be executed when the user-defined behavior is started
        Node.Cmd_StartVariable.VariableChange += Cmd_StartVariable_VariableChange;
        Node.Cmd_StopVariable.VariableChange += Cmd_StopVariable_VariableChange;

        
        _host = new VisualScriptHostOptix(Node);
        _host.StateChanged += Host_StateChange;
        _host.Services.AddSingleton<LogWrapperService>();
        _host.Services.AddSingleton<StoreService>();




        var _nEMs = Node.GetAlias("EMs");
        var _ems = new List<IUANode>();
        if(_nEMs != null)
        {
            foreach(var e in _nEMs.Children.OfType<VisualScriptOptix_EM_ModelType>())
            {
                _ems.Add(e);
            }
        }
        var _nodeService = new NodeService();
        _nodeService.EMs = _ems;
        _host.Services.AddSingleton( _nodeService );

        var configure = new ConfigureService();
        configure.EngineModelType = Node.GetAlias("EngineModelType");
        configure.NodeModelType = Node.GetAlias("NodeModelType");

         


        _host.Services.AddSingleton(configure);
        _host.Services.AddTransient<WorkflowEngineOptix>();

        if (Node.AutoStart)
        {

            _host.Start();
        }



    }

    private void Host_StateChange(object sender, EventArgs e)
    {
        //throw new NotImplementedException();
        Node.Running = _host.Running;
    }

    private void Cmd_StopVariable_VariableChange(object sender, VariableChangeEventArgs e)
    {
        //throw new NotImplementedException();
        if(!Node.Running)
        {
            _host.Start();
        }
    }

    private void Cmd_StartVariable_VariableChange(object sender, VariableChangeEventArgs e)
    {
        //throw new NotImplementedException();
        if(Node.Running)
        {
            _host.Stop();
        }
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined behavior is stopped
        if (Node.Running)
        {
            _host.Dispose();

            _host.Stop();
            _host.StateChanged -= Host_StateChange;
        }
    }



    /// <summary>
    /// 
    /// </summary>
    /// <param name="content">ÄÚÈÝ</param>
    [ExportMethod]
    public void CreateWorkflowInstance(string content,out bool success,out string message)
    {


        var r = createWorkflowInstance(content,out message);
        success = r;
        Node.ErrorMessage = message;

    }


    [ExportMethod]
    public void StartWorkflowInstance(NodeId nodeId)
    {
        //var node = InformationModel.Get(nodeId);
        var r = _host.StartWorkflowEngine(nodeId);
        if(r.Type == VisualScript.Core.ResultType.Failure)
        {
            Node.ErrorMessage = r.ErrorMessage;
        }
        
    }

    [ExportMethod]
    public void StopWorkflowInstance(NodeId nodeId)
    {

    }

    [ExportMethod]
    public void ResetWorkflowInstance(NodeId nodeId)
    {
        var r = _host.ResetWorkflowEngine(nodeId);
        if (r.Type == VisualScript.Core.ResultType.Failure)
        {
            Node.ErrorMessage = r.ErrorMessage;
        }
    }



    private bool createWorkflowInstance(string content, out string error)
    {
        error = string.Empty;
        var wk = JsonHelper.Deserialize<WorkflowOptix>(content);
        if(wk != null) {
            var res = _host.CreateWorkflowEngine(wk);
            if(res.Type == VisualScript.Core.ResultType.Success)
            {

                return true;
            }
            else if(res.Type == VisualScript.Core.ResultType.Failure)
            {
                error = res.ErrorMessage;
                return false;
            }
            else
            {
                error = "result return error type";
                return false;
            }
        }
        else
        {
            error = "workflow data error";
            return false;
        }
    }



#region Auto-generated code, do not edit!
    protected new VisualScriptHostType Node => (VisualScriptHostType)base.Node;
#endregion
}
