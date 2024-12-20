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
using System.Data.Common;
#endregion

public class raC_Dvc_NavButton_PanelLoader_RuntimeNetLogic : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void ChangePanel()
    {
        //constant for library and type location in Logix Controller
        const string library = "Inf_Lib";
        const string libraryType = "Inf_Type";

        //Define strings for library and type to be read from Logix Controller
        string lib = "";
        string lType = "";

        //Define nodes used
        IUANode dBFromString = null;
        IUANode logixTag = null;
        IUAObject lButton = null;
        IUAObject launchAliasObj = null;
        string faceplateTypeName = "";

        //  Find the button owner object and the Ref_* tags associated with it.
        try
        {
            // Get button object
            lButton = Owner.Owner.GetObject(this.Owner.BrowseName);

            // Make Launch Object that will contain aliases
            launchAliasObj = InformationModel.MakeObject("LaunchAliasObj");
            // Get each alias from Launch Button and add them into Launch Object, and assign NodeId values 
            foreach (var inpTag in lButton.Children)
            {
                if (inpTag.BrowseName.Contains("Ref_"))
                {
                    if (inpTag.BrowseName == "Ref_Tag")
                    {
                        logixTag = InformationModel.Get(((UAVariable)inpTag).Value);
                    }
                    // Make a variable with same name as alias of type NodeId
                    var newVar = InformationModel.MakeVariable(inpTag.BrowseName, OpcUa.DataTypes.NodeId);
                    // Assign alias value to new variable
                    newVar.Value = ((UAManagedCore.UAVariable)inpTag).Value;
                    // Add variable int launch object
                    launchAliasObj.Add(newVar);
                }
            }
        }
        catch
        {
            Log.Warning(this.GetType().Name, "Error creating alias Ref_* objects");
            return;
        }

        // Make sure the Logix Tag is valid before continuing
        if (logixTag == null)
        {
            Log.Warning(this.GetType().Name, "Failed to get logix tag from Ref_* objects");
            return;
        }




        try
        {
            var fpType = lButton.GetVariable("Cfg_DisplayType").Value;

            // From Logix Tag assemble the name of Faceplate
            lib = ((string)logixTag.Children.GetVariable(library).RemoteRead().Value).Replace('-', '_');
            lType = (string)logixTag.Children.GetVariable(libraryType).RemoteRead().Value;
            faceplateTypeName = lib + '_' + lType + '_' + fpType;


            // Find DialogBox from assembled Faceplate string
            var foundFp = Project.Current.Find(faceplateTypeName);
            if (foundFp == null)
            {
                Log.Warning(this.GetType().Name, "Dialog Box '" + faceplateTypeName + "' not found for tag '" + logixTag.BrowseName + "'. Check tag members '" + library + "' (" + lib + ") and '" + libraryType + "' (" + lType + ")");
                return;
            }
            dBFromString = foundFp;
            

        }
        catch
        {
            Log.Warning(this.GetType().Name, "Error retrieving Dialog Box for tag '" + logixTag.BrowseName + "'. Check tag members '" + library + "' (" + lib + ") and '" + libraryType + "' (" + lType + ")");
            return;
        }




        var loader = Owner.GetAlias("Loader") as PanelLoader;

        if (loader != null) {
            loader.ChangePanel(dBFromString, new NodeId[] { launchAliasObj.NodeId });
        
        }


    }
}
