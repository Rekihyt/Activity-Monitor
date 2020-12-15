﻿using System;
using System.Management;
using System.Messaging;

namespace ActivityMonitor.Classes
{
    public class ProcessMonitor : IMonitorResource
    {
        private static ProcessMonitor _instance = null;
        private static readonly object _sync = new object();
        private ManagementEventWatcher _startWatch;
        private ManagementEventWatcher _stopWatch;


        private ProcessMonitor()
        {
        }

        public static ProcessMonitor GetInstance()
        {
            if(_instance == null)
            {
                lock (_sync)
                {
                    _instance = new ProcessMonitor();
                }
            }

            return _instance;
        }

        public void StartMonitoring()
        {
            ManagementScope scope = new ManagementScope("\\\\127.0.0.1\\root\\CIMv2");
            scope.Connect();
            try
            {
                _startWatch = new ManagementEventWatcher(scope, new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
                _startWatch.EventArrived += new EventArrivedEventHandler(startWatch_EventArrived);
                _startWatch.Start();

                _stopWatch = new ManagementEventWatcher(scope, new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace"));
                _stopWatch.EventArrived += new EventArrivedEventHandler(stopWatch_EventArrived);
                _stopWatch.Start();
            }
            catch(UnauthorizedAccessException e)
            {
                var activity = new Activity($"Error catched by UnauthorizedAccessException. Message: - {e.Message} -",
                Activity.ActivityObject.Error,
                Activity.ActivityType.Error);

                var message = new Message(activity);

                AddMessage(message);
            }
            catch(ManagementException e)
            {
                var activity = new Activity($"Error catched by ManagementException. Message: - {e.Message} -",
                Activity.ActivityObject.Error,
                Activity.ActivityType.Error);

                var message = new Message(activity);

                AddMessage(message);
            }
            catch(Exception e)
            {
                var activity = new Activity($"Error catched by Exception. Message: - {e.Message} -",
                Activity.ActivityObject.Error,
                Activity.ActivityType.Error);

                var message = new Message(activity);

                AddMessage(message);
            }
        }

        public void StopMonitoring()
        {
            _startWatch.Stop();
            _stopWatch.Stop();

            _startWatch.Dispose();
            _stopWatch.Dispose();
        }

        private void startWatch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            var activity = new Activity(e.NewEvent.Properties["ProcessName"].Value.ToString(), 
                Activity.ActivityObject.Process, 
                Activity.ActivityType.Started);

            var message = new Message(activity);

            AddMessage(message);
        }

        private void stopWatch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            var activity = new Activity(e.NewEvent.Properties["ProcessName"].Value.ToString(),
                Activity.ActivityObject.Process,
                Activity.ActivityType.Stopped);

            var message = new Message(activity);

            AddMessage(message);
        }

        private void AddMessage(Message message)
        {
            QueueManager.GetInstance().AddMessage(message);
        }
    }
}
