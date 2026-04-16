using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;

namespace nikkyai.common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class FloatDriver: LoggerBase
    {
        [FormerlySerializedAs("range")] // 
        [SerializeField, InspectorName("remap range")]
        protected Vector2 remapRange = new Vector2(0, 1);
        
        protected abstract void OnUpdateFloat(float value);

        public void UpdateFloatRescale(float normalizedValue)
        {
            var floatValue = Mathf.LerpUnclamped(remapRange.x, remapRange.y, normalizedValue);
            OnUpdateFloat(floatValue);
        }

        // defaults for Modern UI slider
        // ReSharper disable once InconsistentNaming
        [NonSerialized, UsedImplicitly] public float sliderValue;
        [UsedImplicitly] 
        public void _SliderUpdated()
        {
            var floatValue = Mathf.Lerp(remapRange.x, remapRange.y, sliderValue);
            OnUpdateFloat(floatValue);
        }
        
        protected float cachedValue = float.NaN;
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public virtual void ApplyFloatValue(float value)
        {
            _EnsureInit();
            cachedValue = value;
        }
#endif
    }
}