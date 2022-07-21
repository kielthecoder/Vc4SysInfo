using System;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.CrestronThread;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.Diagnostics;
using Crestron.SimplSharpPro.UI;

namespace SimpleExample
{
    public class ControlSystem : CrestronControlSystem
    {
        private XpanelForSmartGraphics _tp;
        private CTimer _refresh;

        public bool ReallySupportsSystemMonitor { get; private set; }

        public ControlSystem()
            : base()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 20;

                CrestronEnvironment.ProgramStatusEventHandler += _ProgramStatusEventHandler;
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in the constructor: {0}", e.Message);
            }
        }

        private void _ProgramStatusEventHandler(eProgramStatusEventType e)
        {
            if (e == eProgramStatusEventType.Stopping)
            {
                if (_refresh != null)
                {
                    _refresh.Stop();
                    _refresh.Dispose();
                }
            }
        }

        public override void InitializeSystem()
        {
            try
            {
                if (SupportsSystemMonitor)
                {
                    try
                    {
                        SystemMonitor.SetUpdateInterval(10);
                        SystemMonitor.ProcessStatisticChange += _ProcessStatisticChange;

                        ReallySupportsSystemMonitor = true;
                    }
                    catch (Exception e)
                    {
                        ReallySupportsSystemMonitor = false;
                    }
                }
                else
                {
                    ReallySupportsSystemMonitor = false;
                }

                _tp = new XpanelForSmartGraphics(0x03, this);

                _tp.OnlineStatusChange += _tp_OnlineStatusChange;
                _tp.SigChange += _tp_SigChange;
                
                if (_tp.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                {
                    ErrorLog.Error("Registration for XPanel failed: " + _tp.RegistrationFailureReason.ToString());
                }

                _refresh = new CTimer(_TimeRefresh, _tp, 2000, 2000);
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in InitializeSystem: {0}", e.Message);
            }
        }

        private void _tp_OnlineStatusChange(GenericBase dev, OnlineOfflineEventArgs args)
        {
            var tp = (BasicTriListWithSmartObject)dev;

            if (args.DeviceOnLine)
            {
                tp.StringInput[1].StringValue = "System Information";
                tp.BooleanInput[665].BoolValue = ReallySupportsSystemMonitor;

                tp.StringInput[2].StringValue = CrestronEnvironment.DevicePlatform.ToString();
                tp.StringInput[3].StringValue = this.ControllerPrompt;
                tp.StringInput[4].StringValue = this.NumProgramsSupported.ToString();

                tp.StringInput[5].StringValue = CrestronEnvironment.GetLocalTime().ToLongDateString();
                tp.StringInput[6].StringValue = CrestronEnvironment.GetLocalTime().ToLongTimeString();

                tp.StringInput[7].StringValue = Directory.GetApplicationRootDirectory();
                tp.StringInput[8].StringValue = Directory.GetApplicationDirectory();

                if (ReallySupportsSystemMonitor)
                {
                    try
                    {
                        tp.StringInput[9].StringValue = "Gathering data...";
                        tp.StringInput[10].StringValue = "Gathering data...";
                        tp.StringInput[11].StringValue = "Gathering data...";

                        tp.StringInput[12].StringValue = SystemMonitor.VersionInformation.ControlSystemVersion;
                    }
                    catch (Exception e)
                    {
                        ErrorLog.Error("Error in _tp_OnlineStatusChange: {0}", e.Message);
                    }
                }
                else
                {
                    tp.StringInput[9].StringValue = "Not Supported";
                    tp.StringInput[10].StringValue = "Not Supported";
                    tp.StringInput[11].StringValue = "Not Supported";
                    tp.StringInput[12].StringValue = "Not Supported";
                }
            }
        }

        private void _ProcessStatisticChange(ProcessStatisticChangeEventArgs args)
        {
            if (_tp.Registered)
            {
                if (ReallySupportsSystemMonitor)
                {
                    _tp.StringInput[9].StringValue = SystemMonitor.TotalRAMSize.ToString();
                    _tp.StringInput[10].StringValue = SystemMonitor.RAMFree.ToString();
                    _tp.StringInput[11].StringValue = SystemMonitor.NumberOfRunningProcesses.ToString();
                }
            }
        }

        private void _TimeRefresh(object userObj)
        {
            var tp = (BasicTriListWithSmartObject)userObj;

            tp.StringInput[5].StringValue = CrestronEnvironment.GetLocalTime().ToLongDateString();
            tp.StringInput[6].StringValue = CrestronEnvironment.GetLocalTime().ToLongTimeString();
        }

        private void _tp_SigChange(BasicTriList dev, SigEventArgs args)
        {
            if (args.Sig.Type == eSigType.Bool)
            {
                if (args.Sig.BoolValue)
                {
                    switch (args.Sig.Number)
                    {
                        case 666:
                            if (SupportsSystemMonitor)
                            {
                                try
                                {
                                    SystemMonitor.CurrentProgram.Restart();
                                }
                                catch (Exception e)
                                {
                                    ErrorLog.Error("Error in _tp_SigChange: {0}", e.Message);
                                }
                            }
                            break;
                    }
                }
            }
        }
    }
}