#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.SQLiteStore;
using FTOptix.HMIProject;
using FTOptix.NativeUI;
using FTOptix.WebUI;
using FTOptix.Store;
using FTOptix.Recipe;
using FTOptix.System;
using FTOptix.NetLogic;
using FTOptix.Retentivity;
using FTOptix.UI;
using FTOptix.Alarm;
using FTOptix.CoreBase;
using FTOptix.Core;
#endregion
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics.Contracts;
using System.Linq;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;
using FTOptix.EventLogger;
public class FileExplorer_RuntimeNetLogic : BaseNetLogic
{
    IUANode nInfos;
    //IUANode nRoots;
    IUAVariable uCurrentFolder = null;



    List<string> folders = new List<string>();//current open folders



    _TempFileInfo tempFileInfo;

    string RootPath = string.Empty;




    string CurrentFolder
    {
        get
        {
            var _path = string.Join(Path.DirectorySeparatorChar, folders);
            uCurrentFolder.Value = Path.DirectorySeparatorChar +  _path;
            return Path.Combine(RootPath, _path);
        }
    }
    const int ROOT_PATH_COUNT = 10;

    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started

        tempFileInfo = null;
        //nRoots = LogicObject.Get("Roots");
        //for (int i = 0;i < ROOT_PATH_COUNT; i++)
        //{
        //    var v = Owner.GetVariable($"RootPath{i+1}");
        //    if (v == null)
        //    {
        //        continue;
        //    }

        //    var rpath = v.Value.Value as string;
        //    if (string.IsNullOrWhiteSpace(rpath))
        //    {
        //        continue;
        //    }


        //    var _rootpath = GetFileSystemPath(rpath);
        //    if (string.IsNullOrWhiteSpace(_rootpath))
        //    {
        //        continue;
        //    }
        //    Log.Info(_rootpath);

        //    var _name = v.BrowseName;
        //    var nv = InformationModel.MakeVariable(_name,OpcUa.DataTypes.String);
        //    nv.Value = _rootpath;
        //    nRoots.Add(nv);
        //}

        foreach(var v in Owner.Children.OfType<IUAVariable>())
        {
            if (v.BrowseName.StartsWith("RootPath"))
            {
                string path = v.Value;
                var _rootpath = GetFileSystemPath(path);
                if (string.IsNullOrWhiteSpace(_rootpath))
                {
                    continue;
                }
                RootPath = _rootpath;
                break;
            }
        }
        //var v1 = nRoots.Children.FirstOrDefault() as IUAVariable;
        //RootPath = v1.Value;



        //v1 = LogicObject.GetVariable("RefreshTrigger");
        //if (v1 == null)
        //{
        //    throw new Exception();
        //}

        //v1.Value = 1; 

     

      



