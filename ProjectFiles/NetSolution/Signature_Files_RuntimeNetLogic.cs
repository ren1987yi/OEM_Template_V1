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
using System.IO;
#endregion

public class Signature_Files_RuntimeNetLogic : BaseNetLogic
{
    FileSystemWatcher watcher;
    IUANode nFiles;
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        var uri = ResourceUri.FromProjectRelativePath("signature_images");
        var spath = uri.Uri;
        Init(spath);

        watcher = new FileSystemWatcher(spath);

        watcher.NotifyFilter = NotifyFilters.Attributes
                             | NotifyFilters.CreationTime
                             | NotifyFilters.DirectoryName
                             | NotifyFilters.FileName
                             | NotifyFilters.LastAccess
                             | NotifyFilters.LastWrite
                             | NotifyFilters.Security
                             | NotifyFilters.Size;

        watcher.Changed += OnChanged;
        watcher.Created += OnCreated;
        watcher.Deleted += OnDeleted;
        watcher.Renamed += OnRenamed;
        watcher.Error += OnError;

        watcher.Filter = "*.*";
        watcher.IncludeSubdirectories = true;
        watcher.EnableRaisingEvents = true;



        nFiles = LogicObject.Get("Files");








    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }



    private void Init(string folder)
    {
        var f = new DirectoryInfo(folder);
        var finfos = f.EnumerateFiles();

        foreach (var finfo in finfos)
        {
            var v = InformationModel.MakeVariable(finfo.Name, OpcUa.DataTypes.String);
            v.Value = finfo.FullName;
            nFiles.Add(v);
        }

    }

    private  void OnChanged(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType != WatcherChangeTypes.Changed)
        {
            return;
        }
        Console.WriteLine($"Changed: {e.FullPath}");
    }

    private  void OnCreated(object sender, FileSystemEventArgs e)
    {
        string value = $"Created: {e.FullPath}";
        Console.WriteLine(value);


        var v = InformationModel.MakeVariable(e.Name, OpcUa.DataTypes.String);
        v.Value = e.FullPath;
        nFiles.Add(v);
    }

    private void OnDeleted(object sender, FileSystemEventArgs e) {

       
        Console.WriteLine($"Deleted: {e.FullPath}");
    }

    private  void OnRenamed(object sender, RenamedEventArgs e)
    {
        Console.WriteLine($"Renamed:");
        Console.WriteLine($"    Old: {e.OldFullPath}");
        Console.WriteLine($"    New: {e.FullPath}");
    }


    private  void OnError(object sender, ErrorEventArgs e) =>
          PrintException(e.GetException());

    private  void PrintException(Exception? ex)
    {
        if (ex != null)
        {
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine("Stacktrace:");
            Console.WriteLine(ex.StackTrace);
            Console.WriteLine();
            PrintException(ex.InnerException);
        }
    }
}
