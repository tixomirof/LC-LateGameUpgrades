﻿using BepInEx.Logging;
using HarmonyLib;

namespace MoreShipUpgrades.Misc
{
    public class LguLogger
    {
        readonly string moduleName;
        readonly ManualLogSource logSource;
        public LguLogger(string moduleName)
        {
            this.moduleName = moduleName;
            logSource = Plugin.mls;
        }
        public void LogCodeInstruction(CodeInstruction code)
        {
            LogDebug(code.opcode + ", " + code.operand);
        }
        public void LogDebug(object message)
        {
            logSource.LogDebug($"[{moduleName}] {message}");
        }
        public void LogMessage(object message)
        {
            logSource.LogMessage($"[{moduleName}] {message}");
        }
        public void LogInfo(object message)
        {
            logSource.LogInfo($"[{moduleName}] {message}");
        }
        public void LogWarning(object message)
        {
            logSource.LogWarning($"[{moduleName}] {message}");
        }
        public void LogError(object message)
        {
            logSource.LogError($"[{moduleName}] {message}");
        }
    }
}
