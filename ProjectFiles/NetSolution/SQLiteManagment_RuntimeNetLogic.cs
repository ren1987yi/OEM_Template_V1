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
using GCOptixToolkit;
using System.IO;
using OEM_Template_V1;
#endregion

public class SQLiteManagment_RuntimeNetLogic : BaseNetLogic
{
    string bckFolder;
    SQLiteStore nStore;
    string bckNameRule;

    IUAVariable DBFilePath, DBSize, DBInfo;

    IUANode nbackups;


    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started

        bckFolder = Owner.GetVariableValue<string>("BackupDirectory");
        if(string.IsNullOrWhiteSpace(bckFolder))
        {
            bckFolder = string.Empty;
        }
        bckNameRule = Owner.GetVariableValue<string>("BackupNameRule");

        nStore = InformationModel.Get((NodeId)Owner.GetVariable("Store").Value) as SQLiteStore;

        DBFilePath = LogicObject.GetVariable(nameof(DBFilePath));
        DBSize = LogicObject.GetVariable(nameof(DBSize));
        DBInfo = LogicObject.GetVariable(nameof(DBInfo));

        nbackups = LogicObject.Get("Backups");
        nbackups.ClearAll();

        GetStoreInfo();
        GetBakcups();
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }


    #region UI Methods
    [ExportMethod]
    public void BackUp()
    {
        if(nStore != null)
        {
            //nStore.Backup();
            //var rUri = ResourceUri.FromApplicationRelativePath(this.bckFolder);
            //var path = rUri.Uri;
            //if (!IsDirectory(path))
            //{
            //    path = Path.GetDirectoryName(path);
            //}

            var path = Path.Combine(this.bckFolder, string.Format("{0:yyyy_MM_dd_HH_mm_ss}.sqlite",DateTime.Now));
            Log.Info("Store", $"Backup path:{path}");
            var rUri = ResourceUri.FromApplicationRelativePath(path);
            if (nStore.Backup(rUri))
            {
                Log.Info("Store", "Backup ok");
            }
            else
            {
                Log.Info("Store", "Backup faile");
            }
            GetBakcups();
        }
    }

    [ExportMethod]
    public void Delete(NodeId item)
    {
        var n = InformationModel.Get(item) as IUAVariable;
        if(n != null)
        {
            var path = n.Value;
            File.Delete(path);
        }

        GetBakcups();
    }
    [ExportMethod]
    public void Clear()
    {
        ClearDB();
    }

    #endregion



    private void ClearDB()
    {

    }

    private void GetStoreInfo()
    {
        if(nStore == null)
        {
            return;
        }

        var name = nStore.Filename;

        var rUri = ResourceUri.FromApplicationRelativePath(name);
        var filepath = rUri.Uri + ".sqlite";

        DBFilePath.Value = filepath;

        if(File.Exists(filepath))
        {
            var info = new FileInfo(filepath);
            if(info != null)
            {
                var size = Convert.ToInt32(info.Length / 1024 / 1024);
                if(size <= 0)
                {
                    size = 1;
                }
                DBSize.Value = size;

                var ss = @$"
Name:{info.Name}
Directory:{info.DirectoryName}
Create Time:{info.CreationTime}
";
                DBInfo.Value = ss;

            }
        }


    }


    private void GetBakcups()
    {
        nbackups.ClearAll();

        var rUri = ResourceUri.FromApplicationRelativePath(this.bckFolder);
        var path = rUri.Uri;
        if (!IsDirectory(path))
        {
            path = Path.GetDirectoryName(path);
        }

        if (Directory.Exists(path))
        {
            var files = Directory.EnumerateFiles(path, "*.sqlite");
            foreach (var item in files)
            {
                var _name = Path.GetFileNameWithoutExtension(item);
                if(_name == nStore.Filename)
                {
                    continue;
                }
                var v = InformationModel.MakeVariable(Path.GetFileNameWithoutExtension(item),OpcUa.DataTypes.String);
                v.Value = item;
                nbackups.Add(v);
            }
        }

    }

    bool IsDirectory(string f)
    {
        if ((System.IO.File.GetAttributes(f) & FileAttributes.Directory) == FileAttributes.Directory)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
