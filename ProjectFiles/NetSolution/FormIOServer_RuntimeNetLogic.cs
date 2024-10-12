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
using DotNetWebServer;
#endregion

using GCOptixToolkit;
using Newtonsoft.Json;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.IO;
using System.Data.Common;
using FormIOWebApplication;
public class FormIOServer_RuntimeNetLogic : BaseNetLogic
{
    WebApplication app;
    string RootPath;
    UInt16 Port;
    IUAVariable vError;
    SQLiteStore nStore;
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        if (!Owner.GetVariableValue<bool>("Enable"))
        {
            return;
        }
        nStore = Owner.GetAlias("Store") as SQLiteStore;

        vError = Owner.GetVariable("Error");

        Port = Convert.ToUInt16(Owner.GetVariableValue<int>("Port"));

        var root = Owner.GetVariableValue<string>("RootPath");

        var uri = ResourceUri.FromProjectRelativePath(root);
        RootPath = uri.Uri;


        app = new WebApplication(System.Net.IPAddress.Any, Port, "", RootPath);

        app.UseStaticFile();
        //var dbpath = Path.GetFullPath("formio_demo11.db");
        var repo = new RepositoryOptix(nStore);
        app.Services.AddSingleton<IRepository<FormSchema, FormSubmission>>(repo);

        app.MapController(new string[] { "FormIOWebApplication" });

        app.Start();



    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
        app.Stop();
    }


    class RepositoryOptix : IRepository<FormSchema, FormSubmission>
    {
        readonly SQLiteStore nStore;
        public RepositoryOptix(SQLiteStore store)
        {
            nStore = store;
        }
        public bool AddOrUpdateFormSchema(FormSchema schema)
        {
            var sql = $"select count(name) from FormSchemas where id = '{schema.name}'";
            var rows = ExecuteQueryWithResult(sql);
            var count = rows.Length;

            var is_add = Convert.ToInt32(rows[0, 0]) <= 0;

            schema.schema = schema.schema.Replace("\n", string.Empty);
            if (is_add)
            {
                //sql = @$"INSERT INTO FormSchemas (id,name,schema) VALUES ($id, $name, $schema);";
                //var ps = new Dictionary<string, object>();
                //ps.Add("$id", schema.id);
                //ps.Add("$name", schema.name);
                //ps.Add("$schema", schema.schema);

                var values = new object[1, 3];
                values[0,0] = schema.id;
                values[0,1] = schema.name;
                values[0,2] = schema.schema;

                nStore.Insert("FormSchemas", new string[] { "id", "name", "schema" }, values);

            }
            else
            {
                sql = @$"UPDATE FormSchemas
SET schema = '{schema.schema}'
WHERE name = '{schema.name}'";
                ExecuteQueryNoResult(sql);
            }
            return true;

            //throw new NotImplementedException();
        }

        public bool DeleteFormSchemaById(string id)
        {
            throw new NotImplementedException();
        }

        public string[] GetFormSchemaList()
        {
            var sql = "select name from FormSchemas";
            var result = ExecuteQueryWithResult(sql);
            var r = new List<string>();
            var count = result.Length;
            for(var i = 0; i < count; i++)
            {

                r.Add(result[i,0].ToString());
            }
            return r.ToArray();
            //throw new NotImplementedException();
        }

        public bool GetFormSchemaObjectById(string id, out FormSchema schema)
        {
            var sql = $"select schema,name from FormSchemas where name = '{id}'";
            var result = ExecuteQueryWithResult(sql);
            var count = result.Length;
            if (count == 0)
            {
                schema = null;
                return false;
            }
            var s = result[0,0] == null ? "{}" : result[0,0].ToString();
            schema = new FormSchema()
            {
                id = id,
                name = result[0,1].ToString(),
                schema = s
            };
            return true;
        }

        public string GetFormSchemaStringById(string id)
        {

            var sql = $"select schema from FormSchemas where name = '{id}'";
            var result = ExecuteQueryWithResult(sql);
            var count = result.Length;
            var schema = count == 0 ? "{}" : result[0,0].ToString();
            return schema;
            throw new NotImplementedException();
        }

        public bool SaveFormSubmission(string schema_id, string data)
        {

            //var sql = @$"INSERT INTO FormSubmissions (id,data,schema_id,localtime,timestamp) VALUES ($id, $data, $schema_id,$lt,$ts);";
            var dt = DateTime.Now;
            //var ps = new Dictionary<string, object>();
            //ps.Add("$id", Guid.NewGuid().ToString());
            //ps.Add("$data", data);
            //ps.Add("$schema_id", schema_id);
            //ps.Add("$lt", dt.ToLocalTime());
            //ps.Add("$ts", dt.ToUniversalTime());

            var values = new object[1, 5];
            values[0,0] = Guid.NewGuid().ToString();
            values[0, 1] = data;
            values[0,2] = schema_id;
            values[0,3] = dt.ToLocalTime();
            values[0,4] = dt.ToUniversalTime();

            nStore.Insert("FormSubmissions", new string[] { "id", "data", "schema_id", "localtime", "timestamp" }
            ,values
                );

            //ExecuteQueryNoResult(sql, ps);
            return true;


            throw new NotImplementedException();
        }


        private bool ExecuteQueryNoResult(string sql)
        {

            nStore.Query(sql,out var header,out var result);

            return true;
        }

        private object[,] ExecuteQueryWithResult(string sql)
        {


            nStore.Query(sql, out var header, out var result);


            return result;

        }

    }

}




