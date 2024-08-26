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
using System.Linq;
#endregion

public class DesignTimeNetLogic1 : BaseNetLogic
{
    [ExportMethod]
    public void Method1()
    {
        // Insert code to be executed by the method

        foreach(var item in Owner.Children.OfType<IUAVariable>())
        {
            item.Delete();
        }

        for(var i = 0; i < 2000; i++)
        {
            var b = InformationModel.MakeVariable($"a{i}", OpcUa.DataTypes.Int32);
            Owner.Add(b);
        }
    }
}
