using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using ByteSizeLib;
using HASS.Agent.Shared.Functions;
using HASS.Agent.Shared.HomeAssistant.Sensors.GeneralSensors.MultiValue.DataTypes;
using HASS.Agent.Shared.Models.HomeAssistant;
using HASS.Agent.Shared.Models.Internal;
using Newtonsoft.Json;
using Serilog;

namespace HASS.Agent.Shared.HomeAssistant.Sensors.GeneralSensors.MultiValue;

/// <summary>
/// Multivalue sensor containing network and NIC info
/// </summary>
public class NetworkSensors : AbstractMultiValueSensor
{
    private const string DefaultName = "network";
    private readonly int _updateInterval;

    public string NetworkCard { get; protected set; }
    private readonly bool _useSpecificCard;

    public sealed override Dictionary<string, AbstractSingleValueSensor> Sensors { get; protected set; } = new();

    public NetworkSensors(
        int? updateInterval = null,
        string? entityName = DefaultName,
        string? name = DefaultName,
        string networkCard = "*",
        string? id = default) :
        base(entityName ?? DefaultName, name ?? null, updateInterval ?? 30, id)
    {
        _updateInterval = updateInterval ?? 30;

        NetworkCard = networkCard;
        _useSpecificCard = networkCard != "*" && !string.IsNullOrEmpty(networkCard);

        UpdateSensorValues();
    }

    private void AddUpdateSensor(
        string sensorId,
        AbstractSingleValueSensor sensor) =>
        Sensors[sensorId] = sensor;

    public sealed override void UpdateSensorValues()
    {
        var parentSensorSafeName = SharedHelperFunctions.GetSafeValue(EntityName);

        var nicCount = 0;
        var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

        foreach (var nic in networkInterfaces)
        {
            try
            {
                if (nic == null!)
                    continue;

                if (ShouldSkipNic(nic))
                    continue;

                var id = GetNicId(nic);
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                var networkInfo = CreateNetworkInfo(nic);

                AddNetworkInfoSensor(parentSensorSafeName, id, networkInfo, nic);
                nicCount++;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "[NETWORK] [{name}] Error querying NIC: {msg}",
                    EntityName, ex.Message);
            }
        }

        AddNicCountSensor(parentSensorSafeName, nicCount);
    }

    private bool ShouldSkipNic(NetworkInterface nic) => 
        _useSpecificCard && nic.Id != NetworkCard;

    private static string GetNicId(NetworkInterface nic) => 
        nic.Id.Replace("{", "").Replace("}", "").Replace("-", "").ToLower();

    private NetworkInfo CreateNetworkInfo(NetworkInterface nic)
    {
        var networkInfo = new NetworkInfo
        {
            Name = nic.Name,
            NetworkInterfaceType = nic.NetworkInterfaceType.ToString(),
            SpeedBitsPerSecond = nic.Speed,
            OperationalStatus = nic.OperationalStatus.ToString()
        };

        var interfaceStats = nic.GetIPv4Statistics();
        networkInfo.DataReceivedMB = Math.Round(
            ByteSize.FromBytes(interfaceStats.BytesReceived).MegaBytes);
        networkInfo.DataSentMB = Math.Round(
            ByteSize.FromBytes(interfaceStats.BytesSent).MegaBytes);
        networkInfo.IncomingPacketsDiscarded = interfaceStats.IncomingPacketsDiscarded;
        networkInfo.IncomingPacketsWithErrors = interfaceStats.IncomingPacketsWithErrors;
        networkInfo.IncomingPacketsWithUnknownProtocol = interfaceStats.IncomingUnknownProtocolPackets;
        networkInfo.OutgoingPacketsDiscarded = interfaceStats.OutgoingPacketsDiscarded;
        networkInfo.OutgoingPacketsWithErrors = interfaceStats.OutgoingPacketsWithErrors;

        PopulateNetworkProperties(nic, networkInfo);

        return networkInfo;
    }

    private static void PopulateNetworkProperties(
        NetworkInterface nic, 
        NetworkInfo networkInfo)
    {
        var nicProperties = nic.GetIPProperties();

        foreach (var unicast in nicProperties.UnicastAddresses)
        {
            var ip = unicast.Address.ToString();
            if (!string.IsNullOrEmpty(ip) && !networkInfo.IpAddresses.Contains(ip))
                networkInfo.IpAddresses.Add(ip);

            var mac = nic.GetPhysicalAddress().ToString();
            if (!string.IsNullOrEmpty(mac) && !networkInfo.MacAddresses.Contains(mac))
                networkInfo.MacAddresses.Add(mac);
        }

        networkInfo.Gateways.AddRange(GetPopulatedProperties(
            nicProperties.GatewayAddresses, gw => gw.Address.ToString()));
        networkInfo.DhcpAddresses.AddRange(GetPopulatedProperties(
            nicProperties.DhcpServerAddresses, dhcp => dhcp.ToString()));
        networkInfo.DhcpEnabled = nicProperties.GetIPv4Properties().IsDhcpEnabled;
        networkInfo.DnsAddresses.AddRange(GetPopulatedProperties(
            nicProperties.DnsAddresses, dns => dns.ToString()));
        networkInfo.DnsEnabled = nicProperties.IsDnsEnabled;
        networkInfo.DnsSuffix = nicProperties.DnsSuffix;
    }

    private static List<string> GetPopulatedProperties<T>(
        IEnumerable<T> addresses,
        Func<T, string> selector)
    {
        return addresses.Select(selector)
            .Where(address => !string.IsNullOrEmpty(address))
            .ToList();
    }

    private void AddNetworkInfoSensor(string parentSensorSafeName, string id,
        NetworkInfo networkInfo,
        NetworkInterface nic)
    {
        var networkInfoEntityName = $"{parentSensorSafeName}_{id}";
        var networkInfoId = $"{Id}_{id}";
        var networkInfoSensor = new DataTypeStringSensor(
            _updateInterval,
            networkInfoEntityName,
            nic.Name,
            networkInfoId,
            string.Empty,
            "mdi:lan",
            string.Empty,
            EntityName,
            true);

        networkInfoSensor.SetState(nic.OperationalStatus.ToString());

        var info = JsonConvert.SerializeObject(networkInfo, Formatting.Indented);
        networkInfoSensor.SetAttributes(info);

        AddUpdateSensor(networkInfoId, networkInfoSensor);
    }

    private void AddNicCountSensor(string parentSensorSafeName, int nicCount)
    {
        var nicCountEntityName = $"{parentSensorSafeName}_total_network_card_count";
        var nicCountId = $"{Id}_total_network_card_count";
        var nicCountSensor = new DataTypeIntSensor(
            _updateInterval,
            nicCountEntityName,
            "Network Card Count",
            nicCountId,
            string.Empty,
            "measurement",
            "mdi:lan",
            string.Empty,
            EntityName);

        nicCountSensor.SetState(nicCount);
        AddUpdateSensor(nicCountId, nicCountSensor);
    }

    public override DiscoveryConfigModel? GetAutoDiscoveryConfig() => null;
}