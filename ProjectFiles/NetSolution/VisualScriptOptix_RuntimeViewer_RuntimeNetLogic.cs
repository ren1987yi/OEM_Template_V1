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
using GCOptixToolkit;
using VisualScriptOptix;
using VisualScript.Core;
using System.Numerics;
using System.Linq;
using FTOptix.WebUI;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;
#endregion
using VisualScriptDemo;
public class VisualScriptOptix_RuntimeViewer_RuntimeNetLogic : BaseNetLogic
{
    VisualScriptEngineType nEngineModel;
    IUANode nNodeModels;
    IUANode nNodeUIType;

    Item nCanvas;
    Item nCanvasNode;
    Item nCanvasLine;

    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started


        nEngineModel = Owner.GetAlias("Model") as VisualScriptEngineType;
        if(nEngineModel == null)
        {
            throw new Exception("Model is null");
        }
        nNodeModels = nEngineModel.NodeModels;

        nNodeUIType = Owner.GetAlias("NodeUIType");
        if (nNodeUIType == null)
        {
            throw new Exception("node ui type is null");
        }



        nCanvas = LogicObject.GetAlias("Canvas") as Item;
        nCanvasNode = LogicObject.GetAlias("CanvasNode") as Item;
        nCanvasLine = LogicObject.GetAlias("CanvasLine") as Item;

        buildWorkflow();
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    private void buildWorkflow()
    {
        var wk = JsonHelper.Deserialize<WorkflowOptix>(nEngineModel.Content);
        if(wk == null)
        {
            throw new NullReferenceException("workflow is null");
        }

        loadGraph(wk);

    }

    private void loadGraph(WorkflowOptix wk)
    {

        var graph = wk.Graph;

        foreach (var nodeData in graph.Nodes)
        {

            var nNodeModel = nNodeModels.Get(nodeData.Id.ToString());

            if(nNodeModel != null && nodeData != null)
            {

                addNode(nodeData, nNodeModel);
            }
        }

        foreach (var link in graph.Links)
        {
            //link.FromName
            var nodeUI = nCanvasNode.Get(link.FromId);
            var nodeModel = nNodeModels.Get(link.FromId);
            var index = Convert.ToInt32(link.FromName);
            var ports = nodeModel.Get("OutputPorts");
            var port = ports.Children[index] as IUAVariable;

            var flinkport = new LinkPort()
            {
                NodeUI = nodeUI,
                NodeModel = nodeModel,
                Index = index,
                Port = port
            };

            nodeUI = nCanvasNode.Get(link.ToId);
            nodeModel = nNodeModels.Get(link.ToId);
            index = Convert.ToInt32(link.ToName);
            ports = nodeModel.Get("InputPorts");
            port = ports.Children[index] as IUAVariable;

            var tlinkport = new LinkPort()
            {
                NodeUI = nodeUI,
                NodeModel = nodeModel,
                Index = index,
                Port = port
            };





            var linkPair = new LinkPair() { From = flinkport, To = tlinkport };
            //_links.Add(linkPair);

            getLinkPosition(linkPair, out var fpos, out var tpos, out var f_dir, out var t_dir);
            var line = drawLinkLine(fpos, tpos, f_dir, t_dir);
            linkPair.LinkUI = line;
            nCanvasLine.Add(line);
        }

    }

    private void addNode(BaseNode nodeData, IUANode nodeModel)
    {
       

        var nodeUI = InformationModel.MakeObject(nodeModel.BrowseName, nNodeUIType.NodeId) as Item;
        nodeUI.SetAlias("NodeModel", nodeModel);
        nodeUI.SetAlias("Parent", LogicObject);

        nodeUI.LeftMargin = nodeData.Position.X;
        nodeUI.TopMargin = nodeData.Position.Y;
        nodeUI.Width = nodeData.Width;
      

        nodeUI.SetVariableValue<bool>("EditMode", false);


        nCanvasNode.Add(nodeUI);

    }

