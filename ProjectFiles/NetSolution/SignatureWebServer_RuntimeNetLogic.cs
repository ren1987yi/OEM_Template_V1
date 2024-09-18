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
using GCOptixToolkit;
using Newtonsoft.Json;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using OptixWebServer.Signature;
#endregion

public class SignatureWebServer_RuntimeNetLogic : BaseNetLogic
{
    WebApplication app;
    string RootPath;
    UInt16 Port;
    IUAVariable vError;
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        if (!Owner.GetVariableValue<bool>("Enable"))
        {
            return;
        }


        vError = Owner.GetVariable("Error");

        Port = Convert.ToUInt16(Owner.GetVariableValue<int>("Port"));

        var root = Owner.GetVariableValue<string>("RootPath");

        var uri = ResourceUri.FromProjectRelativePath(root);
        RootPath = uri.Uri;


        uri = ResourceUri.FromProjectRelativePath("signature_images");
        var spath = uri.Uri;

        app = new WebApplication(System.Net.IPAddress.Any,Port,"",RootPath);

        app.Services.AddSingleton(new Configure()
        {
            StoragePath = spath,
        });


        app.MapController(new string[] { "OptixWebServer.Signature" });


        app.Start();

    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
        app.Stop();
    }

    

}


namespace OptixWebServer.Signature
{

    public class Configure
    {
        public string StoragePath { get; set; }
    }

    public class Base64Image
    {
        public static Base64Image Parse(string base64Content)
        {
            if (string.IsNullOrEmpty(base64Content))
            {
                throw new ArgumentNullException(nameof(base64Content));
            }

            int indexOfSemiColon = base64Content.IndexOf(";", StringComparison.OrdinalIgnoreCase);

            string dataLabel = base64Content.Substring(0, indexOfSemiColon);

            string contentType = dataLabel.Split(':').Last();

            var startIndex = base64Content.IndexOf("base64,", StringComparison.OrdinalIgnoreCase) + 7;

            var fileContents = base64Content.Substring(startIndex);

            var bytes = Convert.FromBase64String(fileContents);

            return new Base64Image
            {
                ContentType = contentType,
                FileContents = bytes
            };
        }

        public string ContentType { get; set; }

        public byte[] FileContents { get; set; }

        public override string ToString()
        {
            return $"data:{ContentType};base64,{Convert.ToBase64String(FileContents)}";
        }
    }


    public class SignatureController : HttpController
    {
        readonly Configure config;
        public SignatureController(Configure configure)
        {
            this.config = configure;
        }


        [HttpMethod(HttpMethodType.GET)]
        public IResult Test()
        {

            return new TextResult("ok");
        }

        [HttpMethod(HttpMethodType.POST)]
        public IResult save()
        {

            if (!string.IsNullOrWhiteSpace(this.Request.Body))
            {

                var txt = this.Request.Body;
                var dto = JsonConvert.DeserializeObject<RequestDto>(txt);

                Log.Info("Signature save", dto.id);

                var r = Base64Image.Parse(dto.image);

                var fname = DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + dto.id ; 

                switch (r.ContentType)
                {
                    case "image/png":
                        System.IO.File.WriteAllBytes(System.IO.Path.Combine(config.StoragePath, fname + ".png"), r.FileContents);
                        break;
                }


            }
            return new TextResult("ok");
        
        }
    }



    class RequestDto
    {
        public string id { get; set; }
        public string image { get; set; }
    }
}