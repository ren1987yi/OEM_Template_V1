#region Using directives

using FTOptix.NetLogic;
using System;
using System.Net.NetworkInformation;
using UAManagedCore;
using FTOptix.Alarm;
using FTOptix.Recipe;
using FTOptix.SQLiteStore;
using FTOptix.Store;

#endregion

public class GlobalVariable_RuntimeNetLogic : BaseNetLogic
{
    private PeriodicTask task1s;
    private GlobalVariableType globalVariable;
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        globalVariable = GlobalVariableType.Build(Owner);
        task1s = new PeriodicTask((t,e) => { Task1s_Handle(e); }, globalVariable, 1000, LogicObject);
        task1s.Start();
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
        task1s.Cancel();
    }

    private void Task1s_Handle(object arg)
    {
        GlobalVariableType glv = (GlobalVariableType)arg;

        glv.CurrentDateTime = DateTime.Now;
    }

    private class GlobalVariableType
    {
        private IUAVariable CurrentDateTimeVariable;

        public DateTime CurrentDateTime
        {
            get => CurrentDateTimeVariable.Value;
            set => CurrentDateTimeVariable.Value = value;
            
        }

        public static GlobalVariableType Build(IUANode owner)
        {
            if (owner != null)
            {
                var glb = new GlobalVariableType();
                glb.CurrentDateTimeVariable = owner.GetVariable(nameof(CurrentDateTime));
                return glb;
            }
            else
            {
                throw new NullReferenceException("GlobalVariableType host node is empty");
            }
        }
    }
}
