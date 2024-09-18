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
using GCOptixToolkit;
#endregion

public class VisualScript_Dlg_EM_Configure_RuntimeNetLogic : BaseNetLogic
{
    ComboBox cbEM;

    VisualScriptOptixNodeType nModel;
    IUANode nEMs;

    IUAVariable vSelectedName;
    IUANode nSelectedEM;
    IUANode nInputFloats;
    IUANode nInputBools;
    IUANode nOutputFloats;
    IUANode nOutputBools;

    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started

        cbEM = LogicObject.GetAlias("Combox") as ComboBox;


        nModel = Owner.GetAlias("NodeModel") as VisualScriptOptixNodeType;

        nEMs = Owner.GetAlias("EMs");
        nSelectedEM = LogicObject.GetObject("EM");
        vSelectedName = nSelectedEM.GetVariable("Name");
        nInputFloats = nSelectedEM.GetObject("InputFloats");
        nInputBools = nSelectedEM.GetObject("InputBools");
        nOutputFloats = nSelectedEM.GetObject("OutputFloats");
        nOutputBools = nSelectedEM.GetObject("OutputBools");
        loadConfigure();
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }


    string currentEMName;
    
    private void loadConfigure()
    {
        var emName = nModel.Parameters.GetVariableValue<string>("EMName");
        currentEMName = emName ?? string.Empty;
        vSelectedName.Value = currentEMName;

        var mapper = nModel.Parameters.GetVariableValue<string>("Mapper");
       
        


    }



    [ExportMethod]
    public void EM_Change(NodeId node)
    {


        var n = InformationModel.Get(node) as VisualScriptOptix_EM_ModelType;
        if (n != null)
        {
            if(n.Name == currentEMName)
            {

            }
            else
            {
                clearMapper();
                buildMapper(n);

                currentEMName = n.Name;
            }
        }
        else
        {

            clearMapper();
            
        }
    }

    private void clearMapper()
    {


        currentEMName = string.Empty;
        foreach (var item in nInputFloats.Children)
        {
            item.Delete();
        }

        foreach (var item in nInputBools.Children)
        {
            item.Delete();
        }

        foreach (var item in nOutputFloats.Children)
        {
            item.Delete();
        }

        foreach (var item in nOutputBools.Children)
        {
            item.Delete();
        }
    }


    private void buildMapper(VisualScriptOptix_EM_ModelType em)
    {
        foreach(var s in em.InputFloats.Children)
        {
            var name = s.GetVariableValue<string>("Name");
            var m = InformationModel.MakeObject<VisualScriptOptix_InterfaceMapperType>(s.BrowseName);
            m.Source = string.Empty;
            m.Target = name;
            nInputFloats.Add(m);
        }


        foreach (var s in em.InputBools.Children)
        {
            var name = s.GetVariableValue<string>("Name");
            var m = InformationModel.MakeObject<VisualScriptOptix_InterfaceMapperType>(s.BrowseName);
            m.Source = string.Empty;
            m.Target = name;
            nInputBools.Add(m);
        }


        foreach (var s in em.OutputFloats.Children)
        {
            var name = s.GetVariableValue<string>("Name");
            var m = InformationModel.MakeObject<VisualScriptOptix_InterfaceMapperType>(s.BrowseName);
            m.Source = name;
            m.Target = string.Empty;
            nOutputFloats.Add(m);
        }


        foreach (var s in em.OutputBools.Children)
        {
            var name = s.GetVariableValue<string>("Name");
            var m = InformationModel.MakeObject<VisualScriptOptix_InterfaceMapperType>(s.BrowseName);
            m.Source = name;
            m.Target = string.Empty;
            nOutputBools.Add(m);
        }



    }

}