        nInfos = Owner.GetObject("Infos");
        uCurrentFolder = LogicObject.GetVariable("CurrentFolder");
        RefreshCurrentFolder(CurrentFolder);
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }


    #region UI Methods

    [ExportMethod]
    public void ChangeWorkFolder(string folder)
    {
        RootPath = string.Empty;
        this.folders.Clear();
        string path = folder;
        var _rootpath = GetFileSystemPath(path);
        if (string.IsNullOrWhiteSpace(_rootpath))
        {
            return;
        }
        RootPath = _rootpath;
        RefreshCurrentFolder(CurrentFolder);
    }


    [ExportMethod]
    public void GoUp()
    {
        if(folders.Count > 0)
        {
            folders.RemoveAt(folders.Count - 1);
            RefreshCurrentFolder(CurrentFolder);
        }



    }


    [ExportMethod]
    public void Refresh()
    {
        RefreshCurrentFolder(CurrentFolder);
    }

    [ExportMethod]
    public void Cut(NodeId itemId)
    {
        var item = GetFileItem(itemId);
        this.tempFileInfo = CreateTempFileInfo(item);
        if (this.tempFileInfo != null)
        {
            tempFileInfo.Mode = FileOperateModeType.Cut;
        }
    }


    [ExportMethod]
    public void Copy(NodeId itemId)
    {

        var item = GetFileItem(itemId);
        this.tempFileInfo = CreateTempFileInfo(item);
        if(this.tempFileInfo != null)
        {
            tempFileInfo.Mode = FileOperateModeType.Copy;
        }
    }


    [ExportMethod]
    public void Paste()
    {
        if(this.tempFileInfo != null)
        {
            
            switch (tempFileInfo.Mode)
            {
                case FileOperateModeType.Cut:
                    CutFileSystemEntry(tempFileInfo.FullPath, CurrentFolder);
                    break;

                case FileOperateModeType.Copy:
                    CopyFileSystemEntry(tempFileInfo.FullPath, CurrentFolder);
                    break;


                default:
                    break;
            }


            tempFileInfo = null;
            RefreshCurrentFolder(CurrentFolder);
        }
    }


    [ExportMethod]
    public void Delete(NodeId itemId)
    {
        var item = GetFileItem(itemId);
        var path = GetVariableValue<string>(item, "FullPath", string.Empty);
        DeleteFileSystemEntry(path);

        RefreshCurrentFolder(CurrentFolder);
    }

    [ExportMethod]
    public void EnterFolder(NodeId itemId)
    {
        var item = InformationModel.Get(itemId);
        if (item != null)
        {
            var v = item.GetVariable("Type");
            if (v != null)
            {
                if((int)v.Value == 1)
                {
                    //v = item.GetVariable("FullPath");
                    v = item.GetVariable("Name");
                    folders.Add(v.Value);
                    //TODO º”»Î¬∑æ∂path ui
                    Refresh();
                }
            }
        }
    }

    [ExportMethod]
    public void GoHome()
    {
        folders.Clear();
        RefreshCurrentFolder(CurrentFolder);
    }


    #endregion

    private IUANode GetFileItem(NodeId itemId)
    {
        return InformationModel.Get(itemId);
        
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
    
    private void DeleteFileSystemEntry(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }
        path = Path.GetFullPath(path);
        if(IsDirectory(path))
        {
            if (Directory.Exists(path))
            {
                try
                {
                    Directory.Delete(path, true);

                }catch(Exception ex)
                {
                    Log.Error(ex.Message);
                }
            }
        }
        else
        {
            if (System.IO.File.Exists(path))
            {
                try
                {
                    System.IO.File.Delete(path);

                }catch(Exception ex)
                {
                    Log.Error(ex.Message);
                }
            }
        }
    }



    private void CutFileSystemEntry(string srcpath,string targetFolder)
    {

        if (string.IsNullOrWhiteSpace(srcpath) || string.IsNullOrWhiteSpace(targetFolder))
        {
            return;
        }

        if (!Directory.Exists(targetFolder))
        {
            return;
        }

        var path = Path.GetFullPath(srcpath);
        if (IsDirectory(path))
        {

            var new_path = GetNewFolderPath(path,targetFolder);
            if(path != new_path)
            {

                Directory.Move(path, new_path);
            }
        }
        else
        {
            var new_path = GetNewFilePath(path, targetFolder);
            if (path != new_path)
            {

                System.IO.File.Move(path, new_path);
            }
        }
    }


    private void CopyFileSystemEntry(string srcpath, string targetFolder)
    {
        if (string.IsNullOrWhiteSpace(srcpath) || string.IsNullOrWhiteSpace(targetFolder))
        {
            return;
        }

        if (!Directory.Exists(targetFolder))
        {
            return;
        }

        var path = Path.GetFullPath(srcpath);
        if (IsDirectory(path))
        {

            var new_path = GetNewFolderPath(path, targetFolder);
            if (path != new_path)
            {

                CopyDirectory(path,new_path,true);
            }
        }
        else
        {
            var new_path = GetNewFilePath(path, targetFolder);
            if (path != new_path)
            {

                System.IO.File.Copy(path, new_path);
            }
        }
    }

    private string GetNewFilePath(string srcpath,string targetDir)
    {
        var name = System.IO.Path.GetFileName(srcpath);
        return Path.Combine(targetDir, name);
    }

    private string GetNewFolderPath(string srcpath,string targetDir)
    {
        var name = Path.GetDirectoryName(srcpath);
        return Path.Combine(targetDir, name);
    }


    void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
    {
        // Get information about the source directory
        var dir = new DirectoryInfo(sourceDir);

        // Check if the source directory exists
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        // Cache directories before we start copying
        DirectoryInfo[] dirs = dir.GetDirectories();

        // Create the destination directory
        Directory.CreateDirectory(destinationDir);

        // Get the files in the source directory and copy to the destination directory
        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath);
        }

        // If recursive and copying subdirectories, recursively call this method
        if (recursive)
        {
            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
            }
        }
    }

    private void RefreshCurrentFolder(string folder)
    {
        
        var lt = new List<_FileInfos>();

        if (Directory.Exists(folder))
        {

            var fs = Directory.EnumerateFileSystemEntries(folder);
            foreach (var f in fs)
            {
                if ((System.IO.File.GetAttributes(f) & FileAttributes.Directory)== FileAttributes.Directory)
                {
                    lt.Add(CreateFolderItem(f));
                }
                else
                {
                    lt.Add(CreateFileItem(f));
                }

            }

        }
      


        foreach(var item in nInfos.Children)
        {
            item.Delete();
        }


        foreach(var item in lt)
        {
            try
            {

           
                var obj = InformationModel.MakeObject(item.Name);
                if(item.IsFile)
                {
                    var info = item.FileInfo;

                    var v = InformationModel.MakeVariable("Type",OpcUa.DataTypes.Int32);
                    v.Value = 0;
                    obj.Add(v);


                    v = InformationModel.MakeVariable("Name",OpcUa.DataTypes.String);
                    v.Value = item.Name;
                    obj.Add(v);

                    v = InformationModel.MakeVariable("Time", OpcUa.DataTypes.DateTime);
                    v.Value = info.LastAccessTime;
                    obj.Add(v);


                    v = InformationModel.MakeVariable("FullPath", OpcUa.DataTypes.String);
                    v.Value = info.FullName;
                    obj.Add(v);
                }
                else
                {
                    var info = item.DirInfo;
                    var v = InformationModel.MakeVariable("Type", OpcUa.DataTypes.Int32);
                    v.Value = 1;
                    obj.Add(v);


                    v = InformationModel.MakeVariable("Name", OpcUa.DataTypes.String);
                    v.Value = item.Name;
                    obj.Add(v);

                    v = InformationModel.MakeVariable("Time", OpcUa.DataTypes.DateTime);
                    v.Value = info.LastAccessTime;
                    obj.Add(v);

                    v = InformationModel.MakeVariable("FullPath", OpcUa.DataTypes.String);
                    v.Value = info.FullName;
                    obj.Add(v);
                }
                nInfos.Add(obj);
            }
            catch (Exception ex)
            {

            }

        }

    }


    private _FileInfos CreateFileItem(string path)
    {
        var f = new FileInfo(path);

        return new _FileInfos()
        {
            IsFile = true,
            Name = f.Name,
            FileInfo = f
        };

    }

    private _FileInfos CreateFolderItem(string path)
    {
        var f = new DirectoryInfo(path);
        return new _FileInfos()
        {
            IsFile = false,
            Name = f.Name,
            DirInfo = f
        };
    }
    private _TempFileInfo CreateTempFileInfo(IUANode item)
    {
        if(item == null)
        {
            return null;
        }
        else
        {
            //var type = GetVariableValue<int>(item, "Type",-1);

            var fullpath = GetVariableValue(item, "FullPath", string.Empty);

            return new _TempFileInfo() { Mode = FileOperateModeType.None, FullPath = fullpath };

        }
    }
    private T GetVariableValue<T>(IUANode node,string name,T defaultValue)
    {
        if(node == null)
        {
            return defaultValue;
        }
        else
        {
            var v = node.GetVariable(name);
            if(v == null)
            {
                return defaultValue;
            }
            else
            {
                return (T)v.Value.Value;
            }
        }
    }





    class _FileInfos
    {
        public string Name { get;set; }
        public bool IsFile { get; set; }
        public FileInfo FileInfo { get; set; }
        public DirectoryInfo DirInfo { get; set; }
    }


    class _TempFileInfo
    {
        public FileOperateModeType Mode { get; set; }   
        public string FullPath { get; set; }
    }

    enum FileOperateModeType
    {
        None,
        Cut,
        Copy
    }



    string GetFileSystemPath(string value)
    {
        try
        {
            var a = new ResourceUri(value);


            string path = string.Empty;
            switch (a.UriType)
            {
                case UriType.ApplicationRelative:
                    path = a.Uri;
                    break;
                case UriType.ProjectRelative:
                    path = a.Uri;
                    break;
                case UriType.USBRelative:
                    path = a.Uri;
                    break;
                case UriType.AbsoluteFilePath:

                    path = a.Uri;
                    break;
                default: path = value; break;
            }

            if (IsDirectory(path))
            {
                return Path.GetFullPath(path);
            }
            else
            {
                var root = Path.GetDirectoryName(path);
                return root;
            }

        }
        catch (Exception ex)
        {
            return string.Empty;
            
        }
        

        //if (value.StartsWith(ABSOLUT_PATH_PREFIX))
        //{
        //    var path = value.Substring(ABSOLUT_PATH_PREFIX.Length);
        //    if (IsDirectory(path))
        //    {
        //        return Path.GetFullPath(path);
        //    }
        //    else
        //    {
        //        var root = Path.GetPathRoot(path);
        //        return Path.GetFullPath(root);
        //    }
        //}else if (value.StartsWith(PROJECT_PATH_PREFIX))
        //{
        //    var path = value.Substring(PROJECT_PATH_PREFIX.Length);
        //    var rUri = ResourceUri.FromProjectRelativePath(path);
        //    path = Path.GetFullPath(rUri.Uri);

        //    if (IsDirectory(path))
        //    {
        //        return Path.GetFullPath(path);
        //    }
        //    else
        //    {
        //        var root = Path.GetPathRoot(path);
        //        return Path.GetFullPath(root);
        //    }
        //}
        //else if (value.StartsWith(APPLICATION_PATH_PREFIX))
        //{
        //    var path = value.Substring(APPLICATION_PATH_PREFIX.Length);
        //    var rUri = ResourceUri.FromProjectRelativePath(path);
        //    path = Path.GetFullPath(rUri.Uri);

        //    if (IsDirectory(path))
        //    {
        //        return Path.GetFullPath(path);
        //    }
        //    else
        //    {
        //        var root = Path.GetPathRoot(path);
        //        return Path.GetFullPath(root);
        //    }
        //}
        //else
        //{
        //    return null;
        //}
    }
}
