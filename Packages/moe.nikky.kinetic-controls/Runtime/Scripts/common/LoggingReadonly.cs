#define READONLY

using System;
using nikkyai.attribute;
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