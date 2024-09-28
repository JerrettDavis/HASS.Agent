using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Cassia;
using HASS.Agent.Shared.Functions;
using Serilog;

#pragma warning disable CS0169 // Field is never used

namespace HASS.Agent.Shared.Managers;

[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "UnusedType.Local")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class SessionsManager
{
    /// <summary>
    /// Returns the currently logged users, by default all (even non-active)
    /// </summary>
    /// <param name="activeOnly"></param>
    /// <returns></returns>
    internal static IEnumerable<string> GetLoggedUsers(bool activeOnly = false)
    {
        var serverHandle = WTSOpenServer(Environment.MachineName);
        var sessionInfoPtr = IntPtr.Zero;

        try
        {
            var sessionCount = 0;
            var retVal = WTSEnumerateSessions(serverHandle, 0, 1, ref sessionInfoPtr, ref sessionCount);
            var dataSize = Marshal.SizeOf(typeof(Cassia.Impl.WTS_SESSION_INFO));
            var currentSession = sessionInfoPtr;

            return retVal == 0
                // nothing found or errored out
                ? GetLoggedUsersBackup()
                : EnumerateSessions(sessionCount, currentSession, dataSize);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "[ACTIVEUSERS] Error while fetching logged users: {err}", ex.Message);
            return [];
        }
        finally
        {
            WTSFreeMemory(sessionInfoPtr);
            WTSCloseServer(serverHandle);
        }

        IEnumerable<string> EnumerateSessions(
            int sessionCount,
            IntPtr currentSession,
            int dataSize)
        {
            return Enumerable.Range(0, sessionCount)
                .Select(_ =>
                {
                    var sessionInfo =
                        (Cassia.Impl.WTS_SESSION_INFO)Marshal.PtrToStructure(currentSession,
                            typeof(Cassia.Impl.WTS_SESSION_INFO))!;
                    currentSession += dataSize;
                    return sessionInfo;
                })
                .Where(sessionInfo => !activeOnly || sessionInfo.State == ConnectionState.Active)
                .SelectMany(sessionInfo => GetUserFromSession(sessionInfo.SessionID))
                .Distinct(); // avoid adding duplicates
        }

        IEnumerable<string> GetUserFromSession(int sessionId)
        {
            WTSQuerySessionInformation(serverHandle, sessionId, WTS_INFO_CLASS.WTSUserName, out var userPtr, out _);

            try
            {
                var user = Marshal.PtrToStringAnsi(userPtr)?.Trim();
                if (string.IsNullOrEmpty(user) || SharedHelperFunctions.UserIsSystemOrServiceAccount(user))
                {
                    yield break;
                }

                yield return user;
            }
            finally
            {
                WTSFreeMemory(userPtr);
            }
        }
    }


    private static IEnumerable<string> GetLoggedUsersBackup()
    {
        try
        {
            return Process.GetProcessesByName("explorer")
                .Select(explorer =>
                {
                    using (explorer) // Automatically dispose the process
                    {
                        var user = SharedHelperFunctions.GetProcessOwner(explorer, false);
                        return string.IsNullOrEmpty(user) || 
                               SharedHelperFunctions.UserIsSystemOrServiceAccount(user)
                            ? null
                            : user;
                    }
                })
                .Where(user => user != null)
                .Distinct()!; // Ensure distinct users, and exclude null values
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "[ACTIVEUSERS] Error while fetching logged users: {err}", ex.Message);
            return [];
        }
    }

    #region IMPORTS

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
    private readonly struct SystemPowerCapabilities
    {
        [MarshalAs(UnmanagedType.U1)] public readonly bool PowerButtonPresent;
        [MarshalAs(UnmanagedType.U1)] public readonly bool SleepButtonPresent;
        [MarshalAs(UnmanagedType.U1)] public readonly bool LidPresent;
        [MarshalAs(UnmanagedType.U1)] public readonly bool SystemS1;
        [MarshalAs(UnmanagedType.U1)] public readonly bool SystemS2;
        [MarshalAs(UnmanagedType.U1)] public readonly bool SystemS3;
        [MarshalAs(UnmanagedType.U1)] public readonly bool SystemS4;
        [MarshalAs(UnmanagedType.U1)] public readonly bool SystemS5;
        [MarshalAs(UnmanagedType.U1)] public readonly bool HiberFilePresent;
        [MarshalAs(UnmanagedType.U1)] public readonly bool FullWake;
        [MarshalAs(UnmanagedType.U1)] public readonly bool VideoDimPresent;
        [MarshalAs(UnmanagedType.U1)] public readonly bool ApmPresent;
        [MarshalAs(UnmanagedType.U1)] public readonly bool UpsPresent;
        [MarshalAs(UnmanagedType.U1)] public readonly bool ThermalControl;
        [MarshalAs(UnmanagedType.U1)] public readonly bool ProcessorThrottle;
        private readonly byte ProcessorMinThrottle;
        private readonly byte ProcessorMaxThrottle;
        [MarshalAs(UnmanagedType.U1)] public readonly bool FastSystemS4;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly byte[] spare2;

        [MarshalAs(UnmanagedType.U1)] public readonly bool DiskSpinDown;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly byte[] spare3;

        [MarshalAs(UnmanagedType.U1)] public readonly bool SystemBatteriesPresent;
        [MarshalAs(UnmanagedType.U1)] public readonly bool BatteriesAreShortTerm;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly BatteryReportingScale[] BatteryScale;

        private readonly SystemPowerState AcOnLineWake;
        private readonly SystemPowerState SoftLidWake;
        private readonly SystemPowerState RtcWake;
        private readonly SystemPowerState MinDeviceWakeState;
        private readonly SystemPowerState DefaultLowLatencyWake;
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    private struct BatteryReportingScale
    {
        private ulong Granularity;
        private ulong Capacity;
    }

    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private enum SystemPowerState
    {
        PowerSystemUnspecified = 0,
        PowerSystemWorking = 1,
        PowerSystemSleeping1 = 2,
        PowerSystemSleeping2 = 3,
        PowerSystemSleeping3 = 4,
        PowerSystemHibernate = 5,
        PowerSystemShutdown = 6,
        PowerSystemMaximum = 7
    }

    [DllImport("powrprof.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetPwrCapabilities(out SystemPowerCapabilities systemPowerCapabilites);

    private static SystemPowerCapabilities SystemPowerCapabilites;

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool GetTokenInformation(IntPtr tokenHandle, TokenInformationClass tokenInformationClass,
        IntPtr tokenInformation, int tokenInformationLength, out int returnLength);

    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private enum TokenInformationClass
    {
        TokenElevationType
    }

    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private enum TokenElevationType
    {
        TokenElevationTypeDefault = 1,
        TokenElevationTypeFull,
        TokenElevationTypeLimited
    }

    [DllImport("user32.dll", EntryPoint = "GetSystemMetrics")]
    private static extern int GetSystemMetrics(int nIndex);


    // Ingelogde Users imports
    [DllImport("wtsapi32.dll")]
    private static extern IntPtr WTSOpenServer([MarshalAs(UnmanagedType.LPStr)] string pServerName);

    [DllImport("wtsapi32.dll")]
    private static extern void WTSCloseServer(IntPtr hServer);

    [DllImport("wtsapi32.dll")]
    private static extern int WTSEnumerateSessions(
        IntPtr hServer,
        [MarshalAs(UnmanagedType.U4)] int Reserved,
        [MarshalAs(UnmanagedType.U4)] int Version,
        ref IntPtr ppSessionInfo,
        [MarshalAs(UnmanagedType.U4)] ref int pCount);

    [DllImport("wtsapi32.dll")]
    private static extern void WTSFreeMemory(IntPtr pMemory);

    [DllImport("wtsapi32.dll")]
    private static extern bool WTSQuerySessionInformation(
        IntPtr hServer, int sessionId, WTS_INFO_CLASS wtsInfoClass, out IntPtr ppBuffer, out uint pBytesReturned);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct WTS_SESSION_INFO
    {
        private readonly int SessionID;

        [MarshalAs(UnmanagedType.LPStr)] private readonly string pWinStationName;

        private readonly WTS_CONNECTSTATE_CLASS State;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct WTS_CLIENT_ADDRESS
    {
        private readonly int iAddressFamily;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        private readonly byte[] bAddress;
    }

    private enum WTS_INFO_CLASS
    {
        WTSInitialProgram,
        WTSApplicationName,
        WTSWorkingDirectory,
        WTSOEMId,
        WTSSessionId,
        WTSUserName,
        WTSWinStationName,
        WTSDomainName,
        WTSConnectState,
        WTSClientBuildNumber,
        WTSClientName,
        WTSClientDirectory,
        WTSClientProductId,
        WTSClientHardwareId,
        WTSClientAddress,
        WTSClientDisplay,
        WTSClientProtocolType
    }

    private enum WTS_CONNECTSTATE_CLASS
    {
        WTSActive,
        WTSConnected,
        WTSConnectQuery,
        WTSShadow,
        WTSDisconnected,
        WTSIdle,
        WTSListen,
        WTSReset,
        WTSDown,
        WTSInit
    }

    #endregion
}