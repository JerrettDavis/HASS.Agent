using System.Collections.Generic;
using System.Linq;
using HASS.Agent.Shared.Extensions;
using HASS.Agent.Shared.Functions;
using HASS.Agent.Shared.Models.HomeAssistant;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace HASS.Agent.Shared.HomeAssistant.Sensors.GeneralSensors.SingleValue;

/// <summary>
/// Sensor indicating whether the microphone is in use
/// </summary>
public class MicrophoneProcessSensor(
    int? updateInterval = null,
    string? entityName = MicrophoneProcessSensor.DefaultName,
    string? name = MicrophoneProcessSensor.DefaultName,
    string? id = default,
    string? advancedSettings = default)
    : AbstractSingleValueSensor(
        entityName ?? DefaultName,
        name ?? null,
        updateInterval ?? 10,
        id,
        true,
        advancedSettings: advancedSettings)
{
    private const string DefaultName = "microphoneprocess";

    private const string _lastUsedTimeStop = "LastUsedTimeStop";

    private const string _regKey =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone";


    private readonly Dictionary<string, string> _processes = new();

    private string _attributes = string.Empty;

    public override DiscoveryConfigModel? GetAutoDiscoveryConfig()
    {
        if (Variables.MqttManager == null)
            return null;

        var deviceConfig = Variables.MqttManager.GetDeviceConfigModel();
        if (deviceConfig == null)
        {
            return null;
        }

        var model = new SensorDiscoveryConfigModel
        {
            EntityName = EntityName,
            Name = Name,
            Unique_id = Id,
            Device = deviceConfig,
            State_topic =
                $"{Variables.MqttManager.MqttDiscoveryPrefix()}/{Domain}/{deviceConfig.Name}/{ObjectId}/state",
            State_class = "measurement",
            Availability_topic =
                $"{Variables.MqttManager.MqttDiscoveryPrefix()}/sensor/{deviceConfig.Name}/availability",
            Icon = "mdi:microphone",
            Json_attributes_topic =
                $"{Variables.MqttManager.MqttDiscoveryPrefix()}/{Domain}/{deviceConfig.Name}/{ObjectId}/attributes"
        };

        return AutoDiscoveryConfigModel ?? SetAutoDiscoveryConfigModel(model);
    }

    private string MicrophoneProcess()
    {
        _processes.Clear();

        // first local machine
        using var localMachineKey = Registry.LocalMachine.OpenSubKey(_regKey);
        CheckRegForMicrophoneInUse(localMachineKey);

        // then current user
        using var currentUserKey = Registry.CurrentUser.OpenSubKey(_regKey);
        CheckRegForMicrophoneInUse(currentUserKey);

        // add processes as attributes
        _attributes = _processes.Count > 0 
            ? JsonConvert.SerializeObject(_processes, Formatting.Indented) 
            : "{}";

        // return the count
        return _processes.Count.ToString();
    }

    private void CheckRegForMicrophoneInUse(RegistryKey? key)
    {

        key?
            .GetSubKeyNames()
            .ForEach(ProcessSubKey);

        return;
        
        void ProcessSubKey(string subKeyName)
        {
            if (subKeyName == "NonPackaged")
            {
                using var nonPackagedKey = key.OpenSubKey(subKeyName);
                ProcessSubKeys(nonPackagedKey);
            }
            else
            {
                using var subKey = key.OpenSubKey(subKeyName);
                CheckSubKey(subKey);
            }
        }

        void ProcessSubKeys(RegistryKey? parentKey)
        {
            if (parentKey == null) return;

            parentKey.GetSubKeyNames()
                .Select(parentKey.OpenSubKey)
                .ToList()
                .ForEach(CheckSubKey);
        }

        void CheckSubKey(RegistryKey? subKey)
        {
            if (subKey == null || !subKey.GetValueNames().Contains(_lastUsedTimeStop)) return;

            var endTime = subKey.GetValue(_lastUsedTimeStop) is long time ? time : -1;
            if (endTime > 0) return;
            var processName = SharedHelperFunctions.ParseRegWebcamMicApplicationName(subKey.Name);
            _processes[processName] = "on";
        }
    }

    public override string GetState() => MicrophoneProcess();
    public override string GetAttributes() => _attributes;
}