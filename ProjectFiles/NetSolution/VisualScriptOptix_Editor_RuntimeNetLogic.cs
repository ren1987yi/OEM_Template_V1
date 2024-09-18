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
using VisualScriptOptix;
using System.Reflection;
using VisualScript.Core;
using FTOptix.Alarm;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
#endregion

using GCOptixToolkit;
using VisualScriptDemo;


namespace VisualScriptDemo
{
    public enum Direct
    {
        TOP,
        BOTTOM,
        LEFT,
        RIGHT,
    }
    public class SvgHelper
    {

        private static float curvature = 0.3f;
        public static Vector2[] CalcBezierPoints(Vector2 p1, Vector2 p2, Direct p1_dir, Direct p2_dir)
        {

            Vector2 pp0, pp1;

            var points = new Vector2[4];


            if (p1.X < p2.X)
            {
                pp0 = p1;
                pp1 = p2;

                points[0] = pp0;
                points[3] = pp1;

                float n = (Math.Abs(pp0.X - pp1.X) * curvature);
                if (curvature != 0 && n < 30) n = 30;

                if (p1_dir == Direct.RIGHT)
                {

                    points[1].X = pp0.X + n;
                }
                else
                {
                    points[1].X = pp0.X - n;
                }
                points[1].Y = pp0.Y;

                if (p2_dir == Direct.LEFT)
                {

                    points[2].X = pp1.X - n;
                }
                else
                {
                    points[2].X = pp1.X + n;
                }
                points[2].Y = pp1.Y;
            }
            else if (p1.X == p2.X)
            {

            }
            else
            {
                pp0 = p2;
                pp1 = p1;

                points[0] = pp0;
                points[3] = pp1;

                float n = (Math.Abs(pp0.X - pp1.X) * curvature);
                if (curvature != 0 && n < 30) n = 30;
                if (p2_dir == Direct.LEFT)
                {
                    points[1].X = pp0.X - n;

                }
                else
                {
                    points[1].X = pp0.X + n;
                }
                points[1].Y = pp0.Y;

                if (p1_dir == Direct.RIGHT)
                {

                    points[2].X = pp1.X + n;
                }
                else
                {
                    points[2].X = pp1.X - n;
                }
                points[2].Y = pp1.Y;
            }

            return points;
        }
        public static void PointsConvertToBezierPath(Vector2[] points, out string svgpath, out Vector2 start_point, out Vector2 end_point)
        {

            //for (var i = 0; i < points.Length; i++)
            //{
            //    var point = points[i];
            //    point.X = Convert.ToSingle(point.X);
            //    point.Y = Convert.ToSingle(point.Y);
            //}

            svgpath = string.Empty;

            if (points == null || points.Length < 2)
            {
                start_point = new Vector2();
                end_point = new Vector2();
                return;
            }
            else
            {

                var min_x = points.Min(p => p.X);
                var min_y = points.Min(p => p.Y);

                var spoint = new Vector2() { X = min_x, Y = min_y };
                var max_x = points.Max(p => p.X);
                var max_y = points.Max(p => p.Y);
                var epoint = new Vector2() { X = max_x, Y = max_y };


                var path = string.Empty;
                var p0 = new Vector2() { X = points[0].X - spoint.X, Y = points[0].Y - spoint.Y };



                //path = $"M{points[0].X - spoint.X:f2} {points[0].Y - spoint.Y:f2} ";
                //for (var i = 1; i < points.Length; i++)
                //{

                //    path += $"L{points[i].X - spoint.X:f2} {points[i].Y - spoint.Y:f2} ";
                //}

                var _size = Math.Max(epoint.X - spoint.X, epoint.Y - spoint.Y);

                path = $"M {points[0].X - spoint.X} {points[0].Y - spoint.Y} C {points[1].X - spoint.X} {points[1].Y - spoint.Y}, {points[2].X - spoint.X} {points[2].Y - spoint.Y} , {points[3].X - spoint.X} {points[3].Y - spoint.Y} M{epoint.X - spoint.X} {epoint.Y - spoint.Y}" + $"M{_size} {_size}";
                start_point = spoint;
                end_point = epoint;
                svgpath = path;
                return;
            }
        }

    }

}


