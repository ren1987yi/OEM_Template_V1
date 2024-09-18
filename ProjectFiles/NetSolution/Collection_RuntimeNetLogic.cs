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
using System.Collections.Generic;
using System.Linq;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;
using FTOptix.EventLogger;
#endregion

public class Collection_RuntimeNetLogic : BaseNetLogic
{

    Item container;


    IUANode uModel;
    IUANode uUITypeConverter;
    Dictionary<string, UITypeMapper> uiMapper;

    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        container = Owner as Item;
        if(container == null)
        {
            throw new NullReferenceException("Container is not a Base Item UI");
        }

        uModel = Owner.GetAlias("Model");

        var v = Owner.GetVariable("UITypeConverter");
        if(v == null)
        {
            throw new Exception();
        }
        this.uUITypeConverter = InformationModel.Get(v.Value);



        if(uModel == null || uUITypeConverter == null)
        {
            throw new Exception();
        }
        BuildUITypeMapper();
        BuildUI();
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    private void BuildUITypeMapper()
    {
        if(uUITypeConverter == null)
        {
            return;
        }

        uiMapper = new Dictionary<string, UITypeMapper>();


        foreach(var item in uUITypeConverter.Children.OfType<Alias>())
        {
            var key = item.BrowseName;
            var value = uUITypeConverter.GetAlias(key);
            var aliasName = string.Empty;
            var v = item.GetVariable("AliasName");
            if(v != null)
            {
                aliasName = v.Value;
            }

            uiMapper.Add(key, new UITypeMapper() { UiType = value,AliasName = aliasName });

            Log.Info("UI Mapper", $"{key} -> {value.BrowseName}");
        }










    }
    private void BuildUI()
    {
        foreach(var item in this.uModel.Children)
        {
            try
            {
                string typeName = string.Empty;
                if(item is IUAVariable)
                {
                    var vitem = (IUAVariable)item;
                    var dataType = vitem.Context.GetDataType(vitem.DataType);
                    typeName = dataType.BrowseName;
                }
                else
                {
                    typeName = item.GetType().Name;
                }

                Log.Info("GET ITEM TYPE",typeName);

                if(uiMapper.TryGetValue(typeName, out var mapper))
                {
                    var ui = InformationModel.MakeObject(item.BrowseName, mapper.UiType.NodeId);

                    //var ui = InformationModel.MakeObject<__SpinBox__>(item.BrowseName);
                    if (!string.IsNullOrWhiteSpace(mapper.AliasName))
                    {

                        ui.SetAlias(mapper.AliasName,item);
                    }

                    container.Add(ui);
                }

            }catch(Exception ex)
            {
                Log.Error("Collection UI", ex.Message);
                continue;
            }


        }
    }




    class UITypeMapper
    {
        public IUANode UiType { get; set; }
        public string AliasName { get; set; }
    }
}
