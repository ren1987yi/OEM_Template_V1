#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.Retentivity;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.CoreBase;
using FTOptix.Core;
using FTOptix.NetLogic;
using FTOptix.DataLogger;
using FTOptix.Store;
using FTOptix.SQLiteStore;
using FTOptix.Alarm;
using System.Linq;
using FTOptix.WebUI;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;
using VisualScriptOptix;
#endregion

public class VisualScriptOptix_ParameterPanel_RuntimeNetLogic : BaseNetLogic
{

    IUAVariable vTrigger;
    IUAVariable vModel;
    IUANode nContainer;
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started

        nContainer = LogicObject.GetAlias("Container");

        vTrigger = Owner.GetVariable("RefreshTrigger");

        vModel = Owner.GetVariable("Parameters");

        if (vTrigger != null)
        {
            vTrigger.VariableChange += Trigger_VariableChange;
        }
    }

    private void Trigger_VariableChange(object sender, VariableChangeEventArgs e)
    {
        foreach(var item in nContainer.Children)
        {
            item.Delete();
        }

        var nodeId = (NodeId)vModel.Value;
        var nodeModel = InformationModel.GetObject(nodeId) as VisualScriptOptixNodeType;

        

        if(nodeModel != null)
        {

            //if(nodeModel.TypeName == typeof(CallEMNode).FullName){

            //}
            //else
            //{
            //}
            var m = nodeModel.Parameters;
            foreach(var v in m.Children.OfType<IUAVariable>())
            {
                var w = buildWidget(v);
            
                nContainer.Add(w);
            }
        }


    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
        if(vTrigger != null)
        {

            vTrigger.VariableChange -= Trigger_VariableChange;
        }
    }



    private Item buildWidget(IUAVariable v)
    {
        var p = InformationModel.MakeObject<ColumnLayout>(v.BrowseName);
        p.HorizontalAlignment = HorizontalAlignment.Stretch;
        p.VerticalAlignment = VerticalAlignment.Top;
        p.Height = -1;
        p.VerticalGap = 8;

        var l = InformationModel.MakeObject<Label>("label");
        l.Text = v.BrowseName;
        p.Add(l);

        Item w = null;
        if(v.DataType == OpcUa.DataTypes.Boolean)
        {
            var c = InformationModel.MakeObject<CheckBox>("checkbox");
            c.HorizontalAlignment = HorizontalAlignment.Left;
            c.VerticalAlignment = VerticalAlignment.Top;
            c.Width = -1;
            c.Height = -1;
            c.CheckedVariable.SetDynamicLink(v, DynamicLinkMode.ReadWrite);
            w = c;
            
        }
        else if (v.DataType == OpcUa.DataTypes.Float)
        {
            var c = InformationModel.MakeObject<SpinBox>("txtbox");
            c.HorizontalAlignment = HorizontalAlignment.Left;
            c.VerticalAlignment = VerticalAlignment.Top;
            c.Width = 80;
            c.Height = -1;
            c.ValueVariable.SetDynamicLink(v, DynamicLinkMode.ReadWrite);
            w = c;

        }
        else if (v.DataType == OpcUa.DataTypes.String)
        {
            var c = InformationModel.MakeObject<TextBox>("txtbox");
            c.HorizontalAlignment = HorizontalAlignment.Left;
            c.VerticalAlignment = VerticalAlignment.Top;
            c.Width = 200;
            c.Height = -1;
            c.TextVariable.SetDynamicLink(v,DynamicLinkMode.ReadWrite);
            w = c;

        }

        if (w != null)
        {
            w.HorizontalAlignment = HorizontalAlignment.Stretch;
            p.Add(w);
        }


        return p;


    }

}
