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
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
#endregion

public class HighSeverityAlarm_RuntimeNetLogic : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        var context = LogicObject.Context;

        affinityId = context.AssignAffinityId();

        RegisterObserverOnLocalizedAlarmsContainer(context);
        RegisterObserverOnSessionActualLanguageChange(context);
        RegisterObserverOnLocalizedAlarmsObject(context);
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
        alarmEventRegistration?.Dispose();
        alarmEventRegistration2?.Dispose();
        sessionActualLanguageRegistration?.Dispose();
        alarmBannerSelector?.Dispose();

        alarmEventRegistration = null;
        alarmEventRegistration2 = null;
        sessionActualLanguageRegistration = null;
        alarmBannerSelector = null;

        retainedAlarmsObjectObserver = null;
    }


    public void RegisterObserverOnLocalizedAlarmsObject(IContext context)
    {
        var retainedAlarms = context.GetNode(FTOptix.Alarm.Objects.RetainedAlarms);

        retainedAlarmsObjectObserver = new RetainedAlarmsObjectObserver((ctx) => RegisterObserverOnLocalizedAlarmsContainer(ctx));

        // observe ReferenceAdded of localized alarm containers
        alarmEventRegistration2 = retainedAlarms.RegisterEventObserver(
            retainedAlarmsObjectObserver, EventType.ForwardReferenceAdded, affinityId);
    }


    public void RegisterObserverOnLocalizedAlarmsContainer(IContext context)
    {
        var retainedAlarms = context.GetNode(FTOptix.Alarm.Objects.RetainedAlarms);
        var localizedAlarmsVariable = retainedAlarms.GetVariable("LocalizedAlarms");
        var localizedAlarmsNodeId = (NodeId)localizedAlarmsVariable.Value;
        IUANode localizedAlarmsContainer = null;
        if (localizedAlarmsNodeId != null && !localizedAlarmsNodeId.IsEmpty)
            localizedAlarmsContainer = context.GetNode(localizedAlarmsNodeId);

        if (alarmEventRegistration != null)
        {
            alarmEventRegistration.Dispose();
            alarmEventRegistration = null;
        }

        if (alarmBannerSelector != null)
            alarmBannerSelector.Dispose();
        alarmBannerSelector = new AlarmBannerSelector2(LogicObject, localizedAlarmsContainer);

        if (localizedAlarmsContainer?.Children.Count > 0)
        {

            alarmBannerSelector.Initialize();
        }

        alarmEventRegistration = localizedAlarmsContainer?.RegisterEventObserver(
            alarmBannerSelector,
            EventType.ForwardReferenceAdded | EventType.ForwardReferenceRemoved, affinityId);
    }

    public void RegisterObserverOnSessionActualLanguageChange(IContext context)
    {
        var currentSessionActualLanguage = context.Sessions.CurrentSessionInfo.SessionObject.Children["ActualLanguage"];

        sessionActualLanguageChangeObserver = new CallbackVariableChangeObserver(
            (IUAVariable variable, UAValue newValue, UAValue oldValue, uint[] indexes, ulong senderId) =>
            {
                RegisterObserverOnLocalizedAlarmsContainer(context);
            });

        sessionActualLanguageRegistration = currentSessionActualLanguage.RegisterEventObserver(
            sessionActualLanguageChangeObserver, EventType.VariableValueChanged, affinityId);
    }



    private class RetainedAlarmsObjectObserver : IReferenceObserver
    {
        public RetainedAlarmsObjectObserver(Action<IContext> action)
        {
            registrationCallback = action;
        }

        public void OnReferenceAdded(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId)
        {
            string localeId = targetNode.Context.Sessions.CurrentSessionHandler.ActualLocaleId;
            if (String.IsNullOrEmpty(localeId))
                localeId = "en-US";

            if (targetNode.BrowseName == localeId)
                registrationCallback(targetNode.Context);
        }

        public void OnReferenceRemoved(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId)
        {
        }

        private Action<IContext> registrationCallback;
    }

    uint affinityId = 0;
    RetainedAlarmsObjectObserver retainedAlarmsObjectObserver;
    IEventRegistration alarmEventRegistration;
    IEventRegistration alarmEventRegistration2;
    IEventRegistration sessionActualLanguageRegistration;
    IEventObserver sessionActualLanguageChangeObserver;
    AlarmBannerSelector2 alarmBannerSelector;


    private class AlarmSeverityMapper
    {
        public NodeId AlarmId { get; set; }
        public UInt16 Severity { get; set; }
        
    }

    private class AlarmBannerSelector2 : IDisposable, IReferenceObserver
    {

        public AlarmBannerSelector2(IUANode logicNode, IUANode localizedAlarmsContainer)
        {
            this.retaiendAlarms = new List<AlarmSeverityMapper>();
            this.retainedAlarmsLock = new Object();
            this.logicNode = logicNode;

            InitializeRetainedAlarmList(localizedAlarmsContainer);

            currentDisplayedAlarm = logicNode.GetVariable("CurrentDisplayedAlarm");
           
            retainedAlarmsCount = logicNode.GetVariable("AlarmCount");


            UpdateRetainedAlarmsCount();
            UpdateCurrentDisplayedAlarm();


        }
        public void Initialize()
        {

            foreach (var alarm in alarmContainer.Children)
            {
                var v = alarm.GetVariable("Severity");
                var severity = (UInt16)v.Value;


                v = alarm.GetVariable("Time");
                retaiendAlarms.Add(new AlarmSeverityMapper()
                {
                    Severity = severity,
                    AlarmId = alarm.NodeId
                });

            }

            

            UpdateRetainedAlarmsCount();
            UpdateCurrentDisplayedAlarm();
           
        }
        public void OnReferenceAdded(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId)
        {
            lock (retainedAlarmsLock)
            {
                var v = targetNode.GetVariable("Severity");
                var severity = (UInt16)v.Value;

                retaiendAlarms.Add(new AlarmSeverityMapper()
                {
                    Severity = severity,
                    AlarmId = targetNode.NodeId
                });

                UpdateRetainedAlarmsCount();
                UpdateCurrentDisplayedAlarm();

            }
        }

        public void OnReferenceRemoved(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId)
        {
            lock (retainedAlarmsLock)
            {


                retaiendAlarms.RemoveAll(c=>c.AlarmId == targetNode.NodeId);

                
                UpdateRetainedAlarmsCount();
                UpdateCurrentDisplayedAlarm();
            }
        }


        public NodeId CurrentDisplayedAlarmNodeId
        {
            get { return currentDisplayedAlarm.Value; }
        }

        private void UpdateCurrentDisplayedAlarm()
        {
            if (retainedAlarmsCount.Value == 0)
            {
                currentDisplayedAlarm.Value = NodeId.Empty;
              
                return;
            }

            var r = retaiendAlarms.OrderBy(c => c.Severity).LastOrDefault();
            if (r != null)
            {
                currentDisplayedAlarm.Value = r.AlarmId;
            }
            else
            {
                currentDisplayedAlarm.Value = NodeId.Empty;
            }


        }

        private void UpdateRetainedAlarmsCount()
        {
            retainedAlarmsCount.Value = alarmContainer.Children.Count;
        }

        private void InitializeRetainedAlarmList(IUANode localizedAlarmsContainer)
        {
            this.alarmContainer = localizedAlarmsContainer;

         



        }


        private IUAVariable currentDisplayedAlarm;
        private List<AlarmSeverityMapper> retaiendAlarms;
        private IUAVariable retainedAlarmsCount;
        private IUANode logicNode;
        private IUANode alarmContainer;


        private Object retainedAlarmsLock;

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue)
                return;

          

            disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }


}


