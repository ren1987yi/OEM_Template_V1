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
using Godot;
using Newtonsoft.Json;
public class GodotWebBrowser_RuntimeLogic : BaseNetLogic
{
    ModelEventObserver observer;
    DialogType dialog1;
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        IUANode node = Owner.GetAlias("GodotEvents");

        dialog1 = Owner.GetAlias("Dialog1") as DialogType;

        observer = new ModelEventObserver(node, LogicObject, OnAddHandle, OnRemoveHandle);
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
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


