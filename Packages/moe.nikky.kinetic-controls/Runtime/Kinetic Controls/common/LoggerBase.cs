using Texel;
using UnityEngine;
using VRC.SDKBase;

namespace nikkyai.Kinetic_Controls.common
{
    public abstract class LoggerBase : EventBase
    {
        protected virtual DebugLog DebugLog
        {
            get => null;
            set { }
        }

        protected abstract string LogPrefix { get; }

        protected void LogError(string message)
        {
            Debug.LogError($"[{LogPrefix} {name}] {message}");
// #if UNITY_EDITOR && !COMPILER_UDONSHARP
//             Debug.LogError($"[{LogPrefix}] {message}", gameObject);
// #else
//             Debug.LogError($"[{LogPrefix}] {message}");
// #endif
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
            Debug.LogWarning($"[{LogPrefix} {name}] {message}");
// #if UNITY_EDITOR && !COMPILER_UDONSHARP
//             Debug.LogWarning($"[{LogPrefix}] {message}", gameObject);
// #else
//             Debug.LogWarning($"[{LogPrefix}] {message}");
// #endif
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
            Debug.Log($"[{LogPrefix} {name}] {message}");
// #if UNITY_EDITOR && !COMPILER_UDONSHARP
//             Debug.Log($"[{LogPrefix}] {message}", gameObject);
// #else
//             Debug.Log($"[{LogPrefix}] {message}");
// #endif
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