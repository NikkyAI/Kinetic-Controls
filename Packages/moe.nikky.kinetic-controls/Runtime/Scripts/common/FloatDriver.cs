using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;

namespace nikkyai.common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class FloatDriver: LoggingSimple
    {
        [FormerlySerializedAs("range")] // 
        [SerializeField]
        protected bool useRemapRange = false;
        [SerializeField]
        [Tooltip("remaps nroamlized (0-1) values to the provided range, values are NOT clamped")]
        protected Vector2 remapRange = new Vector2(0, 1);
        
        protected abstract void OnUpdateFloat(float value);

        public void UpdateFloatRescale(float normalizedValue)
        {
            var floatValue = normalizedValue;
            if (useRemapRange)
            {
                floatValue = Mathf.LerpUnclamped(remapRange.x, remapRange.y, floatValue);
            }
            OnUpdateFloat(floatValue);
        }

        // defaults for Modern UI slider
        // ReSharper disable once InconsistentNaming
        [HideInInspector, UsedImplicitly] public float sliderValue;
        [UsedImplicitly] 
        public void _SliderUpdated()
        {
            var floatValue = sliderValue;
            if (useRemapRange)
            {
                floatValue = Mathf.LerpUnclamped(remapRange.x, remapRange.y, floatValue);
            }
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