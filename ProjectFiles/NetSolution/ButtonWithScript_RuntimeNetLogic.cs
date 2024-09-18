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
#endregion
using CSScriptLib;
using ExternalScript;

using OEM_Template_V1;
public class ButtonWithScript_RuntimeNetLogic : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started

        var rUri = new ResourceUri(Owner.GetVariable("ScriptPath").Value);

        var path = rUri.Uri;
        if (File.Exists(path))
        {
            var code = File.ReadAllText(path);
            Owner.SetVariableValue("Script", code);
            Owner.SetVariableValue("ActualScriptPath", path);
            if (Eval(code))
            {

            }else
            {

            }
        }

      
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }


    [ExportMethod]
    public void Save()
    {
        var path = Owner.GetVariableValue<string>("ActualScriptPath",string.Empty);
        var code = Owner.GetVariableValue<string>("Script", string.Empty);
        File.WriteAllText(path, code);

    }


    IScriptExecuter script;
    private bool Eval(string code)
    {
        try
        {
            var _code = BuildCode(code);

            script = CSScript.Evaluator
                .ReferenceAssemblyByName("System")
                .ReferenceAssemblyByNamespace("ExternalScript")
                .ReferenceAssemblyByNamespace("UAManagedCore")
                .ReferenceAssemblyByNamespace("FTOptix.UI")
                .ReferenceAssemblyOf(this)
                .ReferenceDomainAssemblies()
                           .LoadCode<IScriptExecuter>(_code);
           
           return true;
        }
        catch (Exception ex)
        {
            script = null;
            Log.Warning(GetType().Name, ex.Message);
            return false;
        }
    }

    [ExportMethod]
    public void Execute(out string result)
    {
        if(script == null)
        {
            Log.Info(GetType().Name, "Script is Error");
            result = null;
            return;
        }


        Log.Info(GetType().Name,"Execute");

        try
        {
            
            var v = script.Exec(Owner, LogicObject);
            Log.Info(GetType().Name, v);
            result = v;
        }
        catch(Exception ex)
        {
            Log.Warning(GetType().Name, ex.Message);
            result = string.Empty;
        }
      

    }



    private string BuildCode(string code)
    {
        
        var r = $@"using System;
using ExternalScript;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;
using FTOptix.EventLogger;

public class Script : IScriptExecuter
{{
    public string Exec(IUANode Owner,IUAObject LogicObject)
    {{
        {code}
    }}
}}";

        return r;
    }
   
}


namespace ExternalScript{
     public interface IScriptExecuter
    {
        string Exec(IUANode Owner,IUAObject LogicObject);
    }
}
