using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using ByteSizeLib;
using HASS.Agent.Shared.Functions;
using HASS.Agent.Shared.HomeAssistant.Sensors.GeneralSensors.MultiValue.DataTypes;
using HASS.Agent.Shared.Models.HomeAssistant;
using HASS.Agent.Shared.Models.Internal;
using Newtonsoft.Json;
using Serilog;

#pragma warning disable CS1591

namespace HASS.Agent.Shared.HomeAssistant.Sensors.GeneralSensors.MultiValue;

/// <summary>
/// Multivalued sensor containing local storage info
/// </summary>
public class StorageSensors : AbstractMultiValueSensor
{
    private const string DefaultName = "storage";
    private readonly int _updateInterval;

    public sealed override Dictionary<string, AbstractSingleValueSensor> Sensors { get; protected set; } = new();

    public StorageSensors(
        int? updateInterval = null,
        string? entityName = DefaultName,
        string? name = DefaultName,
        string? id = default) :
        base(entityName ?? DefaultName, name ?? null, updateInterval ?? 30, id)
    {
        _updateInterval = updateInterval ?? 30;

        UpdateSensorValues();
    }

    private void AddUpdateSensor(
        string sensorId,
        AbstractSingleValueSensor sensor) =>
        Sensors[sensorId] = sensor;

    public sealed override void UpdateSensorValues()
    {
        var parentSensorSafeName = SharedHelperFunctions.GetSafeValue(EntityName);

        var validDrives = GetValidDrives().ToList();

        validDrives.ForEach(drive =>
            ExecuteAndTrapDriveExceptions(() => ProcessDrive(drive)));

        var driveCount = validDrives.Count;

        var driveCountEntityName = $"{parentSensorSafeName}_total_disk_count";
        var driveCountId = $"{Id}_total_disk_count";

        var driveCountSensor = new DataTypeIntSensor(
            _updateInterval,
            driveCountEntityName,
            "Total Disk Count",
            driveCountId,
            string.Empty,
            "measurement",
            "mdi:harddisk",
            string.Empty,
            EntityName
        );

        driveCountSensor.SetState(driveCount);
        AddUpdateSensor(driveCountId, driveCountSensor);

        return;

        IEnumerable<DriveInfo> GetValidDrives() =>
            DriveInfo.GetDrives()
                .Where(d => d is { IsReady: true, DriveType: DriveType.Fixed } && 
                            !string.IsNullOrWhiteSpace(d.Name));

        void ProcessDrive(DriveInfo drive)
        {
            var storageInfo = CreateStorageInfo(drive);
            CreateDriveSensor(drive, storageInfo);
        }

        void CreateDriveSensor(DriveInfo drive, StorageInfo storageInfo)
        {
            var driveNameLower = storageInfo.Name.ToLower();
            var sensorValue = string.IsNullOrEmpty(drive.VolumeLabel) ? storageInfo.Name : drive.VolumeLabel;

            var driveInfoEntityName = $"{parentSensorSafeName}_{driveNameLower}";
            var driveInfoId = $"{Id}_{driveNameLower}";

            var driveInfoSensor = new DataTypeStringSensor(
                _updateInterval,
                driveInfoEntityName,
                storageInfo.Name,
                driveInfoId,
                string.Empty,
                "mdi:harddisk",
                string.Empty,
                EntityName,
                true
            );

            driveInfoSensor.SetState(sensorValue);
            driveInfoSensor.SetAttributes(JsonConvert.SerializeObject(storageInfo, Formatting.Indented));

            AddUpdateSensor(driveInfoId, driveInfoSensor);
        }

        void ExecuteAndTrapDriveExceptions(Action action)
        {
            try
            {
                action();
            }
            catch (SystemException ex) when (ex is UnauthorizedAccessException or SecurityException)
            {
                Log.Fatal(ex, "[STORAGE] [{name}] Disk access denied: {msg}", EntityName, ex.Message);
            }
            catch (DriveNotFoundException ex)
            {
                Log.Fatal(ex, "[STORAGE] [{name}] Disk not found: {msg}", EntityName, ex.Message);
            }
            catch (IOException ex)
            {
                Log.Fatal(ex, "[STORAGE] [{name}] Disk IO error: {msg}", EntityName, ex.Message);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "[STORAGE] [{name}] Error querying disk: {msg}", EntityName, ex.Message);
            }
        }

        StorageInfo CreateStorageInfo(DriveInfo drive)
        {
            var driveName = drive.Name[..1].ToUpper();
            var driveLabel = string.IsNullOrEmpty(drive.VolumeLabel) ? "-" : drive.VolumeLabel;

            var totalSizeMb = CalcInMbFromBytes(drive.TotalSize);
            var availableSpaceMb = CalcInMbFromBytes(drive.AvailableFreeSpace);
            var usedSpaceMb = totalSizeMb - availableSpaceMb;

            return new StorageInfo
            {
                Name = driveName,
                Label = driveLabel,
                FileSystem = drive.DriveFormat,
                TotalSizeMB = totalSizeMb,
                AvailableSpaceMB = availableSpaceMb,
                UsedSpaceMB = usedSpaceMb,
                AvailableSpacePercentage = CalcPercentage(availableSpaceMb),
                UsedSpacePercentage = CalcPercentage(usedSpaceMb)
            };

            int CalcPercentage(double size) => (int)Math.Round(size / totalSizeMb * 100);
            double CalcInMbFromBytes(double size) => Math.Round(ByteSize.FromBytes(size).MegaBytes);
        }
    }

    public override DiscoveryConfigModel? GetAutoDiscoveryConfig() => null;
}