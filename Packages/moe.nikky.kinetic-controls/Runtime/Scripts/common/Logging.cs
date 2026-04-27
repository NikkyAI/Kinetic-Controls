using System;
using nikkyai.extensions;
using Texel;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

namespace nikkyai.common
{
    public abstract class Logging : BaseBehaviour
    {
        protected abstract DebugLog DebugLog
        {
            get;
            set;
        }

        // [SerializeField] private LogLevel logLevel = LogLevel.INFO;

        // protected string _logPrefix;
        private string logPrefix = "";
        private bool _logPrefixInitialized = false;
        private string _colorPrefix = "";
        private string _colorPostfix = "";
        private bool _colorsInitialized = false;

        private string _path = "";
        private bool _pathInitialized = false;

        private void InitPath()
        {
            // Color _pathColor = new Color(.75f, .75f, .75f, 1f);
            var _pathColor = RichTextColor.teal;

            Transform t = transform;
            _path = name.Color(Color.cyan);
            t = t.parent;
            while (t != null)
            {
                _path = $"{t.name.Color(_pathColor)} / {_path}";
                t = t.parent;
            }
            
            _path = $" / {_path}";

            _pathInitialized = true;
        }

        private void InitColors()
        {
            var c = LogColor;
            // _logPrefix = LogPrefix;
            if (c != Color.white)
            {
                _colorPrefix = $"<color=#{c.ToHex()}>";
                _colorPostfix = "</color>";
            }

            _colorsInitialized = true;
        }

        private void InitLogPrefix()
        {
            if (!_colorsInitialized)
            {
                InitColors();
            }
            if (!_pathInitialized)
            {
                InitPath();
            }
            logPrefix = $"{_colorPrefix}{LogPrefix}{_colorPostfix} @ {_path}";
            _logPrefixInitialized = true;
        }

        protected override void _PreInit()
        {
            base._PreInit();

            if (!_logPrefixInitialized)
            {
                InitLogPrefix();
            }
        }

        protected abstract string LogPrefix { get; }

        protected virtual Color LogColor => Color.white;

        protected void LogError(string message)
        {
            if (!_logPrefixInitialized)
            {
                InitLogPrefix();
            }

            // var logPrefix = $"{_colorPrefix}{LogPrefix}{_colorPostfix} @ {_path}";
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

        protected void LogWarning(string message)
        {
            if (!_logPrefixInitialized)
            {
                InitLogPrefix();
            }

            // var logPrefix = $"{_colorPrefix}{LogPrefix}{_colorPostfix} @ {_path}";
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

        protected void Log(string message)
        {
            if (!_logPrefixInitialized)
            {
                InitLogPrefix();
            }

            // var logPrefix = $"{_colorPrefix}{LogPrefix}{_colorPostfix} @ {_path}";
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

        protected void LogAssert(string message)
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