public class VisualScriptOptix_Editor_RuntimeNetLogic : BaseNetLogic
{

    Item nCanvas;
    Item nCanvasNode;
    Item nCanvasLine;


    IUAVariable vCurrentName, vMessage,vShowParameter;

    Store DB;
    IUANode nNodeUIType;
    IUANode nRegisterNodes;
    IUANode nNodeModels;
    IUANode nWorkflow;
    IUANode nVariables;


    IUANode nEditorSetting;

    WorkflowOptix _currentWorkflow = new WorkflowOptix();

    const string TABLE_NAME = "Workflows";


    Assembly asm = typeof(BaseNodeOptix).Assembly;

    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started



        nCanvas = LogicObject.GetAlias("Canvas") as Item;
        nCanvasNode = LogicObject.GetAlias("CanvasNode") as Item;
        nCanvasLine = LogicObject.GetAlias("CanvasLine") as Item;


        nEditorSetting = LogicObject.GetAlias("EditorSetting");

        nNodeUIType = Owner.GetAlias("NodeUIType");


        vCurrentName = LogicObject.GetVariable("CurrentGraphName");
        vMessage = LogicObject.GetVariable("Message");
        vShowParameter = LogicObject.GetVariable("ShowParameter");
        nRegisterNodes = LogicObject.GetObject("RegisterNodes");
        nNodeModels = LogicObject.GetObject("NodeModels");

        nWorkflow = LogicObject.GetObject("Workflow");
        nVariables = nWorkflow.GetObject("Variables");



        var v = Owner.GetVariable("DB");
        DB = InformationModel.Get((NodeId)v.Value) as Store;
        if (DB == null)
        {
            outputMessage("DB is null");
            throw new Exception();
        }