    private void getLinkPosition(LinkPair linkPair, out Vector2 p1, out Vector2 p2, out Direct p1_direct, out Direct p2_direct)
    {
        p1_direct = Direct.RIGHT;

        p2_direct = Direct.LEFT;




        var fui = (linkPair.From.NodeUI as Item);
        var tui = (linkPair.To.NodeUI as Item);


        var ftypename = linkPair.From.NodeModel.GetVariableValue<string>("TypeName");
        var ttypename = linkPair.To.NodeModel.GetVariableValue<string>("TypeName");





        Vector2 fpos = new Vector2(fui.LeftMargin, fui.TopMargin);
        Vector2 tpos = new Vector2(tui.LeftMargin, tui.TopMargin);

        Vector2 fp, tp;

        if (ftypename != typeof(VisualScriptOptix.ReverseNode).FullName)
        {
            fp = new Vector2(fui.Width, linkPair.From.Index * 40 + 40 + 20);

        }
        else
        {
            fp = new Vector2(0, linkPair.From.Index * 40 + 40 + 20);
            p1_direct = Direct.LEFT;
        }

        if (ttypename != typeof(ReverseNode).FullName)
        {

            tp = new Vector2(0, linkPair.To.Index * 40 + 40 + 20);
        }
        else
        {
            tp = new Vector2(tui.Width, linkPair.To.Index * 40 + 40 + 20);
            p2_direct = Direct.RIGHT;
        }

        fpos = fpos + fp;
        tpos = tpos + tp;

        p1 = fpos;
        p2 = tpos;
    }
    private IUANode drawLinkLine(Vector2 p1, Vector2 p2, Direct p1_dir, Direct p2_dir)
    {
        var line = CreatePathWithPolylineV2(p1, p2, p1_dir, p2_dir);
        return line;
    }

    /// <summary>
    /// 创建连线路径
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    private PolyLine CreatePathWithPolylineV2(Vector2 p1, Vector2 p2, Direct f_dir, Direct t_dir, params object[] args)
    {
        var points = SvgHelper.CalcBezierPoints(p1, p2, f_dir, t_dir);

        SvgHelper.PointsConvertToBezierPath(points, out var svgpath, out var start_point, out var end_point);
        var uiline = BuildPath(Guid.NewGuid().ToString(), svgpath);
        uiline.LeftMargin = start_point.X;
        uiline.TopMargin = start_point.Y;
        uiline.Width = -1;
        uiline.Height = -1;


        //var point1 = InformationModel.MakeObject<Ellipse>("point1");
        //point1.Width = 2;
        //point1.Height = 2;
        //point1.FillColor = Colors.Red;
        //point1.LeftMargin = 0;
        //point1.TopMargin = 0;
        //uiline.Add(point1);


        //point1 = InformationModel.MakeObject<Ellipse>("point2");
        //point1.Width = 2;
        //point1.Height = 2;
        //point1.FillColor = Colors.Red;
        //point1.RightMargin = 0;
        //point1.BottomMargin = 0;
        //point1.HorizontalAlignment = HorizontalAlignment.Right;
        //point1.VerticalAlignment = VerticalAlignment.Bottom;
        //uiline.Add(point1);



        return uiline;
    }

    private PolyLine BuildPath(string name, string path)
    {
        var ui = InformationModel.Make<PolyLine>(name);
        ui.Path = path;
        ui.FillColor = Colors.Transparent;
        //ui.LineColor = new Color(255, 181, 181, 181);
        ui.LineColor = new Color(255, 41, 250, 19);
        ui.LineThickness = 2;
        return ui;

    }
    
    class LinkPair
    {
        public LinkPort From { get; set; }
        public LinkPort To { get; set; }
        public IUANode LinkUI { get; set; }
    }

    class LinkPort
    {
        public IUAVariable Port { get; set; }
        public IUANode NodeUI { get; set; }
        public IUANode NodeModel { get; set; }
        public int Index { get; set; }
    }




}
