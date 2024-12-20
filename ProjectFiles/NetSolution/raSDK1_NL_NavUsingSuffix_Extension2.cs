#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.OPCUAServer;
using FTOptix.WebUI;
using FTOptix.RAEtherNetIP;
using FTOptix.Retentivity;
using FTOptix.CommunicationDriver;
using FTOptix.CoreBase;
using FTOptix.Core;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.IO;
#endregion

public class raSDK1_NL_NavUsingSuffix_Extension2 : BaseNetLogic
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

    public void NavSuffix()
    {
        //constant for library and type location in Logix Controller
        const string library = "Inf_Lib";
        const string libraryType = "Inf_Type";

        //Define strings for library and type to be read from Logix Controller
        string lib = "";
        string lType = "";

        //Define nodes used
        IUAObject lButton = null;
        DialogType dBFromString = null;
        IUANode logixTag = null;
        UAVariable launchAlias = null;
        string faceplateTypeName = null;
        string launchAliasPath = null;


        // Get the tag specified by Ref_BaseTag
        try
        {
            // Get button object
            lButton = Owner.Owner.GetObject(this.Owner.BrowseName);
            // Get Alias from button
            launchAlias = (UAManagedCore.UAVariable)lButton.Children.Get("Ref_BaseTag");
            // Get logix Tag from passed alias NodeId
            IUANode tagNodeId = InformationModel.Get(launchAlias.Value);
            // Get Browse Path for the tag
            launchAliasPath = GetOptixPathByNode(tagNodeId, "CommDrivers");
        }
        catch
        {
            Log.Warning(this.GetType().Name, "Error retrieving base tag specified by variable Ref_BaseTag (" + launchAlias + ").");
            return;
        }

        // Add the suffix to the base tag and get the resultant tag
        try
        {
            // Add Suffix to the tag name
            launchAliasPath += lButton.Children.GetVariable("Cfg_Suffix").Value;
            // Get Logix Tag from tag+Suffix   
            logixTag = Project.Current.Get((string)launchAliasPath);
        }
        catch
        {
            Log.Warning(this.GetType().Name, "Error retrieving tag specified by Ref_BaseTag + Cfg_Suffix (" + launchAliasPath + ").");
            return;
        }

        // Make sure the Logix Tag is valid before continuing
        if (logixTag == null)
        {
            Log.Warning(this.GetType().Name, "Failed to get tag for path '" + launchAliasPath + "'");
            return;
        }


        // Build the dialog box name and return the object
        try
        {
            var fpType = lButton.GetVariable("Cfg_DisplayType").Value;

            // From Logix Tag assemble the name of Faceplate
            lib = ((string)logixTag.Children.GetVariable(library).RemoteRead().Value).Replace('-', '_');
            lType = (string)logixTag.Children.GetVariable(libraryType).RemoteRead().Value;


            if ((lType == "raC_Tec_PwrDiscreteStateMonitor")|| (lType == "raC_Tec_PwrVelocityStateMonitor")|| (lType == "raC_Tec_PwrMotionStateMonitor"))
            {
                lType = "raC_Tec_PwrDvcStateMonitor";
            }

            faceplateTypeName = lib + '_' + lType + '_' + fpType;

            // Find DialogBox from assembled Faceplate string
            var foundFp = Project.Current.Find(faceplateTypeName);
            if (foundFp == null)
            {
                Log.Warning(this.GetType().Name, "Dialog Box '" + faceplateTypeName + "' not found for tag '" + logixTag.BrowseName + "'. Check tag members '" + library + "' (" + lib + ") and '" + libraryType + "' (" + lType + ")");
                return;
            }

            // if found is DialogType, than it is a faceplate type
            if (foundFp.GetType() == typeof(DialogType))
            {
                dBFromString = (DialogType)foundFp;
            }
            else // found current instance of faceplate
            {
                // Get faceplate type from instance
                System.Reflection.PropertyInfo objType = foundFp.GetType().GetProperty("ObjectType");
                dBFromString = (DialogType)(objType.GetValue(foundFp, null));
            }
        }
        catch
        {
            Log.Warning(this.GetType().Name, "Dialog Box not found for tag '" + logixTag.BrowseName + "'. Check tag members '" + library + "' (" + lib + ") and '" + libraryType + "' (" + lType + ")");
            return;
        }

        // Create the object that contains the alias and launch the faceplate
        try
        {
            // Make Launch Object that will contain aliases
            var launchAliasObj = InformationModel.MakeObject("LaunchAliasObj");
            // Make new variable "Ref_Tag"
            var newVar = InformationModel.MakeVariable("Ref_Tag", OpcUa.DataTypes.NodeId);
            // Assign value of Logix Tag NodeId to new variable 
            newVar.Value = logixTag.NodeId;
            // Add new variable into the launch object
            launchAliasObj.Add(newVar);
            // Launch DialogBox passing Launch Object that contains the alias as an alias 
            UICommands.OpenDialog(lButton, dBFromString, launchAliasObj.NodeId);
        }
        catch
        {
            Log.Warning(this.GetType().Name, "Failed to launch dialog box '" + faceplateTypeName + "' for tag '" + logixTag.BrowseName + "'.");
            return;
        }

        // If configured, close the dialog box containing launch button
        try
        {
            bool cfgCloseCurrent = lButton.GetVariable("Cfg_CloseCurrentDisplay").Value;
            if (cfgCloseCurrent)
            {
                CloseCurrentDB(Owner);
            }
        }
        catch
        {
            Log.Warning(this.GetType().Name, "Failed to close current dialog box");
        }

    }
    public string GetOptixPathByNode(IUANode inputNode, string topContainer)
    {
        List<string> pathToVar = new List<string>();

        FindBrowsePath(inputNode);
        if (pathToVar.Count > 0)
        {
            var launchAliasPath = ConstructBrowsePath();
            return launchAliasPath;
        }
        else
        {
            return null;
        }
        string ConstructBrowsePath()
        {
            string outStr = topContainer;
            for (long i = (pathToVar.LongCount() - 1); i >= 0; i--)
            {
                outStr = outStr + "/" + pathToVar[(int)i];
            }
            pathToVar = new List<string>();
            return outStr;
        }

        void FindBrowsePath(IUANode inputNode)
        {
            if (inputNode.Owner != null)
            {
                if (inputNode.BrowseName == topContainer)
                {
                    return;
                }
                pathToVar.Add(inputNode.BrowseName);
                FindBrowsePath(inputNode.Owner);
            }
        }


    }

    public void CloseCurrentDB(IUANode inputNode)
    {
        // if input node is of type Dialog, close it
        if (inputNode.GetType().BaseType.BaseType == typeof(Dialog))
        {
            // close dialog box
            ((Dialog)inputNode).Close();
            return;
        }
        // if input node is Main Window, no dialog box was found, return
        //if (inputNode.GetType() == typeof(MainWindow))
        //{
        //    return;
        //}
        // continue search for Dialog or Main Window
        CloseCurrentDB(inputNode.Owner);
    }
}