namespace FormIOWebApplication
{
    /*
    public class RepositorySQLite : IRepository<FormSchema, FormSubmission>
    {
        readonly string connectionString;
        public RepositorySQLite(string filepath)
        {

            connectionString = $"Data Source={filepath}";

            if (File.Exists(filepath))
            {
                CheckAndFixTableInfo();
            }
            else
            {
                using (var connection = new SqliteConnection(connectionString))
                {
                    connection.Open();

                    var command = connection.CreateCommand();
                    command.CommandText = @"CREATE TABLE ""FormSchemas"" (
	""name""	TEXT NOT NULL,
	""id""	TEXT,
	""schema""	TEXT,
	PRIMARY KEY(""name"")
);
";

                    command.ExecuteNonQuery();


                    command.CommandText = @"CREATE TABLE ""FormSubmissions"" (
	""id""	TEXT NOT NULL,
	""data""	TEXT,
	""schema_id""	TEXT,
	""localtime""	NUMERIC,
	""timestamp""	NUMERIC,
	PRIMARY KEY(""id"")
);";
                    command.ExecuteNonQuery();

                }
            }

        }


        private bool CheckAndFixTableInfo()
        {
            var sql = "select name from sqlite_master where type='table';";
            var rows = ExecuteQueryWithResult(sql);
            var names = new List<string>();
            foreach (var row in rows)
            {
                var name = row[0].ToString();
                names.Add(name);
            }

            var b1 = names.Exists(n => n == "FormSchemas");
            var b2 = names.Exists(n => n == "FormSubmissions");
            if (!b1)
            {
                var sql1 = @"CREATE TABLE ""FormSchemas"" (
	""name""	TEXT NOT NULL,
	""id""	TEXT,
	""schema""	TEXT,
	PRIMARY KEY(""name"")
);
";
                ExecuteQueryNoResult(sql1);
            }


            if (!b2)
            {
                var sql2 = @"CREATE TABLE ""FormSubmissions"" (
	""id""	TEXT NOT NULL,
	""data""	TEXT,
	""schema_id""	TEXT,
	""localtime""	NUMERIC,
	""timestamp""	NUMERIC,
	PRIMARY KEY(""id"")
);";
                ExecuteQueryNoResult(sql2);
            }


            return true;
        }

        public bool AddOrUpdateFormSchema(FormSchema schema)
        {
            //throw new NotImplementedException();

            var sql = $"select count(name) from FormSchemas where id = '{schema.name}'";
            var result = ExecuteQueryWithResult(sql);
            var r = result.FirstOrDefault();
            var count = r == null ? 0 : Convert.ToInt32(r[0]);
            if (count <= 0)
            {
                schema.schema = schema.schema.Replace("\n", string.Empty);
                sql = @$"INSERT INTO FormSchemas (id,name,schema) VALUES ($id, $name, $schema);";
                var ps = new Dictionary<string, object>();
                ps.Add("$id", schema.id);
                ps.Add("$name", schema.name);
                ps.Add("$schema", schema.schema);
                ExecuteQueryNoResult(sql, ps);
            }
            else
            {
                schema.schema = schema.schema.Replace("\n", string.Empty);
                sql = @$"UPDATE FormSchemas
SET schema = '{schema.schema}'
WHERE name = '{schema.name}';";
                ExecuteQueryNoResult(sql);
            }
            return true;

        }

        public bool DeleteFormSchemaById(string id)
        {
            throw new NotImplementedException();
        }

        public string[] GetFormSchemaList()
        {

            var sql = "select name from FormSchemas";
            var result = ExecuteQueryWithResult(sql);
            var r = new List<string>();
            foreach (var row in result)
            {
                r.Add(row[0].ToString());
            }
            return r.ToArray();
            //throw new NotImplementedException();

        }

        public bool GetFormSchemaObjectById(string id, out FormSchema schema)
        {

            var sql = $"select schema,name from FormSchemas where name = '{id}'";
            var result = ExecuteQueryWithResult(sql);
            var r = result.FirstOrDefault();
            if (r == null)
            {
                schema = null;
                return false;
            }
            var s = r == null ? "{}" : r[0].ToString();
            schema = new FormSchema()
            {
                id = id,
                name = r[1].ToString(),
                schema = s
            };
            return true;
        }

        public string GetFormSchemaStringById(string id)
        {

            var sql = $"select schema from FormSchemas where name = '{id}'";
            var result = ExecuteQueryWithResult(sql);
            var r = result.FirstOrDefault();
            var schema = r == null ? "{}" : r[0].ToString();
            return schema;



            throw new NotImplementedException();
        }

        public bool SaveFormSubmission(string schema_id, string data)
        {
            var sql = @$"INSERT INTO FormSubmissions (id,data,schema_id,localtime,timestamp) VALUES ($id, $data, $schema_id,$lt,$ts);";
            var dt = DateTime.Now;
            var ps = new Dictionary<string, object>();
            ps.Add("$id", Guid.NewGuid().ToString());
            ps.Add("$data", data);
            ps.Add("$schema_id", schema_id);
            ps.Add("$lt", dt.ToLocalTime());
            ps.Add("$ts", dt.ToUniversalTime());
            ExecuteQueryNoResult(sql, ps);
            return true;
            //throw new NotImplementedException();
        }

        private void ExecuteQueryNoResult(string sql)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = sql;


                command.ExecuteNonQuery();

            }

        }

        private void ExecuteQueryNoResult(string sqlpattern, Dictionary<string, object> parameters)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = sqlpattern;
                if (parameters != null)
                {
                    foreach (var kv in parameters)
                    {
                        command.Parameters.AddWithValue(kv.Key, kv.Value);
                    }
                }


                command.ExecuteNonQuery();

            }

        }

        private List<object[]> ExecuteQueryWithResult(string sql)
        {

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = sql;


                using (var reader = command.ExecuteReader())
                {

                    var columnCount = reader.GetColumnSchema().Count;
                    var result = new List<object[]>();

                    if (reader.HasRows)
                    {

                        while (reader.Read())
                        {
                            var values = new object[columnCount];
                            var c = reader.GetValues(values);
                            result.Add(values);


                        }
                        return result;
                    }
                    else
                    {
                        return result;
                    }
                }
            }

        }



    }

    */


