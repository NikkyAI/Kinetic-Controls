using Texel;
using UnityEngine;
using VRC.SDKBase;

namespace nikkyai.common
{
    public abstract class LoggerBase : EventBase
    {
        [Header("Debug")] // header
        [SerializeField]
        private DebugLog debugLog;

        protected DebugLog DebugLog
        {
            get => debugLog;
            set => debugLog = value;
        }

        protected abstract string LogPrefix { get; }

        protected void LogError(string message)
        {
            Debug.LogError($"[{LogPrefix}] {message}");
#if UNITY_EDITOR && !COMPILER_UDONSHARP
            return;
#endif
            if (Utilities.IsValid(DebugLog))
            {
                DebugLog._WriteError(
                    LogPrefix,
                    message
                );
            }
        }

        protected void LogWarning(string message)
        {
            Debug.LogWarning($"[{LogPrefix}] {message}");
#if UNITY_EDITOR && !COMPILER_UDONSHARP
            return;
#endif
            if (Utilities.IsValid(DebugLog))
            {
                DebugLog._WriteError(
                    LogPrefix,
                    message
                );
            }
        }

        protected void Log(string message)
        {
            Debug.Log($"[{LogPrefix}] {message}");
#if UNITY_EDITOR && !COMPILER_UDONSHARP
            return;
#endif
            if (Utilities.IsValid(DebugLog))
            {
                DebugLog._Write(
                    LogPrefix,
                    message
                );
            }
        }
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public DebugLog EditorDebugLog
        {
            get => DebugLog;
            set => DebugLog = value;
        }
#endif
    }
}