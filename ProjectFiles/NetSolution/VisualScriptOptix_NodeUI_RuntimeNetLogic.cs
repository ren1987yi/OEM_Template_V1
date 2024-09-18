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
using FTOptix.RAEtherNetIP;
using FTOptix.System;
using FTOptix.Retentivity;
using FTOptix.UI;
using FTOptix.Alarm;
using FTOptix.CommunicationDriver;
using FTOptix.EventLogger;
using FTOptix.Core;
using System.Linq;
#endregion

public class VisualScriptOptix_NodeUI_RuntimeNetLogic : BaseNetLogic
{
    IUANode nPortContainer;

    IUANode nNodeModel;
    IUANode nParent;

    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        nNodeModel = Owner.GetAlias("NodeModel");
        nParent = Owner.GetAlias("Parent");
        nPortContainer = LogicObject.GetAlias("PortContainer");

        buildUI();
    }


    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }



    private void buildUI()
    {
        buildPorts();
    }


    private void buildPorts()
    {
        var n = nNodeModel as VisualScriptOptixNodeType;

        if (n != null)
        {
            var slotCount = Math.Max(n.InputPorts.Children.Count, n.OutputPorts.Children.Count);

            for (int i = 0; i < slotCount; i++)
            {
                IUAVariable inpPort = null;
                IUAVariable oupPort = null;
                if (i < n.InputPorts.Children.Count)
                {
                    inpPort = n.InputPorts.Children[i] as IUAVariable;
                }

                if (i < n.OutputPorts.Children.Count)
                {
                    oupPort = n.OutputPorts.Children[i] as IUAVariable;
                }


                var is_reverse = false;

                if (n.TypeName == typeof(VisualScriptOptix.ReverseNode).FullName)
                {
                    is_reverse = true;
                }

                var slotUI = buildPort(i, inpPort, oupPort, is_reverse);
                nPortContainer.Add(slotUI);
            }


            foreach (var v in n.Parameters.Children.OfType<IUAVariable>())
            {
                var ui = buildParameter(v);
                nPortContainer.Add(ui);
            }





            nPortContainer.Add(buildPlaceholder());
        }
    }

    private IUANode buildPort(int slotindex, IUAVariable inpPort, IUAVariable oupPort, bool is_reverse)
    {

        var hl = InformationModel.MakeObject<RowLayout>(slotindex.ToString());
        hl.HorizontalAlignment = HorizontalAlignment.Stretch;
        hl.VerticalAlignment = VerticalAlignment.Top;
        hl.Height = -1;


        if (!is_reverse)
        {
            if (inpPort != null)
            {

                var inputUI = InformationModel.MakeObject<VisualScriptOptix_InpPort>("in");
                inputUI.HorizontalAlignment = HorizontalAlignment.Stretch;
                inputUI.SetAlias("Port", inpPort);
                inputUI.SetAlias("Parent", nParent);
                inputUI.SetAlias("NodeUI", Owner);
                var v = inputUI.GetVariable("Index");
                if (v != null)
                {
                    v.Value = slotindex;
                }
                hl.Add(inputUI);
            }

            if (oupPort != null)
            {
                var outputUI = InformationModel.MakeObject<VisualScriptOptix_OupPort>("out");
                outputUI.HorizontalAlignment = HorizontalAlignment.Stretch;
                outputUI.SetAlias("Port", oupPort);
                outputUI.SetAlias("Parent", nParent);
                outputUI.SetAlias("NodeUI", Owner);
                var v = outputUI.GetVariable("Index");
                if (v != null)
                {
                    v.Value = slotindex;
                }
                hl.Add(outputUI);
            }
        }
        else
        {


            if (oupPort != null)
            {
                var outputUI = InformationModel.MakeObject<VisualScriptOptix_OupPort_Reverse>("out");
                outputUI.HorizontalAlignment = HorizontalAlignment.Stretch;
                outputUI.SetAlias("Port", oupPort);
                outputUI.SetAlias("Parent", nParent);
                outputUI.SetAlias("NodeUI", Owner);
                var v = outputUI.GetVariable("Index");
                if (v != null)
                {
                    v.Value = slotindex;
                }
                hl.Add(outputUI);
            }


            if (inpPort != null)
            {

                var inputUI = InformationModel.MakeObject<VisualScriptOptix_InpPort_Reverse>("in");
                inputUI.HorizontalAlignment = HorizontalAlignment.Stretch;
                inputUI.SetAlias("Port", inpPort);
                inputUI.SetAlias("Parent", nParent);
                inputUI.SetAlias("NodeUI", Owner);
                var v = inputUI.GetVariable("Index");
                if (v != null)
                {
                    v.Value = slotindex;
                }
                hl.Add(inputUI);
            }
        }



        return hl;

    }





    private IUANode buildPlaceholder()
    {
        var ui = InformationModel.MakeObject<Panel>("placeholder");
        ui.HorizontalAlignment = HorizontalAlignment.Stretch;
        ui.VerticalAlignment = VerticalAlignment.Top;
        ui.Height = 4;
        return ui;
    }

    private IUANode buildParameter(IUAVariable v)
    {
        var ui = InformationModel.MakeObject<VisualScriptOptix_Parameter>(v.BrowseName);

        ui.HorizontalAlignment = HorizontalAlignment.Stretch;
        ui.SetAlias("Item", v);

        return ui;
    }

}
