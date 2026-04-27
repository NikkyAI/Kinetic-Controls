using Texel;
using UnityEngine;

namespace nikkyai.common
{
    public abstract class LoggingSimple : Logging
    {
        [Header("Logging")] // header
        [SerializeField]
        private DebugLog debugLog;

        protected override DebugLog DebugLog
        {
            get => debugLog;
            set => debugLog = value;
        }
    }
}