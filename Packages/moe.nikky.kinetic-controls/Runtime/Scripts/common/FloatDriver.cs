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
        private Vector2 remapRange = new Vector2(0, 1);
        
        protected abstract void UpdateFloat(float value);

        public void UpdateFloatRescale(float normalizedValue)
        {
            var floatValue = Mathf.LerpUnclamped(remapRange.x, remapRange.y, normalizedValue);
            UpdateFloat(floatValue);
        }

        [NonSerialized, UsedImplicitly] public float sliderValue;
        [UsedImplicitly] 
        public void _SliderUpdated()
        {
            var floatValue = Mathf.Lerp(remapRange.x, remapRange.y, sliderValue);
            UpdateFloat(floatValue);
            // UpdateFloat(sliderValue);
        }
#if UNITY_EDITOR && !COMPILER_UDONSHARP

        public virtual void ApplyFloatValue(float value)
        {
        }
#endif
    }
}