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
using FTOptix.System;
using FTOptix.Retentivity;
using FTOptix.UI;
using FTOptix.Alarm;
using FTOptix.Core;
using System.IO;
using System.Collections.Generic;
using OEM_Template_V1;
using System.Linq;
using System.Globalization;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;
using FTOptix.EventLogger;
#endregion

public class OutputLogViewer_RuntimeNetLogic : BaseNetLogic
{
    IUAVariable vRefreshSource;
    IUAVariable vRefreshGrd;
    IUAVariable vPageCount;
    IUAVariable vPageIndex;
    IUAVariable vPageSize;

    IUANode nSource;
    IUANode nLogs;

    string prjName;
    bool isEmulate;
    DirectoryInfo runtimeDirIndo;
    DirectoryInfo logDir = null;

    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
       

        var prjectName = Project.Current.BrowseName;
        prjName = prjectName;

        isEmulate = IsEmulate(out runtimeDirIndo);

        vRefreshSource = LogicObject.GetVariable("RefreshSourceTrigger");
        vRefreshGrd = LogicObject.GetVariable("RefreshGridTrigger");

        vPageCount = LogicObject.GetVariable("PageCount");
        vPageIndex = LogicObject.GetVariable("PageIndex");
        vPageSize = LogicObject.GetVariable("PageSize");

        nSource = LogicObject.Get("Sources");
        nLogs = LogicObject.Get("Logs");


        var path = GetLogRootPath();
        if (!string.IsNullOrWhiteSpace(path))
        {
            logDir = new DirectoryInfo(path);
            var res = GetLogFiles(logDir);
            if (res != null)
            {
                BuildLogSourceModel(nSource, res);
            }
        }
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }


    List<FTOptixOutputLogInfo> LogInfos;



    #region UI Methods
    [ExportMethod]
    public void RefreshLog(string logfilepath)
    {
        var res = GetLogContent(logfilepath);
        if (res != null)
        {
            LogInfos = res.OrderBy(c=>c.RecordTime).ToList();
            LogInfos.Reverse();
            vPageCount.Value = CalcPageCount(res,vPageSize.Value);
            vPageIndex.Value = 1;


            BuildLogContentModel(nLogs, LogInfos, vPageIndex.Value,vPageSize.Value);
            vRefreshGrd.Value = !((bool)vRefreshGrd.Value);
        }

    }
    [ExportMethod]
    public void NextPage()
    {
        var index = vPageIndex.Value + 1;
        vPageIndex.Value = Math.Min(index,vPageCount.Value);

        BuildLogContentModel(nLogs, LogInfos, vPageIndex.Value, vPageSize.Value);
        vRefreshGrd.Value = !((bool)vRefreshGrd.Value);

    }
    [ExportMethod]
    public void LastPage()
    {
        var index = vPageIndex.Value - 1;
        vPageIndex.Value = Math.Max(index, 1);

        BuildLogContentModel(nLogs, LogInfos, vPageIndex.Value, vPageSize.Value);
        vRefreshGrd.Value = !((bool)vRefreshGrd.Value);
    }





    #endregion






    private int CalcPageCount(IEnumerable<FTOptixOutputLogInfo> infos,int pagecount)
    {
        return (int)(Math.Floor((decimal)(infos.LongCount() / pagecount)));
    }


    private IEnumerable<FileInfo> GetLogFiles(DirectoryInfo dirInfo,string filiter = "*.log")
    {
        return dirInfo.EnumerateFiles(filiter);
    }

    private void BuildLogSourceModel(IUANode parent,IEnumerable<FileInfo> fileInfos)
    {
        if (parent == null)
        {
            throw new ArgumentNullException(nameof(parent));
        }

        parent.ClearAll();

        foreach (FileInfo fileInfo in fileInfos)
        {
            var v = InformationModel.MakeVariable(Path.GetFileNameWithoutExtension(fileInfo.Name), OpcUa.DataTypes.String);
            v.Value = fileInfo.FullName;
            parent.Add(v);
        }

    }
    private string GetLogRootPath()
    {
        string path = string.Empty;
        if(isEmulate)
        {
            path = Path.Combine(runtimeDirIndo.FullName ,"Log",prjName);
            if (Directory.Exists(path))
            {
                return path;
            }
        }else
        {
            path = Path.Combine(runtimeDirIndo.FullName, "Log");
            if (Directory.Exists(path))
            {
                return path;
            }
        }
        return null;
    }
    
    private bool IsEmulate(out DirectoryInfo runtimePath)
    {
        var rUri = ResourceUri.FromApplicationRelativePath(string.Empty);
        var appPath = rUri.Uri;

        //Õ˘…œµπ3≤„
        var dir = new DirectoryInfo(appPath);
        for(var i = 0; i< 3;i++)
        {
            if(dir.Parent != null)
            {
                dir = dir.Parent;
            }
            else
            {
                Log.Error(this.GetType().Name, "µπ»˝≤„¥ÌŒÛ");
                throw new Exception("µπ»˝≤„¥ÌŒÛ");
            }
        }

        if(dir.Name == "Emulator")
        {
            runtimePath = dir;
            return true;
        }
        else
        {
            runtimePath = dir;
            return false;
        }




    }

    private IEnumerable<FTOptixOutputLogInfo> GetLogContent(string filepath)
    {
        if(!File.Exists(filepath))
        {
            return null;
        }

        var culture = CultureInfo.CreateSpecificCulture("en-US");
        var infos = new List<FTOptixOutputLogInfo>();

        foreach(var line in File.ReadAllLines(filepath))
        {
            var info = new FTOptixOutputLogInfo();
            var cs = line.Split(";");
            //if(DateTime.TryParse(cs[0],culture,DateTimeStyles.None ,out var dt))
            if(DateTime.TryParseExact(cs[0],"dd-MM-yyyy HH:mm:ss.fff",CultureInfo.InvariantCulture,DateTimeStyles.None,out var dt))
            {
                info.RecordTime = dt;
            }
            info.Contents.AddRange(cs.Skip(1).Take(cs.Length - 1));

            infos.Add(info);
        }

        return infos;





    }


    private void BuildLogContentModel(IUANode parent, List<FTOptixOutputLogInfo> infos,int pageindex,int pagesize)
    {
        parent.ClearAll();

        long sidx = Math.Max((pageindex - 1)*pagesize,0) ;
        var eidx = Math.Min(pageindex * pagesize,infos.LongCount());

        var idx = 0L;

        for (idx = sidx;idx < eidx; idx++){
            var info = infos[(int)idx];
        
            var n = InformationModel.MakeObject(idx.ToString());
            var v = InformationModel.MakeVariable("RecordTime", OpcUa.DataTypes.DateTime);
            v.Value = info.RecordTime;
            n.Add(v);


            v = InformationModel.MakeVariable("Messages", OpcUa.DataTypes.String);
            v.Value = string.Join("-", info.Contents);
            n.Add(v);
            parent.Add(n);
            idx++;
        }

       


    }

    class FTOptixOutputLogInfo
    {
        public DateTime RecordTime { get; set; }
        public List<string> Contents { get; set; } = new List<string>();
    }



}
