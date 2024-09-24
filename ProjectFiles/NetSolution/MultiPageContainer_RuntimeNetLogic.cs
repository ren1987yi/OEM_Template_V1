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
using GCOptixToolkit;
using System.Collections.Generic;
using System.Linq;
#endregion

public class MultiPageContainer_RuntimeNetLogic : BaseNetLogic
{

    IUANode nPanelLoader;
    IUANode nTabContainer;
    Item nTop,nBottom,nLeft,nRight;
    IUANode nTabUIType;

    IUAVariable vTargetPage;
    NavigationPanelTabPosition tabPosition;


    List<TabMapper> tabs = new List<TabMapper>();


    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started


        tabPosition = Owner.GetVariableValue<NavigationPanelTabPosition>("TabPosition");
        vTargetPage = Owner.GetVariable("TargetPage");

        nTabUIType = Owner.GetAlias("TabUIType");
        nTop = LogicObject.GetAlias("Top") as Item;
        nBottom = LogicObject.GetAlias("Bottom") as Item;
        nLeft = LogicObject.GetAlias("Left") as Item;
        nRight = LogicObject.GetAlias("Right") as Item;
        nPanelLoader = LogicObject.GetAlias("PanelLoader");

        nTop.Visible = false;
        nBottom.Visible = false;
        nLeft.Visible = false;
        nRight.Visible = false;
        IUANode curTabContainer = null;
        switch (tabPosition)
        {
            case NavigationPanelTabPosition.Top:
                nTop.Visible = true;
                curTabContainer = nTop;
                break;
            case NavigationPanelTabPosition.Bottom:
                nBottom.Visible = true;
                curTabContainer= nBottom;
                break;
            case NavigationPanelTabPosition.Left:
                nLeft.Visible = true;
                curTabContainer = nLeft;
                break;
            case NavigationPanelTabPosition.Right:
                nRight.Visible = true;
                curTabContainer = nRight;
                break;
            case NavigationPanelTabPosition.Hidden:
                nTop.Visible = false;
                nBottom.Visible = false;
                nLeft.Visible = false;
                nRight.Visible = false;
                break;
            
        }

        if(curTabContainer == null)
        {
            nTabContainer = null;
        }
        else
        {
            nTabContainer = curTabContainer.Get("Container");
        }

        if(vTargetPage != null)
        {
            vTargetPage.VariableChange += VTargetPage_VariableChange;
        }

    }

    

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
        if (vTargetPage != null)
        {
            vTargetPage.VariableChange -= VTargetPage_VariableChange;
        }
    }


    [ExportMethod]
    public void ClosePage(NodeId id)
    {
        var _tab = InformationModel.Get(id);
        var mapper = this.tabs.Where(m => m.UITab.NodeId == id).FirstOrDefault();
        if (mapper != null)
        {
            this.tabs.Remove(mapper);

            mapper.UIIns.Visible = false;
            mapper.UIIns.Delete();
            _tab.Delete();

            var _mapper = tabs.FirstOrDefault();
            if(_mapper != null)
            {
                _mapper.UIIns.Visible = true;
                _mapper.UITab.SetVariableValue("Selected", true);
            }

        }

    }

    [ExportMethod]
    public void ChangePage(NodeId id)
    {
        var _tab = InformationModel.Get(id);
        var mapper = this.tabs.Where(m => m.UITab.NodeId == id).FirstOrDefault();
        if (mapper != null)
        {
            foreach ( var tab in this.tabs)
            {
                tab.UIIns.Visible = false;
                tab.UITab.SetVariableValue<bool>("Selected", false);
            }

            mapper.UIIns.Visible = true;
            mapper.UITab.SetVariableValue("Selected", true);
        }
    }


    private void VTargetPage_VariableChange(object sender, VariableChangeEventArgs e)
    {
        //throw new NotImplementedException();

        var id = e.NewValue.Value as NodeId;
        if(id.NamespaceIndex == NodeId.InvalidNamespaceIndex)
        {
            return;
        }
        var mapper = IsNewPage(id);
        if (mapper == null)
        {
            BuildTab(id);
        }
        else
        {
            OpenPage(mapper);
        }
        vTargetPage.Value = NodeId.Random(NodeId.InvalidNamespaceIndex);

    }


    private TabMapper IsNewPage(NodeId id)
    {
        return tabs.Where(m => m.UIType.NodeId == id).FirstOrDefault();
    }

    private void OpenPage(TabMapper mapper)
    {
        foreach(var tab in tabs)
        {
            tab.UIIns.Visible = false;
            tab.UITab.SetVariableValue<bool>("Selected", false);
        }


        mapper.UIIns.Visible = true;
        mapper.UITab.SetVariableValue("Selected", true);
    }
    private void BuildTab(NodeId id)
    {
        IUANode uitype = InformationModel.Get(id);

        var ins = InformationModel.MakeObject(Guid.NewGuid().ToString(), uitype.NodeId) as Item;
        ins.VerticalAlignment = VerticalAlignment.Stretch;
        ins.HorizontalAlignment = HorizontalAlignment.Stretch;

        foreach(var mapper in this.tabs)
        {
            mapper.UIIns.Visible = false;
            mapper.UITab.SetVariableValue<bool>("Selected", false);
        }

        ins.Visible = true;
        nPanelLoader.Add(ins);

        var _mapper = new TabMapper()
        {
            UIType = uitype,
            UIIns = ins,

        };

        tabs.Add(_mapper);


        if(nTabContainer == null)
        {
            
            return;
        }
        Log.Info("add tab");
        var _tab = InformationModel.MakeObject(ins.BrowseName, nTabUIType.NodeId) as Item;
        _tab.SetVariableValue<NodeId>("ParentLogic", LogicObject.NodeId);
        _tab.SetVariableValue<string>("Title",ins.BrowseName);
        _tab.SetAlias("PageUIType", uitype);
        _tab.SetVariableValue<bool>("Selected", true);
        _mapper.UITab = _tab;
        nTabContainer.Add(_tab);

    }




    class TabMapper
    {
        public IUANode UIType { get; set; }
        public Item UIIns { get; set; }
        public Item UITab { get; set; }
    }
}
