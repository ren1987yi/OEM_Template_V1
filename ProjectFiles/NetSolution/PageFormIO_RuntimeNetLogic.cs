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
#endregion

public class PageFormIO_RuntimeNetLogic : BaseNetLogic
{
    SQLiteStore nStore;
    IUANode nSchemas;
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        nStore = Owner.GetAlias("Store") as SQLiteStore;
        nSchemas = LogicObject.GetObject("Schemas");
        
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }


    [ExportMethod]
    public void AddNewFormSchema(string name)
    {
        var sql = $"select count(name) from FormSchemas where id = '{name}'";
        var rows = ExecuteQueryWithResult(sql);
        var count = rows.Length;

        var is_add = Convert.ToInt32(rows[0, 0]) <= 0;

        
        if (is_add)
        {
            //sql = @$"INSERT INTO FormSchemas (id,name,schema) VALUES ($id, $name, $schema);";
            //var ps = new Dictionary<string, object>();
            //ps.Add("$id", schema.id);
            //ps.Add("$name", schema.name);
            //ps.Add("$schema", schema.schema);

            var values = new object[1, 3];
            values[0, 0] = name;
            values[0, 1] = name;
            values[0, 2] = "{}";

            nStore.Insert("FormSchemas", new string[] { "id", "name", "schema" }, values);

        }
      
    }


    private bool ExecuteQueryNoResult(string sql)
    {

        nStore.Query(sql, out var header, out var result);

        return true;
    }

    private object[,] ExecuteQueryWithResult(string sql)
    {


        nStore.Query(sql, out var header, out var result);


        return result;

    }
}
