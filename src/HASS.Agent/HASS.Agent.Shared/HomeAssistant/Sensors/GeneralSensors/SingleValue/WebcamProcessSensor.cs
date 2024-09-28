using System.Collections.Generic;
using System.Linq;
using HASS.Agent.Shared.Extensions;
using HASS.Agent.Shared.Functions;
using HASS.Agent.Shared.Models.HomeAssistant;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace HASS.Agent.Shared.HomeAssistant.Sensors.GeneralSensors.SingleValue;

/// <summary>
/// Sensor indicating whether the webcam is in use
/// </summary>
public class WebcamProcessSensor(
    int? updateInterval = null,
    string? entityName = WebcamProcessSensor.DefaultName,
    string? name = WebcamProcessSensor.DefaultName,
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
    private const string DefaultName = "webcamprocess";

    private readonly Dictionary<string, string> _processes = new();

    private string _attributes = string.Empty;

    public override string GetState() => WebcamProcess();
    public void SetAttributes(string value) => 
        _attributes = string.IsNullOrWhiteSpace(value) ? "{}" : value;
    public override string GetAttributes() => _attributes;

    public override DiscoveryConfigModel? GetAutoDiscoveryConfig()
    {
        if (Variables.MqttManager == null) return null;

        var deviceConfig = Variables.MqttManager.GetDeviceConfigModel();
        if (deviceConfig == null) return null;

        var model = new SensorDiscoveryConfigModel
        {
            EntityName = EntityName,
            Name = Name,
            Unique_id = Id,
            Device = deviceConfig,
            State_topic = $"{Variables.MqttManager.MqttDiscoveryPrefix()}/{Domain}/{deviceConfig.Name}/{ObjectId}/state",
            State_class = "measurement",
            Availability_topic = $"{Variables.MqttManager.MqttDiscoveryPrefix()}/sensor/{deviceConfig.Name}/availability",
            Icon = "mdi:webcam"
        };

        if (UseAttributes)
        {
            model.Json_attributes_topic = $"{Variables.MqttManager.MqttDiscoveryPrefix()}/{Domain}/{deviceConfig.Name}/{ObjectId}/attributes";
        }

        return AutoDiscoveryConfigModel ?? SetAutoDiscoveryConfigModel(model);
    }

    private string WebcamProcess()
    {
        const string regKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam";

        _processes.Clear();

        // first local machine
        using var localMachineKey = Registry.LocalMachine.OpenSubKey(regKey);
        CheckRegForWebcamInUse(localMachineKey);

        // then current user
        using var currentUserKey = Registry.CurrentUser.OpenSubKey(regKey);
        CheckRegForWebcamInUse(currentUserKey);

        // add processes as attributes
        if (_processes.Count > 0) 
            _attributes = JsonConvert.SerializeObject(_processes, Formatting.Indented);

        // return the count
        return _processes.Count.ToString();
    }

    private void CheckRegForWebcamInUse(RegistryKey? key)
    {
        key?.GetSubKeyNames()
            .Select(key.OpenSubKey)
            .Where(subKey => subKey != null)
            .ForEach(ProcessKey);
        
        return;

        void ProcessKey(RegistryKey? subKey)
        {
            if (subKey == null) return;

            if (subKey.Name.Contains("NonPackaged"))
                ProcessSubKeys(subKey);
            else if (IsWebcamInUse(subKey)) 
                AddProcess(subKey);
        }

        void ProcessSubKeys(RegistryKey nonpackagedKey)
        {
            nonpackagedKey.GetSubKeyNames()
                .Select(nonpackagedKey.OpenSubKey)
                .Where(subKey => subKey != null && IsWebcamInUse(subKey))
                .ForEach(AddProcess!);
        }

        bool IsWebcamInUse(RegistryKey subKey)
        {
            var lastUsedTimeStop = subKey.GetValue("LastUsedTimeStop");
            return lastUsedTimeStop is long and <= 0;
        }

        void AddProcess(RegistryKey subKey)
        {
            var appName = SharedHelperFunctions.ParseRegWebcamMicApplicationName(subKey.Name);
            _processes.Add(appName, "on");
        }
    }
}