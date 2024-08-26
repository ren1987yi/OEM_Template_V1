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
using FTOptix.System;
using FTOptix.Retentivity;
using FTOptix.UI;
using FTOptix.Alarm;
using FTOptix.Core;
using GCOptixToolkit;
#endregion

public class Lock_RuntimeNetLogic : BaseNetLogic
{
    IUAVariable vReqSession;
    IUANode nLock;
    IUAVariable vOpenTrigger;
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        //var v = Owner.GetVariable("CurrentSession");
        var obj = Project.Current.Get("Model/GlobalModels/Lock");
        if (obj == null)
        {
            return;
        }
        nLock = obj as IUANode;
        var n = obj.GetAlias("ActiveSession");
        if (n == null)
        {
            obj.SetAlias("ActiveSession", Session);
        }

        var v = obj.GetVariable("RequestSession");
        if (v !=null )
        {
            vReqSession = v;
            vReqSession.VariableChange += VReqSession_VariableChange;
        }
        vOpenTrigger = LogicObject.GetVariable("OpenDlgTrigger");

    }


    [ExportMethod]
    public void TaskControlRequest()
    {
        //var act = nLock.GetAlias("ActiveSession");
        //var req = Session;
        //if (req == null || req.NodeId == NodeId.Empty)
        //{
        //    return;
        //}

        //if (act == req)
        //{
        //    return;
        //}

        var act = nLock.GetAlias("ActiveSession");
        if (act == null)
        {
            nLock.SetAlias("ActiveSession", Session);
            return;
        }
        nLock.SetAlias("RequestSession",Session);

        //vOpenTrigger.Value = !((bool)vOpenTrigger.Value);
    }

    private void VReqSession_VariableChange(object sender, VariableChangeEventArgs e)
    {
        //throw new NotImplementedException();
        if(e.NewValue.Value == NodeId.Empty)
        {

        }
        else
        {

            var act = nLock.GetAlias("ActiveSession");
            var req = nLock.GetAlias("RequestSession");
            if (req == null || req.NodeId == NodeId.Empty)
            {
                return;
            }
            
            if(act == Session)
            {
                vOpenTrigger.Value = !((bool)vOpenTrigger.Value);
                return;
            }




            //var dlgType = LogicObject.GetAlias("Dlg") as DialogType;
            ////var dlg = Session.
            //var dlg = FTOptix.UI.UICommands.OpenDialog(Session, dlgType);

        }
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
        if(vReqSession != null)
        {
            vReqSession.VariableChange -= VReqSession_VariableChange;
        }
    }
}
