#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.Retentivity;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.CoreBase;
using FTOptix.Core;
using FTOptix.WebUI;
#endregion
using GCOptixToolkit;
using GodotServerHost;
using Newtonsoft.Json;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;
using FTOptix.EventLogger;
public class GodotWebBrowser_RuntimeLogic : BaseNetLogic
{
    ModelEventObserver observer;
    WebBrowser browser;
    string RootUrl;
    //DialogType dialog1;






    IUAVariable vEventType, vEventSubType, vEventSourceObject, vEventArgs0, vEventArgs1, vEventArgs2, vEventArgs3, vNewEventTrigger;
    IUANode nGodotEvents;



    IUAVariable vCamIndex,vSceneName;

    IUAObject nGodotServer;


    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        IUANode node = Owner.GetAlias("GodotEvents");
        nGodotEvents = node;
        RootUrl = Owner.GetVariableValue<string>("Root");
        vNewEventTrigger = Owner.GetVariable("NewEventTrigger");
        vEventType = Owner.GetVariable("EventType");
        vEventSubType = Owner.GetVariable("EventSubType");
        vEventSourceObject = Owner.GetVariable("EventSourceObject");
        vEventArgs0 = Owner.GetVariable("EventArgs0");
        vEventArgs1 = Owner.GetVariable("EventArgs1");
        vEventArgs2 = Owner.GetVariable("EventArgs2");
        vEventArgs3 = Owner.GetVariable("EventArgs3");


        vCamIndex = Owner.GetVariable("CamIndex");
        if(vCamIndex != null)
        {
            vCamIndex.VariableChange += CamIndex_VariableChange;
        }

        vSceneName = Owner.GetVariable("SceneName");
        if (vSceneName != null)
        {
            vSceneName.VariableChange += SceneName_VariableChange;
        }


        nGodotServer = InformationModel.Get<GodotServerType>(Owner.GetVariableValue<NodeId>("GodotServer"));
        if(nGodotServer == null)
        {
            throw new Exception();
        }

        //dialog1 = Owner.GetAlias("Dialog1") as DialogType;

        observer = new ModelEventObserver(node, LogicObject, OnAddHandle, OnRemoveHandle);
        browser = Owner as WebBrowser;
        if(browser == null)
        {
            throw new Exception();
        }

        var wshost = string.Empty;
        var uri = new Uri(RootUrl);
        var bg = Owner.GetVariableValue<string>("Background");
        var scene  = Owner.GetVariableValue<string>("SceneName");
        wshost = $"{uri.Host}:{uri.Port}/comm";

        var url = RootUrl + $"?id={Owner.NodeId}&ws={wshost}&bg={bg}&scene={scene}";
        browser.URL = ResourceUri.FromUri(url);
        
        Log.Info("GodotViewer", $"URL:{url}");

    }

    private void CamIndex_VariableChange(object sender, VariableChangeEventArgs e)
    {
        //throw new NotImplementedException();
        nGodotServer.ExecuteMethod("ChangeCamIndex", new object[] { Owner.NodeId.ToString(), (int)e.NewValue});

    }

    private void SceneName_VariableChange(object sender, VariableChangeEventArgs e)
    {
        //throw new NotImplementedException();
        nGodotServer.ExecuteMethod("ChangeSceneName", new object[] { Owner.NodeId.ToString(), (string)e.NewValue });

    }


    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped

        if(vCamIndex != null)
        {
            vCamIndex.VariableChange -= CamIndex_VariableChange;
        }

        if (vSceneName != null)
        {
            vSceneName.VariableChange -= SceneName_VariableChange;
        }

        observer.Dispose();
        observer = null;
    }

    private void OnAddHandle(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId)
    {

        if(targetNode is IUAVariable)
        {
            lock (sourceNode)
            {

                var v = targetNode as IUAVariable;
                var jsonContent = v.Value;
                var recv = JsonConvert.DeserializeObject<ReciveDto>(jsonContent);
                if(recv != null)
                {
                    if (recv.ClientId == Owner.NodeId.ToString())
                    {
            
                        Log.Info(jsonContent);
                        //UICommands.OpenDialog(Owner, dialog1);
                        sourceNode.Remove(targetNode);
                        targetNode.Delete();

                        vEventType.Value = (int)recv.Type;
                        vEventSubType.Value = recv.SubType;
                        vEventSourceObject.Value = recv.SourceObject;
                        vEventArgs0.Value = recv.Args.Length > 0 ? recv.Args[0] : string.Empty;

                        vNewEventTrigger.Value = !((bool)vNewEventTrigger.Value);
                        //TODO 需要把选中的东西路径传到 Owner里 通知别人

                    }
                    else
                    {
                        //Log.Info(jsonContent);
                        ////UICommands.OpenDialog(Owner, dialog1);
                        //sourceNode.Remove(targetNode);
                        //targetNode.Delete();
                    }
                    
                }
            }
        }
    }
    private void OnRemoveHandle(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId)
    {

    }
}


