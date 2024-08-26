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
#endregion

public class UsersAndGroupsManagement_RuntimeNetLogic : BaseNetLogic
{

    IUANode nActualGroups;

    IUANode nGroups;
    IUANode nUsers;

    IUAVariable vRefresh;
    int indent;
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started

        nActualGroups = LogicObject.GetObject("Groups");

        nUsers = InformationModel.Get(Owner.GetVariableValue("Users", NodeId.Empty));
        nGroups = InformationModel.Get(Owner.GetVariableValue("Groups", NodeId.Empty));

        indent = Owner.GetVariableValue("Indent", 8);
        vRefresh = LogicObject.GetVariable("RefreshTrigger");
        BuildGroupsModel();
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }


    #region UI Methods
    [ExportMethod]
    public void RefreshUsers()
    {

    }

    [ExportMethod]
    public void RefreshGroups()
    {

    }

    [ExportMethod]
    public void RefreshAll()
    {
        BuildGroupsModel();
    }

    [ExportMethod]
    public void MoveIn(string user,string group)
    {
        UserMoveInGroup(user,group);
        BuildGroupsModel();
        vRefresh.Value = (bool)vRefresh.Value ? false: true;
    }

    [ExportMethod]
    public void MoveOut(string value)
    {
        var vv = value.Split("$");
        if(vv.Length == 2 ) { 
            var group = vv[0];
            var user = vv[1];
            UserMoveOutGroup(user, group);
            BuildGroupsModel();
            vRefresh.Value = (bool)vRefresh.Value ? false : true;
        }
    }

    [ExportMethod]
    public void Delete(NodeId nodeId)
    {
        DeleteUser(nodeId);
        BuildGroupsModel();
    }

    #endregion



    private User GetUser(string user)
    {
        return nUsers.Get(user) as User;
    }
    private Group GetGroup(string group)
    {
        return nGroups.Get(group) as Group;
    }

    private void DeleteUser(NodeId nodeId)
    {
        var userObjectToRemove = InformationModel.Get(nodeId);

        nUsers.Remove(userObjectToRemove);


        //if (userObjectToRemove == null)
        //{
        //    Log.Error("UserEditor", "Cannot obtain the selected user.");
        //    return;
        //}

        //var userVariable = Owner.Owner.Owner.Owner.GetVariable("Users");
        //if (userVariable == null)
        //{
        //    Log.Error("UserEditor", "Missing user variable in UserEditor Panel.");
        //    return;
        //}

        //if (userVariable.Value == null || (NodeId)userVariable.Value == NodeId.Empty)
        //{
        //    Log.Error("UserEditor", "Fill User variable in UserEditor.");
        //    return;
        //}
        //var usersFolder = InformationModel.Get(userVariable.Value);
        //if (usersFolder == null)
        //{
        //    Log.Error("UserEditor", "Cannot obtain Users folder.");
        //    return;
        //}

        //usersFolder.Remove(userObjectToRemove);

        //if (usersFolder.Children.Count > 0)
        //{
        //    var usersList = Owner.Owner.Owner.Get<ListBox>("HorizontalLayout1/UsersList");
        //    usersList.SelectedItem = usersFolder.Children.First().NodeId;
        //}
    }

    private void UserMoveInGroup(string user,string group)
    {
        var _user = GetUser(user);
        var _group = GetGroup(group);
        if (_user == null || _group == null)
        {
            return;
        }

        _user.Refs.AddReference(FTOptix.Core.ReferenceTypes.HasGroup, _group);


    }

    private void UserMoveOutGroup(string user, string group)
    {
        var _user = GetUser(user);
        var _group = GetGroup(group);
        if (_user == null || _group == null)
        {
            return;
        }

        _user.Refs.RemoveReference(FTOptix.Core.ReferenceTypes.HasGroup, _group.NodeId,false);


    }


    private void BuildGroupsModel()
    {
        var users = nUsers.Children.OfType<User>();

        var groups = nGroups.Children.OfType<Group>();

        nActualGroups.ClearAll();


        foreach ( var group in groups )
        {
            if(group.BrowseName == "Guests")
            {
                continue;
            }

            var vGroup = InformationModel.MakeVariable(group.BrowseName, OpcUa.DataTypes.String);
            vGroup.Value = group.BrowseName;
            nActualGroups.Add(vGroup);
            foreach (var user in  users)
            {
                if (UserHasGroup(user, group.NodeId))
                {

                    var vUser =InformationModel.MakeVariable(group.BrowseName + "$" + user.BrowseName,OpcUa.DataTypes.String);
                    vUser.Value = "🤵" + user.BrowseName;
                    nActualGroups.Add(vUser);
                }
            }

            

        }


    }

    private bool UserHasGroup(IUANode user, NodeId groupNodeId)
    {
        if (user == null)
            return false;
        var userGroups = user.Refs.GetObjects(FTOptix.Core.ReferenceTypes.HasGroup, false);
        foreach (var userGroup in userGroups)
        {
            if (userGroup.NodeId == groupNodeId)
                return true;
        }
        return false;
    }

}
