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
        [FormerlySerializedAs("useRemapRange")]
        [Header("Value Remapping")]
        [SerializeField]
        protected bool enableValueRemapping = false;
        [SerializeField]
        // [Tooltip("remaps nroamlized (0-1) values to the provided range, values are NOT clamped")]
        protected Vector2 remapFrom = new Vector2(0, 1);
        [SerializeField]
        [FormerlySerializedAs("remapRange")]
        // [Tooltip("remaps normalized (0-1) values to the provided range, values are NOT clamped")]
        protected Vector2 remapTo = new Vector2(0, 1);
        
        protected abstract void OnUpdateFloat(float value);

        public void UpdateFloatRescale(float inputValue)
        {
            // var inputValue = inputValue;
            if (enableValueRemapping)
            {
                inputValue = Mathf.InverseLerp(remapFrom.x, remapFrom.y, inputValue);
                inputValue = Mathf.LerpUnclamped(remapTo.x, remapTo.y, inputValue);
            }
            OnUpdateFloat(inputValue);
        }

        // defaults for Modern UI slider
        // ReSharper disable once InconsistentNaming
        [HideInInspector, UsedImplicitly] public float sliderValue;
        [UsedImplicitly] 
        public void _SliderUpdated()
        {
            var floatValue = sliderValue;
            if (enableValueRemapping)
            {
                floatValue = Mathf.LerpUnclamped(remapTo.x, remapTo.y, floatValue);
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