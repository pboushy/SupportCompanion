using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using ReactiveUI;
using SupportCompanion.Helpers;
using SupportCompanion.Interfaces;
using SupportCompanion.Models;
using SupportCompanion.Services;

namespace SupportCompanion.ViewModels;

public class TransparentWindowViewModel : ViewModelBase
{
    private IOKitService _iioKit;
    private SystemInfoService _systemInfo;
    private readonly StorageService _storage;
    private Timer? _timer;
    private StorageModel? _storageInfo;
    private DeviceInfoModel? _deviceInfo;
    private string ModelValue { get; set; } = string.Empty;
    public ITransparentWindow TransparentWindow { get; set; }
    public static string SupportPhoneNumber => App.Config.SupportPhone;
    public static string SupportEmail => App.Config.SupportEmail;
    public static int FontSize => App.Config.FontSize;
    public bool ShowSeparator { get; private set; }
    public bool ShowHostname { get; private set; }
    public bool ShowSerialNumber { get; private set; }
    public bool ShowIpAddress { get; private set; }
    public bool ShowOsVersion { get; private set; }
    public bool ShowOsBuild { get; private set; }
    public bool ShowModel { get; private set; }
    public bool ShowProcessor { get; private set; }
    public bool ShowMemSize { get; private set; }
    public bool ShowLastBootTime { get; private set; }
    public bool ShowStorageInfo { get; private set; }
    public bool ShowSupportEmail { get; private set; }
    public bool ShowSupportPhone { get; private set; }
    public string HorizontalAlignment { get; private set; }
    public string VerticalAlignment { get; private set; }
    public object CurrentView { get; private set; }
    public static string DesktopInfoBackgroundColor => App.Config.DesktopInfoBackgroundColor;
    public static double DesktopInfoBackgroundOpacity => App.Config.DesktopInfoBackgroundOpacity; 

    public TransparentWindowViewModel(IOKitService iioKit, SystemInfoService systemInfo, StorageService storage)
    {
        ShowSeparator = false;
        _iioKit = iioKit;
        _systemInfo = systemInfo;
        _storage = storage;
        DeviceInfo = new DeviceInfoModel();
        StorageInfo = new StorageModel();
        SetVisibility(App.Config.DesktopInfoLevel);
        InitializeAsync();
        if (App.Config.DesktopInfoLevel == "Full" || 
            App.Config.DesktopInfoCustomItems.Contains("SupportPhone") || 
            App.Config.DesktopInfoCustomItems.Contains("SupportEmail"))
        {
            ShowSeparator = true;
        }
        VerticalAlignment = App.Config.DesktopPosition.Contains("Bottom") ? "Bottom" : "Top";
        HorizontalAlignment = App.Config.DesktopPosition.Contains("Right") ? "Right" : "Left";
    }
    
    public DeviceInfoModel? DeviceInfo
    {
        get => _deviceInfo;
        private set => this.RaiseAndSetIfChanged(ref _deviceInfo, value);
    }
    
    public StorageModel? StorageInfo
    {
        get => _storageInfo;
        private set => this.RaiseAndSetIfChanged(ref _storageInfo, value);
    }
   
    private async void GatherSystemInfoSafe()
    {
        try
        {
            await GatherSystemInfo();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    public void SetVisibility(string level)
    {
        switch (level)
        {
            case "Minimal":
                CurrentView = new Views.TransparentWindowMinimalView();
                break;
            case "Full":
                CurrentView = new Views.TransparentWindowFullView();
                break;
            case "Hardware":
                CurrentView = new Views.TransparentWindowHardwareView();
                break;
            case "Custom":
                ShowHostname = App.Config.DesktopInfoCustomItems.Contains("HostName");
                ShowModel = App.Config.DesktopInfoCustomItems.Contains("Model");
                ShowProcessor = App.Config.DesktopInfoCustomItems.Contains("Processor");
                ShowMemSize = App.Config.DesktopInfoCustomItems.Contains("MemSize");
                ShowIpAddress = App.Config.DesktopInfoCustomItems.Contains("IpAddress");
                ShowOsBuild = App.Config.DesktopInfoCustomItems.Contains("OsBuild");
                ShowOsVersion = App.Config.DesktopInfoCustomItems.Contains("OsVersion");
                ShowSerialNumber = App.Config.DesktopInfoCustomItems.Contains("SerialNumber");
                ShowStorageInfo = App.Config.DesktopInfoCustomItems.Contains("StorageInfo");
                ShowSupportEmail = App.Config.DesktopInfoCustomItems.Contains("SupportEmail");
                ShowSupportPhone = App.Config.DesktopInfoCustomItems.Contains("SupportPhone");
                ShowLastBootTime = App.Config.DesktopInfoCustomItems.Contains("LastBootTime");
                CurrentView = new Views.TransparentWindowCustomView();
                break;
        }
    }
    
    private async Task InitializeAsync()
    {
        var interval = (int)TimeSpan.FromSeconds(30).TotalMilliseconds;
        _timer ??= new Timer(_ => GatherSystemInfoSafe(), null, 0, interval);
    }

    private async Task GetDeviceInfo()
    {
        DeviceInfo.SerialNumber = _iioKit.GetSerialNumber();
        DeviceInfo.IpAddress = await _systemInfo.GetIPAddress();
        DeviceInfo.OsVersion = _systemInfo.GetOSVersion();
        DeviceInfo.OsBuild = _systemInfo.GetOSBuild();
        DeviceInfo.HostName = SystemInfo.GetSystemInfo("kern.hostname");
        ModelValue = _iioKit.GetProductName() ?? _systemInfo.GetModel();
        DeviceInfo.Model = ModelValue;
        DeviceInfo.Processor = _systemInfo.GetProcessor();
        DeviceInfo.MemSize = _systemInfo.GetMemSize();
        DeviceInfo.LastBootTime = await _systemInfo.GetLastBootTime();
        if (App.Config.DesktopInfoColorHighlight)
        {
            DeviceInfo.LastBootTimeColor = DeviceInfo.LastBootTime switch
            {
                < 7 => "LightGreen",
                < 14 => "#FCE100",
                _ => "#FF4F44"
            };
        }
    }

    private async Task GetStorageInfo()
    {
        var storageInfo = await _storage.GetStorageInfo();
        StorageInfo.VolumeName = storageInfo["VolumeName"].ToString();
        StorageInfo.VolumeUsedPercentage = Convert.ToDouble(storageInfo["VolumeUsedPercentage"]);
        StorageInfo.IsEncrypted = Convert.ToBoolean(storageInfo["IsEncrypted"]);
        StorageInfo.FileVaultEnabled = Convert.ToBoolean(storageInfo["FileVaultEnabled"]);
        if (App.Config.DesktopInfoColorHighlight)
        {
            StorageInfo.IsEncryptedColor = StorageInfo.FileVaultEnabled ? "LightGreen" : "#FF4F44";
            StorageInfo.VolumeUsedPercentageColor = StorageInfo.VolumeUsedPercentage switch
            {
                < 65 => "LightGreen",
                < 80 => "#FCE100",
                _ => "#FF4F44"
            };
        }
    }
    
    private async Task GatherSystemInfo()
    {
        await GetDeviceInfo();
        await GetStorageInfo();
        Dispatcher.UIThread.InvokeAsync( async () =>
        {
            TransparentWindow.invalidateVisual();
            TransparentWindow.hide();
            TransparentWindow.show();
        });
    }
}