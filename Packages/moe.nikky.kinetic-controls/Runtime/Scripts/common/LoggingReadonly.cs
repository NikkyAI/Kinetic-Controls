#define HIDE_INSPECTOR

using System;
using nikkyai.extensions;
using Texel;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

namespace nikkyai.common
{
    public abstract class LoggingReadonly : Logging
    {
        [Header("Logging")] // header
        [ReadOnly]
        private DebugLog debugLog;

        protected override DebugLog DebugLog
        {
            get => debugLog;
            set => debugLog = value;
        }
    }
}