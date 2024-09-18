#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.SQLiteStore;
using FTOptix.HMIProject;
using FTOptix.NativeUI;
using FTOptix.CoreBase;
using FTOptix.WebUI;
using FTOptix.Recipe;
using FTOptix.Store;
using FTOptix.System;
using FTOptix.NetLogic;
using FTOptix.Retentivity;
using FTOptix.UI;
using FTOptix.Alarm;
using FTOptix.Core;
using OEM_Template_V1;
using System.Linq;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;
using FTOptix.EventLogger;
#endregion

public class Dialog_AddOrUpdateUser_RuntimeNetLogic : BaseNetLogic
{
    int model = 0;
    User curUser;
    IUANode nUsers ;

    IUAVariable vUserName;
    IUAVariable vPassword;

    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started

        var v = Owner.GetAlias("ModeVariable") as IUAVariable;
        model = (int)v.Value;
        nUsers = InformationModel.Get( Owner.GetVariableValue("Users",NodeId.Empty));

        curUser = Owner.GetAlias("User") as User;

        //vUserName = LogicObject.GetVariable("UserName");
        //vPassword = LogicObject.GetVariable("Password");


        if(model == 1)
        {
            LogicObject.SetVariableValue("UserName", curUser.BrowseName);
            LogicObject.SetVariableValue("Password", (string)curUser.PasswordVariable.Value);
        }

    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }


    [ExportMethod]
    public void Apply()
    {
        var username = LogicObject.GetVariableValue("UserName", string.Empty);
        var password = LogicObject.GetVariableValue("Password", string.Empty);
        if (model == 0)
        {
            CreateUser(username,password,string.Empty,out var result);
            if(result != null)
            {
                (Owner as Dialog).Close();
            }
        }else if( model == 1)
        {
            var result = UpdateUser(username, password, string.Empty);
            if( result  != NodeId.Empty )
            {
                (Owner as Dialog).Close();
            }
        }
    }





    private NodeId UpdateUser(string username, string password, string locale)
    {
        var users = GetUsers();
        if (users == null)
        {
            //ShowMessage(2);
            Log.Error("EditUserDetailPanelLogic", "Unable to get users");
            return NodeId.Empty;
        }

        var user = users.Get<FTOptix.Core.User>(username);
        if (user == null)
        {
            //ShowMessage(3);
            Log.Error("EditUserDetailPanelLogic", "User not found");
            return NodeId.Empty;
        }

        //Apply LocaleId
        if (!string.IsNullOrEmpty(locale))
            user.LocaleId = locale;

        //Apply groups
        //ApplyGroups(user);

        //Apply password
        if (!string.IsNullOrEmpty(password))
        {
            var result = Session.ChangePasswordInternal(username, password);
            try
            {

                switch (result.ResultCode)
                {
                    case FTOptix.Core.ChangePasswordResultCode.Success:
                        var editPasswordTextboxPtr = LogicObject.GetVariable("PasswordTextbox");
                        if (editPasswordTextboxPtr == null)
                            Log.Error("EditUserDetailPanelLogic", "PasswordTextbox variable not found");

                        var nodeId = (NodeId)editPasswordTextboxPtr.Value;
                        if (nodeId == null)
                            Log.Error("EditUserDetailPanelLogic", "PasswordTextbox not set");

                        var editPasswordTextbox = (TextBox)InformationModel.Get(nodeId);
                        if (editPasswordTextbox == null)
                            Log.Error("EditUserDetailPanelLogic", "EditPasswordTextbox not found");

                        editPasswordTextbox.Text = string.Empty;
                        break;
                    case FTOptix.Core.ChangePasswordResultCode.WrongOldPassword:
                        //Not applicable
                        break;
                    case FTOptix.Core.ChangePasswordResultCode.PasswordAlreadyUsed:
                        //ShowMessage(4);
                        return NodeId.Empty;
                    case FTOptix.Core.ChangePasswordResultCode.PasswordChangedTooRecently:
                        //ShowMessage(5);
                        return NodeId.Empty;
                    case FTOptix.Core.ChangePasswordResultCode.PasswordTooShort:
                        //ShowMessage(6);
                        return NodeId.Empty;
                    case FTOptix.Core.ChangePasswordResultCode.UserNotFound:
                        //ShowMessage(7);
                        return NodeId.Empty;
                    case FTOptix.Core.ChangePasswordResultCode.UnsupportedOperation:
                        //ShowMessage(8);
                        return NodeId.Empty;

                }

            }
            catch (Exception ex)
            {

            }

            if (result.ResultCode == FTOptix.Core.ChangePasswordResultCode.Success)
            {
                return user.NodeId;
            }
            else
            {
                return NodeId.Empty ;
            }

        }

        //ShowMessage(9);
        return user.NodeId;
    }



    public void CreateUser(string username, string password, string locale, out NodeId result)
    {
        result = NodeId.Empty;

        if (string.IsNullOrEmpty(username))
        {
            //ShowMessage(1);
            Log.Error("EditUserDetailPanelLogic", "Cannot create user with empty username");
            return;
        }

        result = GenerateUser(username, password, locale);
    }
    private IUANode GetUsers()
    {
        return nUsers;
    }
    private NodeId GenerateUser(string username, string password, string locale)
    {
        var users = GetUsers();
        if (users == null)
        {
            //ShowMessage(2);
            Log.Error("EditUserDetailPanelLogic", "Unable to get users");
            return NodeId.Empty;
        }

        foreach (var child in users.Children.OfType<FTOptix.Core.User>())
        {
            if (child.BrowseName.Equals(username, StringComparison.OrdinalIgnoreCase))
            {
                //ShowMessage(10);
                Log.Error("EditUserDetailPanelLogic", "Username already exists");
                return NodeId.Empty;
            }
        }

        var user = InformationModel.MakeObject<FTOptix.Core.User>(username);
        users.Add(user);

        //Apply LocaleId
        if (!string.IsNullOrEmpty(locale))
            user.LocaleId = locale;

        //Apply groups
        //ApplyGroups(user);

        //Apply password
        var result = Session.ChangePassword(username, password, string.Empty);

        switch (result.ResultCode)
        {
            case FTOptix.Core.ChangePasswordResultCode.Success:
                break;
            case FTOptix.Core.ChangePasswordResultCode.WrongOldPassword:
                //Not applicable
                break;
            case FTOptix.Core.ChangePasswordResultCode.PasswordAlreadyUsed:
                //Not applicable
                break;
            case FTOptix.Core.ChangePasswordResultCode.PasswordChangedTooRecently:
                //Not applicable
                break;
            case FTOptix.Core.ChangePasswordResultCode.PasswordTooShort:
                //ShowMessage(6);
                users.Remove(user);
                return NodeId.Empty;
            case FTOptix.Core.ChangePasswordResultCode.UserNotFound:
                //Not applicable
                break;
            case FTOptix.Core.ChangePasswordResultCode.UnsupportedOperation:
                //ShowMessage(8);
                users.Remove(user);
                return NodeId.Empty;

        }

        return user.NodeId;
    }
}
