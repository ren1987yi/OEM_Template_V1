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
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;
using FTOptix.EventLogger;
#endregion

public class TreeNode_Accordion_RuntimeNetLogic : BaseNetLogic
{
    #region Input Parameters
    IUANode nTreeNodeUIType;
    IUANode nTreeLeafUIType;
    IUANode nModel;

    float indent;
    float vGap;
    string filiter;

    IUANode nContainer;
    IUANode nHeadContainer;

    IUANode UIOwner;
    IUANode nSelectedItem;
    #endregion

    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started

        UIOwner = LogicObject.GetAlias("Host");

        nTreeNodeUIType = InformationModel.Get(UIOwner.GetVariableValue("TreeNode", NodeId.Empty));
        nTreeLeafUIType = InformationModel.Get(UIOwner.GetVariableValue("TreeLeafNode", NodeId.Empty));
        nSelectedItem = UIOwner.GetAlias("SelectedItem");
        nModel = UIOwner.GetAlias("Model");


        indent = UIOwner.GetVariableValue("Indent", 0.0f);
        vGap = UIOwner.GetVariableValue("VerticalGap", 0.0f);
        filiter = UIOwner.GetVariableValue("Filiter", string.Empty);

        if (nTreeNodeUIType == null
            || nTreeLeafUIType == null
            || nModel == null
            )
        {
            throw new Exception();
        }

        nContainer = LogicObject.GetAlias("Container");
        nHeadContainer = LogicObject.GetAlias("HeaderContainer");
        nContainer.ClearAll();
        //nHeadContainer.ClearAll();


        //BuildHeadUI(nHeadContainer);

        var v = (nContainer as ColumnLayout);
        //v.LeftMargin = indent;
        v.VerticalGap = vGap;
        BuildChildrenUI(this.nContainer);
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    private void BuildHeadUI(IUANode container)
    {
        var ui = InformationModel.MakeObject(nModel.BrowseName + "_ui", this.nTreeLeafUIType.NodeId) as Item;
        ui.SetAlias("Item", nModel);
        ui.SetVariableValue("CurrentItem", nSelectedItem.NodeId);
        ui.HorizontalAlignment = HorizontalAlignment.Stretch;
        ui.LeftMargin = 0;
        container.Add(ui);
    }


    private void BuildChildrenUI(IUANode container)
    {

        foreach (var item in nModel.Children)
        {
            var typeName = item.GetType().Name;

            if (typeName == filiter)
            {
                // node

                var ui = InformationModel.MakeObject(item.BrowseName + "_ui", this.nTreeNodeUIType.NodeId) as Item;
                ui.SetVariableValue("TreeNode", this.nTreeNodeUIType.NodeId);
                ui.SetVariableValue("TreeLeafNode", this.nTreeLeafUIType.NodeId);
                ui.SetAlias("Model", item);
                ui.SetVariableValue("Indent", this.indent);
                ui.SetVariableValue("VerticalGap", this.vGap);
                ui.SetVariableValue("Filiter", this.filiter);
                ui.SetAlias("SelectedItem", nSelectedItem);

                container.Add(ui);

            }
            else
            {
                //leaf
                var ui = InformationModel.MakeObject(item.BrowseName + "_ui", this.nTreeLeafUIType.NodeId) as Item;
                ui.SetAlias("Item", item);
                ui.SetVariableValue("CurrentItem", nSelectedItem.NodeId);
                ui.SetVariableValue("IsLeaf", true);
                //ui.LeftMargin = indent;
                container.Add(ui);
            }


        }


    }
}
