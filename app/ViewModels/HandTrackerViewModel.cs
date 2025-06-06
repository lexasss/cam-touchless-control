﻿using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Threading;

namespace CameraTouchlessControl;

public class HandTrackerViewModel : INotifyPropertyChanged
{
    public bool IsHandTrackerReady
    {
        get => field;
        private set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsHandTrackerReady)));
        }
    } = false;

    public bool IsHandTrackerConnected
    {
        get => field;
        private set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsHandTrackerConnected)));
        }
    } = false;

    public bool IsHandTrackingRunning
    {
        get => field;
        set
        {
            if (field == value)
                return;

            field = value;
            if (value)
            {
                StartHandTracking();
            }
            else
            {
                StopHandTracking();
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsHandTrackingRunning)));
        }
    } = false;

    public LeapMotionDevice? SelectedHandTracker
    {
        get => field;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedHandTracker)));

            HasSelectedHandTracker = value != null;
        }
    } = null;

    public bool HasSelectedHandTracker
    {
        get => field;
        private set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasSelectedHandTracker)));
        }
    }

    public bool HasHandTrackers
    {
        get => field;
        private set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasHandTrackers)));
        }
    } = false;

    public ObservableCollection<LeapMotionDevice> HandTrackers { get; } = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    public HandTrackerViewModel(HandTrackingService handTrackingService, Dispatcher dispatcher)
    {
        _handTrackingService = handTrackingService;
        _dispatcher = dispatcher;

        IsHandTrackerReady = _handTrackingService.IsReady;

        _handTrackingService.ConnectionStatusChanged += HandTracker_ConnectionStatusChanged;
        _handTrackingService.DeviceAdded += HandTracker_DeviceAdded;
        _handTrackingService.DeviceRemoved += HandTracker_DeviceRemoved;

        foreach (var device in _handTrackingService.Devices)
        {
            HandTrackers.Add(new LeapMotionDevice(device));
        }

        HasHandTrackers = _handTrackingService.Devices.Count > 0;

        EnsureSomeHandTrackerIsSelected();
    }

    // Internal

    readonly HandTrackingService _handTrackingService;
    readonly Dispatcher _dispatcher;

    private void EnsureSomeHandTrackerIsSelected()
    {
        if (HandTrackers.Count > 0 && SelectedHandTracker == null)
        {
            SelectedHandTracker = HandTrackers.First();
        }
    }

    private void StartHandTracking()
    {
        if (IsHandTrackerConnected)
            return;

        if (_handTrackingService.Devices.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("Found no hand tracking devices");
            return;
        }

        if (_handTrackingService.IsConnected)
        {
            IsHandTrackerConnected = true;
        }
        else
        {
            _handTrackingService.Connect();
        }
    }

    private void StopHandTracking()
    {
        IsHandTrackingRunning = false;
    }

    private void HandTracker_DeviceRemoved(object? sender, Leap.Device e)
    {
        var device = HandTrackers.FirstOrDefault(d => d.Device.SerialNumber == e.SerialNumber);

        if (device != null)
        {
            _dispatcher.Invoke(() =>
            {
                if (IsHandTrackingRunning)
                {
                    StopHandTracking();
                }

                HandTrackers.Remove(device);
                HasHandTrackers = _handTrackingService?.Devices.Count > 0;
            });

            System.Diagnostics.Debug.WriteLine($"Hand tracking device {e.SerialNumber} was lost");
        }
    }

    private void HandTracker_DeviceAdded(object? sender, Leap.Device e)
    {
        if (HandTrackers.FirstOrDefault(d => d.Device.SerialNumber == e.SerialNumber) == null)
        {
            _dispatcher.Invoke(() =>
            {
                HandTrackers.Add(new LeapMotionDevice(e));
                HasHandTrackers = _handTrackingService?.Devices.Count > 0;
                EnsureSomeHandTrackerIsSelected();
            });

            System.Diagnostics.Debug.WriteLine($"Found hand tracking device {e.SerialNumber}");
        }
    }

    private void HandTracker_ConnectionStatusChanged(object? sender, bool e)
    {
        IsHandTrackerConnected = e;
    }
}