    class ExecResultDto
    {
        public bool success { get; set; }
        public string errMsg { get; set; }
    }

    class FormSchemaDto
    {
        public string id { get; set; }
        public string name { get; set; }
        public string schema { get; set; }
    }

    class ServerConfigure
    {

    }

    public class FormSchema
    {
        public string id { get; set; }
        public string name { get; set; }
        public string schema { get; set; }
    }

    public class FormSubmission
    {
        public string id { get; set; }
        public string data { get; set; }
        public string schema_id { get; set; }
        public DateTime recordtime { get; set; }
        public DateTime timestamp { get; set; }
    }

    public interface IRepository<TD, TF> where TD : class where TF : class
    {

        string[] GetFormSchemaList();

        string GetFormSchemaStringById(string id);

        bool GetFormSchemaObjectById(string id, out TD schema);

        bool AddOrUpdateFormSchema(TD schema);

        bool DeleteFormSchemaById(string id);

        bool SaveFormSubmission(string schema_id, string data);

    }

    public class ApiController : HttpController
    {
        readonly IRepository<FormSchema, FormSubmission> repository;
        public ApiController(IRepository<FormSchema, FormSubmission> repo)
        {
            this.repository = repo;
        }


        [HttpMethod(HttpMethodType.POST)]
        public IResult GetSchemaList()
        {
            var result = this.repository.GetFormSchemaList();
            if (result == null)
            {
                return new JsonResult(new string[] { });
            }
            else
            {
                return new JsonResult(result);
            }
        }


        /// <summary>
        /// 保存设计
        /// </summary>
        /// <returns></returns>
        [HttpMethod(HttpMethodType.POST)]
        public IResult SaveSchema()
        {

            var body = Request.Body;
            var dto = JsonConvert.DeserializeObject<FormSchemaDto>(body);
            var schema = new FormSchema() { id = dto.id, name = dto.name, schema = dto.schema };
            var result = repository.AddOrUpdateFormSchema(schema);
            if (result)
            {
                return new JsonResult(new ExecResultDto() { success = true, errMsg = string.Empty });
            }
            else
            {
                return new JsonResult(new ExecResultDto() { success = false, errMsg = "save schema error" });
            }
        }


        /// <summary>
        /// 打开设计
        /// </summary>
        /// <returns></returns>
        [HttpMethod(HttpMethodType.GET)]
        public IResult GetSchema()
        {
            var id = QueryParameters["id"];
            if (!string.IsNullOrWhiteSpace(id))
            {
                var result = this.repository.GetFormSchemaObjectById(id, out var schema);
                if (result)
                {
                    return new TextResult(schema.schema);
                }
                else
                {
                    return new JsonResult(new { });
                }
            }
            else
            {
                return new JsonResult(new { });
            }
        }



        [HttpMethod(HttpMethodType.POST)]
        public IResult Submission()
        {
            var body = Request.Body;
            Console.WriteLine("Recive the form data");
            Console.WriteLine($"{body.ToString()}");

            var headerCount = Request.Headers;
            var schema_id = string.Empty;
            for (var i = 0; i < headerCount; i++)
            {
                (var key, var value) = Request.Header(i);
                if (key == "id")
                {
                    schema_id = value;
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(schema_id))
            {
                Console.WriteLine("error , submission schemid missing");
            }
            else
            {

                this.repository.SaveFormSubmission(schema_id, body);
            }

            return new TextResult(body);
        }
    }
}

