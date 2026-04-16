using System;
using Texel;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

namespace nikkyai.common
{
    public abstract class LoggerBase : BaseBehaviour
    {
        [Header("Logging")] // header
        [SerializeField]
        private DebugLog debugLog;

        protected DebugLog DebugLog
        {
            get => debugLog;
            set => debugLog = value;
        }

        // [SerializeField] private LogLevel logLevel = LogLevel.INFO;

        // protected string _logPrefix;
        private string logPrefix = "";
        private string _colorPrefix = "";
        private string _colorPostfix = "";

        protected override void _PreInit()
        {
            base._PreInit();

            var c = LogColor;
            // _logPrefix = LogPrefix;
            if (c != Color.white)
            {
                _colorPrefix = string.Format(
                    "<color=#{0:X2}{1:X2}{2:X2}>",
                    (byte)Mathf.Clamp01(c.r) * 255,
                    (byte)Mathf.Clamp01(c.g) * 255,
                    (byte)Mathf.Clamp01(c.b) * 255
                );
                _colorPostfix = "</color>";
            }
            logPrefix = $"{_colorPrefix}{LogPrefix}{_colorPostfix}";
        }

        protected abstract string LogPrefix { get; }

        protected virtual Color LogColor => Color.white;

        protected override void LogError(string message)
        {
            var logPrefix = $"{_colorPrefix}{LogPrefix}{_colorPostfix}";
            Debug.LogError($"[{logPrefix}] {message}", this);
#if UNITY_EDITOR && !COMPILER_UDONSHARP
            return;
#endif
            if (Utilities.IsValid(DebugLog))
            {
                DebugLog._WriteError(
                    logPrefix,
                    message
                );
            }
        }

        protected override void LogWarning(string message)
        {
            var logPrefix = $"{_colorPrefix}{LogPrefix}{_colorPostfix}";
            Debug.LogWarning($"[{logPrefix}] {message}", this);
#if UNITY_EDITOR && !COMPILER_UDONSHARP
            return;
#endif
            if (Utilities.IsValid(DebugLog))
            {
                DebugLog._WriteError(
                    logPrefix,
                    message
                );
            }
        }

        protected override void Log(string message)
        {
            var logPrefix = $"{_colorPrefix}{LogPrefix}{_colorPostfix}";
            Debug.Log($"[{logPrefix}] {message}", this);
#if UNITY_EDITOR && !COMPILER_UDONSHARP
            return;
#endif
            if (Utilities.IsValid(DebugLog))
            {
                DebugLog._Write(
                    logPrefix,
                    message
                );
            }
        }

        /*
        protected void LogTrace(string message)
        {
            if (logLevel < LogLevel.TRACE) return;
            var logPrefix = $"[TRC] {_colorPrefix}{LogPrefix}{_colorPostfix}";
            Debug.Log($"[{logPrefix}] {message}", this);
#if UNITY_EDITOR && !COMPILER_UDONSHARP
            return;
#endif
            if (Utilities.IsValid(DebugLog))
            {
                DebugLog._Write(
                    logPrefix,
                    message
                );
            }
        }
        protected void LogDebug(string message)
        {
            if (logLevel < LogLevel.DEBUG) return;
            var logPrefix = $"[DBG] {_colorPrefix}{LogPrefix}{_colorPostfix}";
            Debug.Log($"[{logPrefix}] {message}", this);
#if UNITY_EDITOR && !COMPILER_UDONSHARP
            return;
#endif
            if (Utilities.IsValid(DebugLog))
            {
                DebugLog._Write(
                    logPrefix,
                    message
                );
            }
        }
        protected void LogInfo(string message)
        {
            if (logLevel < LogLevel.INFO) return;
            var logPrefix = $"[INF] {_colorPrefix}{LogPrefix}{_colorPostfix}";
            Debug.Log($"[{logPrefix}] {message}", this);
#if UNITY_EDITOR && !COMPILER_UDONSHARP
            return;
#endif
            if (Utilities.IsValid(DebugLog))
            {
                DebugLog._Write(
                    logPrefix,
                    message
                );
            }
        }
        */

        protected override void LogAssert(string message)
        {
            var logPrefix = $"{_colorPrefix}{LogPrefix}{_colorPostfix}";
            Debug.LogAssertion($"[{logPrefix}] {message}", this);
#if UNITY_EDITOR && !COMPILER_UDONSHARP
            return;
#endif
            if (Utilities.IsValid(DebugLog))
            {
                DebugLog._Write(
                    logPrefix,
                    message
                );
            }
        }
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public DebugLog EditorDebugLog
        {
            get => DebugLog;
            set
            {
                // if (value != null)
                // {
                //     Log($"Setting DebugLog to {value} on {name}");
                // }
                // else
                // {
                //     Log($"Setting DebugLog to null on {name}");
                // }
                if (DebugLog != value)
                {
                    EditorUtility.SetDirty(this);
                }
                DebugLog = value;
            }
        }
#endif
    }
}