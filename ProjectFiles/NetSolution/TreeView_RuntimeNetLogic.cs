#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.SQLiteStore;
using FTOptix.HMIProject;
using FTOptix.NativeUI;
using FTOptix.CoreBase;
using FTOptix.WebUI;
using FTOptix.Recipe;
using FTOptix.Store;
using FTOptix.System;
using FTOptix.NetLogic;
using FTOptix.Retentivity;
using FTOptix.UI;
using FTOptix.Alarm;
using FTOptix.Core;
using OEM_Template_V1;
using System.Linq;
using System.Reflection;
using System.ComponentModel;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;
using FTOptix.EventLogger;
#endregion

public class TreeView_RuntimeNetLogic : BaseNetLogic
{
    #region Input Parameters
    IUANode nTreeNodeUIType;
    IUANode nTreeLeafUIType;
    IUANode nModel;
    bool isShowRoot;
    float indent;
    float vGap;
    string filiter;

    IUANode nContainer;
    IUAVariable nSelectedItem;
    #endregion



    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started

        nTreeNodeUIType = InformationModel.Get(Owner.GetVariableValue("TreeNode",NodeId.Empty));
        nTreeLeafUIType = InformationModel.Get(Owner.GetVariableValue("TreeLeafNode",NodeId.Empty));

        nSelectedItem = Owner.GetVariable("SelectedItem");

        nModel = Owner.GetAlias("Model");

        isShowRoot = Owner.GetVariableValue("ShowRoot", false);
        indent = Owner.GetVariableValue("Indent", 0.0f);
        vGap = Owner.GetVariableValue("VerticalGap", 0.0f);
        filiter = Owner.GetVariableValue("Filiter", string.Empty);

        if(nTreeNodeUIType == null
            || nTreeLeafUIType == null
            || nModel == null
            )
        {
            throw new Exception();
        }

        nContainer = LogicObject.GetAlias("Container");

        nContainer.ClearAll();
        if (isShowRoot)
        {

        }
        var v = (nContainer as ColumnLayout);
        //v.LeftMargin = indent;
        v.VerticalGap = vGap;

        BuildChildrenUI(this.nContainer);
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }


    #region UI Methods
    #endregion

    

    private void BuildChildrenUI(IUANode container)
    {
        
        foreach(var item in nModel.Children)
        {
            var typeName = item.GetType().Name;

            if(typeName == filiter)
            {
                // node

                var ui = InformationModel.MakeObject(item.BrowseName + "_ui", this.nTreeNodeUIType.NodeId) as Item;
                ui.SetVariableValue("TreeNode", this.nTreeNodeUIType.NodeId);
                ui.SetVariableValue("TreeLeafNode",this.nTreeLeafUIType.NodeId);
                ui.SetAlias("Model", item);
                ui.SetVariableValue("Indent", this.indent);
                ui.SetVariableValue("VerticalGap",this.vGap);
                ui.SetVariableValue("Filiter",this.filiter);
                ui.SetAlias("SelectedItem", nSelectedItem);

                container.Add(ui);

            }
            else
            {
                //leaf
                var ui = InformationModel.MakeObject(item.BrowseName + "_ui", this.nTreeLeafUIType.NodeId) as Item;

                
                ui.SetAlias("Item", item);
                ui.SetVariableValue("CurrentItem", nSelectedItem.NodeId);
                ui.SetVariableValue("IsLeaf",true);

                container.Add(ui);

            }


        }


    }
}