        InitializeUI();
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }






    #region Export Methods
    [ExportMethod]
    public void NewWorkflow(string name)
    {
        var sql = $"SELECT Name FROM {TABLE_NAME} WHERE Name = '{name}'";
        var res = StoreHelpr.Query(DB, sql);
       
        if(res.Count() > 0)
        {
            outputMessage($"graph :{name} is exists");
        }
        else
        {
            var wk = new WorkflowOptix() { Name = name ,Version = 1};
            StoreHelpr.InsertOneRow(DB, TABLE_NAME, new string[] { "Name", "EditDateTime", "Content" ,"Version"}, new object[] { name, DateTime.Now, JsonHelper.Serialize(wk),1 }) ;
            clearEditor();
            _currentWorkflow = wk ;
        }


    }
    
    [ExportMethod]
    public void OpenWorkflow(string name,UInt32 version)
    {
        var sql = $"SELECT Name,Content FROM {TABLE_NAME} WHERE Name = '{name}' AND Version = {version}";
        var res = StoreHelpr.Query(DB, sql);
        if(res.Count() > 0)
        {
            var content = res.First()["Content"] as string;
            clearEditor();


            //TODO parse the data
            var wf = JsonHelper.Deserialize<WorkflowOptix>(content);
            if(wf != null)
            {

                _currentWorkflow = wf;
            }
            else
            {
                outputMessage($"graph :{name} is error");
                _currentWorkflow = new WorkflowOptix() { Name = name };

            }
            loadWorkflow(_currentWorkflow);

            vCurrentName.Value = name;
        }
        else
        {
            outputMessage($"graph :{name} is not exists");
        }
    }

    [ExportMethod]
    public void DeleteWorkflow(string name,UInt32 version)
    {
        var sql = $"DELETE FROM {TABLE_NAME} WHERE Name = '{name}' AND Version = {version}";
        StoreHelpr.ExecuteSql(DB, sql);

    }

    [ExportMethod]
    public void SaveWorkflow(string name,UInt32 version)
    {


        var graph = saveGraph(out var errs);


        var _msg = string.Join("\n", errs);
        outputMessage(_msg);

        var vs = saveVariables();


        _currentWorkflow.Name = name;
        _currentWorkflow.Version = version;
        _currentWorkflow.Graph = graph;
        _currentWorkflow.Variables = new List<Variable>();
        _currentWorkflow.Variables.AddRange(vs);


        var _content = JsonHelper.Serialize(_currentWorkflow);
        var sql = $"UPDATE {TABLE_NAME} SET Content = '{_content}', EditDateTime = '{DateTime.Now}' WHERE Name = '{name}' AND Version = {version}";
        StoreHelpr.ExecuteSql(DB,sql);

    }


    [ExportMethod]
    public void SaveNewVersionWorkflow(string name, UInt32 version)
    {
        var sql = $"SELECT Name,Content,Version FROM {TABLE_NAME} WHERE Name = '{name}' AND Version = {version}";
        var res = StoreHelpr.Query(DB, sql);

        if (res.Count() > 0)
        {





            var content = res.First()["Content"] as string;


            sql = $"SELECT MAX(Version) as mm FROM {TABLE_NAME} WHERE Name = '{name}'";
            var res1 = StoreHelpr.Query(DB, sql);
            if(res1.Count() > 0)
            {
                dynamic _maxversion = res1.First()["mm"];
                _maxversion++;



                var _wk = JsonHelper.Deserialize<WorkflowOptix>(content);
                _wk.Version = Convert.ToUInt32(_maxversion);


                content = JsonHelper.Serialize(_wk);

                StoreHelpr.InsertOneRow(DB, TABLE_NAME, new string[] { "Name", "EditDateTime", "Content", "Version" }, new object[] { name, DateTime.Now, content, _maxversion });
                outputMessage($"graph :{name} save as new version ok");
            }

        }
        else
        {
            outputMessage($"graph :{name} is not exists");
        }


      

    }





    [ExportMethod]
    public void RefreshDB()
    {

    }

    [ExportMethod]
    public void AddNode(NodeId nodeId)
    {
        var node = InformationModel.Get(nodeId);
        if (node == null)
        {
            outputMessage("AddNode failed,no selected node");
            return;
        }
        else
        {
            var v = node.GetVariable("NodeType");
            var v1 = node.GetVariable("Content");
            if (v != null && v1 != null)
            {
                var typeName = (string)v.Value;
                var content = (string)v1.Value;
                outputMessage($"selected node type name:{typeName}");
                addNode(typeName,null,content);
            }
            else
            {
                outputMessage("AddNode failed,selected node have not nodetype infomation");
            }

        }
    }

    [ExportMethod]
    public void DeleteNode(NodeId nodeId)
    {
        var nodeUI = InformationModel.Get(nodeId);
        if (nodeUI == null)
        {
            outputMessage("DeleteNode failed,no input node ui");
            return;
        }
        else
        {
            delNode(nodeUI);
          

        }
    }



    [ExportMethod]
    public void SelectInputPort(NodeId portId,NodeId nodeId,int index)
    {

        Log.Info("click input port");

        var nodeUI = InformationModel.Get(nodeId);
        var nodeModel = nodeUI.GetAlias("NodeModel");
        var v = InformationModel.Get(portId) as IUAVariable;
        if(v != null)
        {
            clickPort(v, 1,nodeUI,nodeModel,index);

        }
    }

    [ExportMethod]
    public void SelectOutputPort(NodeId portId, NodeId nodeId, int index)
    {
        Log.Info("click output port");
        var nodeUI = InformationModel.Get(nodeId);
        var nodeModel = nodeUI.GetAlias("NodeModel");
        var v = InformationModel.Get(portId) as IUAVariable;
        if (v != null)
        {
            clickPort(v, 0, nodeUI, nodeModel, index);

        }
    }



    [ExportMethod]
    public void ShowPara(NodeId nodeModelId)
    {
        var nodeModel = InformationModel.Get(nodeModelId) as VisualScriptOptixNodeType;
        if(nodeModel != null)
        {
            //LogicObject.SetAlias("SelectedNodeModel", nodeModel.GetObject("Parameters"));

            //if(nodeModel.TypeName == typeof(CallEMNode).FullName)
            //{
            //    var dlgType = nEditorSetting.GetAlias("DlgUIType_EMConfigure") as DialogType;
            //    var nEMs = nEditorSetting.GetAlias("EMs");
            //    if(dlgType != null && nEMs != null)
            //    {
            //        (Owner as Item).OpenDialog(dlgType, new NodeId[] { nodeModel.NodeId,nEMs.NodeId });
            //    }
            //}
            //else
            //{

            //}


            LogicObject.SetAlias("SelectedNodeModel", nodeModel);
            vShowParameter.Value = true;
        }

        



    }
    #endregion




    #region 需要定制修改部分
    /// <summary>
    /// 这是画面初始化程序,可定制修改
    /// </summary>
    private void InitializeUI()
    {
        RegisterVisualNodes();
    }

    /// <summary>
    /// 注册节点的类型
    /// </summary>
    private void RegisterVisualNodes()
    {
        RegisterVisualNode("Logic", "Start", typeof(StartNode));
        RegisterVisualNode("Logic", "End", typeof(EndNode));
        //RegisterVisualNode("Logic", "Test", typeof(TestNode));
        RegisterVisualNode("Logic", "IfElse", typeof(IfElseNode));
        RegisterVisualNode("Logic", "AddBranch", typeof(AddBranchNode));
        RegisterVisualNode("Logic", "OrBranch", typeof(OrBranchNode));
        RegisterVisualNode("Logic","Hub",typeof(VisualScriptOptix.ReverseNode));


        RegisterVisualNode("Assign", "Assign", typeof(AssignNode));

        RegisterVisualNode("Function", "Script", typeof(ScriptNode));
        RegisterVisualNode("Function", "Sleep", typeof(SleepNode));
        //RegisterVisualNode("Logic", "CallEM", typeof(CallEMNode));



        //RegisterVisualNode("PLC", "PlcDelay", typeof(PlcDelayNode));
        //RegisterVisualNode("PLC", "AxisMAM", typeof(PlcAxisMAMNode));


        //RegisterVisualNode("PLC", "AxisMAM_V2", typeof(PlcAxisMAM_V2Node));


        //TODO 搜集所有外部指令块

        //RegisterVisualNode("PLC", "PLC Delay", typeof(PlcEM_Node),"123");
        registerEMNode();


    }

    private void RegisterVisualNode(string catelog,string name,Type nodeType,string content=null)
    {
        var obj = InformationModel.MakeObject($"{catelog}_{name}");

        var v = InformationModel.MakeVariable("Catelog", OpcUa.DataTypes.String);
        v.Value = catelog;
        obj.Add(v);

        v = InformationModel.MakeVariable("Name", OpcUa.DataTypes.String);
        v.Value = name;
        obj.Add(v);

        v = InformationModel.MakeVariable("NodeType", OpcUa.DataTypes.String);
        v.Value = nodeType.FullName;
        obj.Add(v);

        v = InformationModel.MakeVariable("Content", OpcUa.DataTypes.String);
        v.Value = content??string.Empty;
        obj.Add(v);

        this.nRegisterNodes.Add(obj);
    }


    #endregion


    private void registerEMNode()
    {
        

        var emIndex = 0;
        var nEMs = nEditorSetting.GetAlias("EMs");
        foreach (var nEM in nEMs.Children.OfType<VisualScriptOptix_EM_ModelType>())
        {
            var name = nEM.Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var emInfo = new PLC_EM_Info();
            emInfo.Name = name;
            emInfo.EMIndex = emIndex;



            foreach(var v in nEM.InputFloats.Children.OfType<AnalogItem>())
            {
                var pname = v.GetVariableValue<string>("Name");
                if(string.IsNullOrWhiteSpace(pname))
                {
                    break;
                }

                emInfo.Parameters.Add(pname);

            }

            RegisterVisualNode("EM", name, typeof(PlcEM_Node), JsonHelper.Serialize(emInfo));



            emIndex++;
        }
    }



    private void clearEditor()
    {
        vCurrentName.Value = string.Empty;
        vMessage.Value = string.Empty;
        selectedPorts[0] = null; selectedPorts[1] = null;

        foreach(var item in nCanvasNode.Children)
        {
            item.Delete();
        }

        foreach (var item in nCanvasLine.Children)
        {
            item.Delete();
        }

        foreach (var item in nNodeModels.Children)
        {
            item.Delete();
        }

        _links.Clear();


        foreach(var item in nVariables.Children)
        {
            item.Delete();
        }



    }

    private void outputMessage(string message)
    {
        vMessage.Value = message;
    }


    private void init_PlcEM_Node(PlcEM_Node plcemNode, string content)
    {
        var emInfo = JsonHelper.Deserialize<PLC_EM_Info>(content);
        if (emInfo == null)
        {
            plcemNode.Initialize();
        }
        else
        {
            plcemNode.Initialize(() => {

                plcemNode.Name = emInfo.Name;

                plcemNode.AddParameter("Index", DataValueType.NUMBER, $"{emInfo.EMIndex}");

                foreach (var p in emInfo.Parameters)
                {
                    plcemNode.AddParameter(p, DataValueType.STRING, "");
                }

                

            });
        }
    }

    private void addNode(string typefullname,BaseNode nodeData=null,string content=null)
    {

  

        var type = asm.GetType(typefullname);
        if(type ==  null)
        {
            outputMessage("node type is missing");
            return;
        }
        if(nodeData == null)
        {

            nodeData = System.Activator.CreateInstance(type) as BaseNode;
            nodeData.Id = Guid.NewGuid();

            if (string.IsNullOrWhiteSpace(content))
            {
                nodeData.Initialize();

            }
            else
            {

                if(typefullname == typeof(PlcEM_Node).FullName)
                {

                    var plcemNode = nodeData as PlcEM_Node;
                    if(plcemNode != null)
                    {


                        init_PlcEM_Node(plcemNode, content);
                    }
                    else
                    {
                        outputMessage("node type is error");
                    }
                }
            }


        }


        

     

        var nodeModel = buildNodeModel(nodeData);



        nNodeModels.Add(nodeModel);

        var nodeUI = InformationModel.MakeObject(nodeModel.BrowseName, nNodeUIType.NodeId) as Item;
        nodeUI.SetAlias("NodeModel", nodeModel);
        nodeUI.SetAlias("Parent", LogicObject);
        
        nodeUI.LeftMargin = nodeData.Position.X;
        nodeUI.TopMargin = nodeData.Position.Y; 
        nodeUI.Width = nodeData.Width;
        var vPos = nodeUI.GetVariable("Pos");
        if(vPos != null)
        {
            vPos.VariableChange += (s,e) => { on_NodePos_VariableChange(s, e, nodeUI); };
        }

        nodeUI.SetVariableValue<bool>("EditMode", true);


        nCanvasNode.Add(nodeUI);

    }

  
    private void delNode(IUANode nodeUI)
    {
        var nodeModel = nodeUI.GetAlias("NodeModel");
        var vPos = nodeUI.GetVariable("Pos");
        if (vPos != null)
        {
            vPos.VariableChange -= (s, e) => { on_NodePos_VariableChange(s, e, nodeUI); };
        }

        //remove link
        RemoveLinkWithNode(nodeUI);
        nodeUI.Delete();
        if (nodeModel != null)
        {
            nodeModel.Delete();

        }
        else
        {
            outputMessage("DeleteNode failed,selected node have not nodemodel");
        }
    }


    private IUANode buildNodeModel(BaseNode data)
    {
        var nodeModel = InformationModel.MakeObject<VisualScriptOptixNodeType>(data.Id.ToString());
        nodeModel.TypeName = data.GetType().FullName;
        nodeModel.Id = data.Id;
        nodeModel.Name = data.Name;
        nodeModel.Comment = data.Comment;
        foreach(var sp in data.Parameters.Where(pp=>pp.Visible == true))
        {
            var vdatatype = NodeId.Empty;
            object vValue = null;
            switch (sp.DataType)
            {
                case DataValueType.BOOL:
                    vdatatype = OpcUa.DataTypes.Boolean;
                    vValue = Convert.ToBoolean(sp.Value);
                    break;
                case DataValueType.STRING:
                    vdatatype = OpcUa.DataTypes.String;
                    vValue = Convert.ToString(sp.Value);
                    break;
                case DataValueType.NUMBER:
                    vdatatype = OpcUa.DataTypes.Float;
                    vValue = Convert.ToSingle(sp.Value);
                    break;
                default:
                    throw new Exception($"node parameter datatype error ,parameter is :{sp.Name}");
                    break;
            }

            var v = InformationModel.MakeVariable(sp.Name, vdatatype);
            if(vValue != null)
            {
                v.Value = new UAValue(vValue);
            }
            nodeModel.Parameters.Add(v);

        }

        foreach(var sp in data.InputPorts)
        {
            var v = InformationModel.MakeVariable(sp.Name,OpcUa.DataTypes.Boolean);
            nodeModel.InputPorts.Add(v);
        }

        foreach (var sp in data.OutputPorts)
        {
            var v = InformationModel.MakeVariable(sp.Name, OpcUa.DataTypes.Boolean);
            nodeModel.OutputPorts.Add(v);
        }

        

        return nodeModel;
    }






    LinkPort[] selectedPorts = new LinkPort[2];
    private void clickPort(IUAVariable port,int idx,IUANode nodeUI,IUANode nodeModel,int portIdx)
    {
        if (selectedPorts[idx] != null)
        {
            var _port = selectedPorts[idx];
            if(_port.Port == null || _port.NodeUI == null || _port.NodeModel == null)
            {
                selectedPorts[idx] = null;
            }
        }


        if (selectedPorts[idx] != null)
        {
            var v = selectedPorts[idx].Port;
            v.Value = false;


            if (selectedPorts[idx].Port.NodeId == port.NodeId)
            {
                selectedPorts[idx] = null;
            }
            else
            {

                port.Value = true;
                selectedPorts[idx] = new LinkPort() { Port = port,Index = portIdx, NodeUI = nodeUI,NodeModel = nodeModel};
            }
        }
        else
        {

            port.Value = true;
            selectedPorts[idx] = new LinkPort() { Port = port, Index = portIdx, NodeUI = nodeUI, NodeModel = nodeModel };

            if (selectedPorts[0] != null && selectedPorts[1] != null) {

                //TODO link
                updateLink();

                //clear both port selected
                selectedPorts[0].Port.Value = false;
                selectedPorts[1].Port.Value = false;
                selectedPorts[0] = null; 
                selectedPorts[1] = null;
            }



        }
    }



    private void on_NodePos_VariableChange(object sender, VariableChangeEventArgs e,IUANode nodeUI)
    {

        //throw new NotImplementedException();
        updateLinkWithNode(nodeUI);
       
    }


    private void RemoveLinkWithNode(IUANode nodeUI)
    {


        var links = _links.Where(l => l.From.NodeUI.NodeId == nodeUI.NodeId).Select(ll=>ll.LinkUI).ToList();
        links.AddRange(_links.Where(l => l.To.NodeUI.NodeId == nodeUI.NodeId).Select(ll => ll.LinkUI));

        foreach(var link in links)
        {
            link.Delete();
        }

        _links.RemoveAll(l=>l.From.NodeUI.NodeId == nodeUI.NodeId);
        _links.RemoveAll(l => l.To.NodeUI.NodeId == nodeUI.NodeId);


    }


    private void updateLinkWithNode(IUANode nodeUI)
    {
        var links = _links.Where(l => l.From.NodeUI.NodeId == nodeUI.NodeId).ToList();
        links.AddRange(_links.Where(l => l.To.NodeUI.NodeId == nodeUI.NodeId));

        foreach (var linkPair in links)
        {

            getLinkPosition(linkPair, out var fpos, out var tpos,out var f_dir,out var t_dir);
            var points = SvgHelper.CalcBezierPoints(fpos, tpos,f_dir,t_dir);

            SvgHelper.PointsConvertToBezierPath(points, out var svgpath, out var start_point, out var end_point);

            var lineUI = linkPair.LinkUI as PolyLine;
            if (lineUI != null)
            {
                lineUI.Path = svgpath;
                lineUI.LeftMargin = start_point.X;
                lineUI.TopMargin = start_point.Y;
                lineUI.Width = -1;
                lineUI.Height = -1;
            }

        }
    }



    List<LinkPair> _links = new List<LinkPair>();
    private void updateLink()
    {
        var linkPair = _links.Where(l => l.From.Port.NodeId == selectedPorts[0].Port.NodeId &&
                    l.To.Port.NodeId == selectedPorts[1].Port.NodeId).FirstOrDefault();
        if(linkPair != null )
        {
            //remove link

            linkPair.LinkUI.Delete();

            _links.Remove(linkPair);


        }else
        {
            //add link
            linkPair = new LinkPair() { From = selectedPorts[0], To = selectedPorts[1] };
            _links.Add(linkPair);

            getLinkPosition(linkPair, out var fpos, out var tpos, out var f_dir, out var t_dir);
            var line = drawLinkLine(fpos, tpos, f_dir, t_dir);
            linkPair.LinkUI = line;
            nCanvasLine.Add(line);
        }

    }


    private void getLinkPosition(LinkPair linkPair,out Vector2 p1,out Vector2 p2,out Direct p1_direct ,out Direct p2_direct)
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

        if(ftypename != typeof(VisualScriptOptix.ReverseNode).FullName)
        {
            fp = new Vector2(fui.Width - 6, linkPair.From.Index * 40 + 40 + 20);

        }
        else
        {
            fp = new Vector2(6, linkPair.From.Index * 40 + 40 + 20);
            p1_direct = Direct.LEFT;
        }

        if(ttypename != typeof(ReverseNode).FullName)
        {

            tp = new Vector2(6, linkPair.To.Index * 40 + 40 + 20);
        }
        else
        {
            tp = new Vector2(tui.Width-6, linkPair.To.Index * 40 + 40 + 20);
            p2_direct = Direct.RIGHT;
        }

        fpos = fpos + fp;
        tpos = tpos + tp;

        p1 = fpos;
        p2 = tpos;
    }
    private IUANode drawLinkLine(Vector2 p1,Vector2 p2,Direct p1_dir,Direct p2_dir)
    {
        var line = CreatePathWithPolylineV2(p1, p2,p1_dir,p2_dir);
        return line;
    }



    private Variable[] saveVariables()
    {
        var lt = new List<Variable>();

        foreach(var item in nVariables.Children)
        {
            var v = new Variable();
            v.Name = item.GetVariableValue<string>("Name");
            v.DefaultValue = item.GetVariableValue<string>("DefaultValue");
            v.DataType = (DataType)item.GetVariableValue<int>("DataType");
            lt.Add(v);
        }




        return lt.ToArray();
    }

    private Graph saveGraph(out List<string> errList)
    {

        errList = new List<string>();


        var graph = new Graph();
        graph.Nodes = new List<BaseNode>();
        graph.Links = new List<Link>();
        foreach (var nodeModel in nNodeModels.Children.OfType<VisualScriptOptixNodeType>())
        {

            var nodeUI = nCanvasNode.Get(nodeModel.BrowseName) as Item;
            if (nodeUI == null)
            {
                throw new Exception("node ui is not exists");
            }

            var typeName = nodeModel.TypeName;
            var type = asm.GetType(typeName);
            var node = System.Activator.CreateInstance(type) as BaseNodeOptix;
            node.Initialize();

            //if (typeName == typeof(PlcEM_Node).FullName)
            //{

            //    var plcemNode = node as PlcEM_Node;
            //    if (plcemNode != null)
            //    {


            //        init_PlcEM_Node(plcemNode, nodeModel.Content);
            //    }
            //    else
            //    {
            //        outputMessage("node type is error");
            //    }
            //}
            //else
            //{
            //    node.Initialize();
            //}


            node.Name = nodeModel.Name;
            node.Position = new Vector2(nodeUI.LeftMargin, nodeUI.TopMargin);
            node.Comment = nodeModel.Comment;


            foreach (var p in nodeModel.Parameters.Children.OfType<IUAVariable>())
            {
                var pv = node.Parameters.Where(pp => pp.Name == p.BrowseName).FirstOrDefault();

                if (pv != null)
                {

                    pv.Value = p.Value.Value.ToString();
                }
                else
                {
                    //throw new Exception("parameter is not exists");
                    node.AddParameter(p.BrowseName, DataValueType.STRING, "");
                    pv = node.Parameters.Where(pp => pp.Name == p.BrowseName).FirstOrDefault();
                    if (pv != null)
                    {

                        pv.Value = p.Value.Value.ToString();
                    } else
                    {
                        throw new Exception("parameter is not exists");
                    }
                }

            }
            node.Id = new Guid(nodeModel.BrowseName);


            var _test_result = node.Test() as ResultNodeExecute;
            if (_test_result.Type == ResultType.Success)
            {

            }
            else
            {
                errList.Add($"Node:{node.Name}  {_test_result.ErrorMessage}");
            }

            nodeModel.SetVariableValue<int>("State", (int)node.State);
            
            graph.Nodes.Add(node);



        }

        foreach(var linkpair in this._links)
        {
            var fromId = linkpair.From.NodeUI.BrowseName;
            
            var fromName = linkpair.From.Index.ToString();

            var toId = linkpair.To.NodeUI.BrowseName;
            var toName = linkpair.To.Index.ToString();

            var link = new Link()
            {
                FromId = fromId,
                FromName = fromName,
                ToId = toId,
                ToName = toName,
            };
            graph.Links.Add(link);
        }



        return graph;
    }


    private void loadGraph(Graph graph)
    {
        foreach(var nodeData in graph.Nodes)
        {

            addNode(nodeData.GetType().FullName, nodeData);
        }

        foreach(var link in graph.Links)
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
            _links.Add(linkPair);

            getLinkPosition(linkPair, out var fpos, out var tpos, out var f_dir, out var t_dir);
            var line = drawLinkLine(fpos, tpos,f_dir,t_dir);
            linkPair.LinkUI = line;
            nCanvasLine.Add(line);
        }

    }

    private void loadWorkflow(WorkflowOptix wk)
    {
        loadGraph(wk.Graph);
        loadVariables(wk.Variables);
    }

    private void loadVariables(IEnumerable<Variable> variables)
    {

        foreach (var variable in variables)
        {
            var n = InformationModel.MakeObject(variable.Name);
            var v = InformationModel.MakeVariable("Name",OpcUa.DataTypes.String);
            v.Value = variable.Name;
            n.Add(v);


            v = InformationModel.MakeVariable("DataType", OpcUa.DataTypes.Int32);
            v.Value = (int)variable.DataType;
            n.Add(v);

            v = InformationModel.MakeVariable("DefaultValue", OpcUa.DataTypes.String);
            v.Value = variable.DefaultValue;
            n.Add(v);


            nVariables.Add(n);
        }
    }

    /// <summary>
    /// 创建连线路径
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    private PolyLine CreatePathWithPolylineV2(Vector2 p1, Vector2 p2, Direct f_dir,Direct t_dir, params object[] args)
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

    class VisualScriptEditor
    {
        class RegisterNode
        {
            public string Name { get; set; }
            public string Catelog { get; set; }

            public Type NodeType { get; set; }
        }
    }
    

  


}



