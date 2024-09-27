﻿using System;
using System.Diagnostics.CodeAnalysis;
using HASS.Agent.Shared.Enums;
using HASS.Agent.Shared.Managers.Audio;
using Serilog;

namespace HASS.Agent.Shared.HomeAssistant.Commands.InternalCommands;

/// <summary>
/// Command to set the system's audio volume level
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
public class SetVolumeCommand : InternalCommand
{
    private const string DefaultName = "setvolume";
    private readonly int _volume = -1;

    public SetVolumeCommand(
        string? entityName = DefaultName, 
        string? name = DefaultName, 
        string volume = "",
        CommandEntityType entityType = CommandEntityType.Button, 
        string? id = default) : 
        base(entityName ?? DefaultName, name ?? null, volume, entityType, id)
    {
        if (!string.IsNullOrWhiteSpace(volume))
        {
            var parsed = int.TryParse(volume, out var volumeInt);
            if (!parsed)
            {
                Log.Error("[SETVOLUME] [{name}] Unable to parse configured volume level, not an int: {val}", EntityName,
                    volume);
                _volume = -1;
            }

            _volume = volumeInt;
        }

        State = "OFF";
    }

    public override void TurnOn()
    {
        State = "ON";

        try
        {
            if (_volume == -1f)
            {
                Log.Warning(
                    "[SETVOLUME] [{name}] Unable to trigger command, it's configured as action-only",
                    EntityName);

                return;
            }

            AudioManager.SetDefaultDeviceProperties(
                DeviceType.Output, 
                DeviceRole.Multimedia | DeviceRole.Console,
                _volume, 
                null);
        }
        catch (Exception ex)
        {
            Log.Error("[SETVOLUME] [{name}] Error while processing: {err}", EntityName, ex.Message);
        }
        finally
        {
            State = "OFF";
        }
    }

    public override void TurnOnWithAction(string action)
    {
        State = "ON";

        try
        {
            var parsed = int.TryParse(action, out _); // TODO: Confirm this is valid.
            if (!parsed)
            {
                Log.Error(
                    "[SETVOLUME] [{name}] Unable to trigger command, the provided action value can't be parsed: {val}",
                    EntityName, action);

                return;
            }

            // TODO: Confirm if this is the correct way to set the volume or if volumeInt should be used instead
            AudioManager.SetDefaultDeviceProperties(
                DeviceType.Output, 
                DeviceRole.Multimedia | DeviceRole.Console,
                _volume, 
                null);
        }
        catch (Exception ex)
        {
            Log.Error("[SETVOLUME] [{name}] Error while processing action: {err}", EntityName, ex.Message);
        }
        finally
        {
            State = "OFF";
        }
    }
}