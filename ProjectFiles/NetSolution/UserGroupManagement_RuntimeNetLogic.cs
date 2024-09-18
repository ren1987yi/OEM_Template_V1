#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.Retentivity;
using FTOptix.NativeUI;
using FTOptix.System;
using FTOptix.WebUI;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.Alarm;
using FTOptix.CoreBase;
using FTOptix.Core;
using FTOptix.Recipe;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using System.Linq;
using System.Collections.Generic;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;
using FTOptix.EventLogger;
#endregion

public class UserGroupManagement_RuntimeNetLogic : BaseNetLogic
{
    IUANode nodeUsers;
    IUANode nodeGroups;
    IUANode nodeUserInfos;

    IUANode[] Groups;



    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        nodeUsers = Owner.GetAlias("Users");
        if (nodeUsers == null)
        {
            throw new NullReferenceException("users node is empty");
        }

        nodeGroups = Owner.GetAlias("Groups");
        if (nodeGroups == null)
        {
            throw new NullReferenceException("groups node is empty");
        }

        Groups = nodeGroups.Children.OfType<Group>().ToArray();
        



        nodeUserInfos = Owner.Get("UserInfos");
        if(nodeUserInfos == null)
        {
            throw new NullReferenceException("userinfos node is empty");
        }

        BuildUserInfos();


    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void OnAdd()
    {

    }

    [ExportMethod]
    public void OnDelete(NodeId nodeId)
    {

    }

    [ExportMethod]
    public void OnDisable(NodeId nodeId)
    {

    }

    [ExportMethod]
    public void OnEnable(NodeId nodeId)
    {

    }


    [ExportMethod]
    public void OnEdit(NodeId nodeId)
    {

    }

    [ExportMethod]
    public void OnRefresh(NodeId nodeId) { 
    }



    private void BuildUserInfos()
    {

        nodeUserInfos.Children.Clear();

        foreach(var _user in nodeUsers.Children.OfType<User>())
        {
            var obj = InformationModel.MakeObject(_user.BrowseName);

            var v = InformationModel.MakeVariable("LocaleId", OpcUa.DataTypes.String);
            v.Value = _user.LocaleId;
            obj.Add(v);


            v = InformationModel.MakeVariable("Language", OpcUa.DataTypes.String);
            v.Value = _user.Language;
            obj.Add(v);

            v = InformationModel.MakeVariable("Groups", OpcUa.DataTypes.String);


            var groupnames = new List<string>();


            foreach (var _group in Groups)
            {
                if (UserHasGroup(_user, _group.NodeId))
                {
                    groupnames.Add(_group.BrowseName);
                }
            }

            v.Value = string.Join(';', groupnames);
            obj.Add(v);

            nodeUserInfos.Add(obj);
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
