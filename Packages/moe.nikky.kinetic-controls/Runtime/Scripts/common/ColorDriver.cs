using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;

namespace nikkyai.common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class ColorDriver: LoggerBase
    {
        public abstract void OnUpdateColor(Color value);

        protected Color cachedValue = Color.clear;
#if UNITY_EDITOR && !COMPILER_UDONSHARP

        public virtual void ApplyColorValue(Color value)
        {
            _EnsureInit();
            cachedValue = value;
        }
#endif
    }
}