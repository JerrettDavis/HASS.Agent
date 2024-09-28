using System.Linq;
using HASS.Agent.Shared.Models.HomeAssistant;
using Microsoft.Win32;
using HASS.Agent.Shared.Extensions;

namespace HASS.Agent.Shared.HomeAssistant.Sensors.GeneralSensors.SingleValue;

/// <summary>
/// Sensor indicating whether the webcam is in use
/// </summary>
public class WebcamActiveSensor : AbstractSingleValueSensor
{
    private const string DefaultName = "webcamactive";

    public WebcamActiveSensor(
        int? updateInterval = null,
        string? entityName = DefaultName,
        string? name = DefaultName, string? id = default, string? advancedSettings = default) :
        base(
            entityName ?? DefaultName,
            name ?? null,
            updateInterval ?? 10,
            id,
            advancedSettings: advancedSettings)
    {
        Domain = "binary_sensor";
    }

    public override string GetState()
    {
        return IsWebcamInUse() ? "ON" : "OFF";
    }

    public override string GetAttributes() => string.Empty;

    public override DiscoveryConfigModel? GetAutoDiscoveryConfig()
    {
        if (Variables.MqttManager == null) return null;

        var deviceConfig = Variables.MqttManager.GetDeviceConfigModel();
        if (deviceConfig == null) return null;

        return AutoDiscoveryConfigModel ?? SetAutoDiscoveryConfigModel(
            new SensorDiscoveryConfigModel
            {
                EntityName = EntityName,
                Name = Name,
                Unique_id = Id,
                Device = deviceConfig,
                State_topic =
                    $"{Variables.MqttManager.MqttDiscoveryPrefix()}/{Domain}/{deviceConfig.Name}/{EntityName}/state",
                Availability_topic =
                    $"{Variables.MqttManager.MqttDiscoveryPrefix()}/sensor/{deviceConfig.Name}/availability",
                Icon = "mdi:webcam"
            });
    }

    private static bool IsWebcamInUse()
    {
        const string regKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam";
        bool inUse;

        // first local machine
        using var localMachineKey = Registry.LocalMachine.OpenSubKey(regKey);
        inUse = CheckRegForWebcamInUse(localMachineKey);
        if (inUse) return true;

        // then current user
        using var currentUserKey = Registry.CurrentUser.OpenSubKey(regKey);
        inUse = CheckRegForWebcamInUse(currentUserKey);
        return inUse;
    }
    
    private static bool CheckRegForWebcamInUse(RegistryKey? key)
    {
        if (key == null) return false;
        
        return key.GetSubKeyNames()
            .Select(subKeyName => subKeyName == "NonPackaged"
                ? key.OpenSubKey(subKeyName)?.Let(CheckNonPackaged) == true
                : key.OpenSubKey(subKeyName)?.Let(HasWebcamInUse) == true)
            .Any(x => x);

        bool HasWebcamInUse(RegistryKey? subKey)
        {
            if (subKey == null || !subKey.GetValueNames().Contains("LastUsedTimeStop"))
                return false;

            var endTime = subKey.GetValue("LastUsedTimeStop") is long time ? time : -1;
            return endTime <= 0;
        }

        bool CheckNonPackaged(RegistryKey nonpackagedKey) =>
            nonpackagedKey.GetSubKeyNames()
                .Select(nonpackagedKey.OpenSubKey)
                .Any(HasWebcamInUse);
    }
}