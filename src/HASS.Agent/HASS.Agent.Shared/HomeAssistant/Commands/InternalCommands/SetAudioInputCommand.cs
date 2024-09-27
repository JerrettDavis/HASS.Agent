using System;
using HASS.Agent.Shared.Enums;
using HASS.Agent.Shared.Managers.Audio;
using Serilog;

namespace HASS.Agent.Shared.HomeAssistant.Commands.InternalCommands;

public class SetAudioInputCommand : InternalCommand
{
    private const string DefaultName = "setaudioinput";

    private string InputDevice => CommandConfig;

    public SetAudioInputCommand(
        string? entityName = DefaultName,
        string? name = DefaultName,
        string audioDevice = "",
        CommandEntityType entityType = CommandEntityType.Button,
        string? id = default) :
        base(entityName ?? DefaultName, name ?? null, audioDevice, entityType, id)
    {
        State = "OFF";
    }

    public override void TurnOn()
    {
        if (string.IsNullOrWhiteSpace(InputDevice))
        {
            Log.Error("[SETAUDIOIN] Error, input device name cannot be null/blank");

            return;
        }

        TurnOnWithAction(InputDevice);
    }

    public override void TurnOnWithAction(string action)
    {
        State = "ON";

        try
        {
            AudioManager.ActivateDevice(action);
        }
        catch (Exception ex)
        {
            Log.Error("[SETAUDIOIN] Error while processing action '{action}': {err}", action, ex.Message);
        }
        finally
        {
            State = "OFF";
        }
    }
}