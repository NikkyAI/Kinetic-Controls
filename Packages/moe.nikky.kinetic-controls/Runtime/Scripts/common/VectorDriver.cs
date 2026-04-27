using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;

namespace nikkyai.common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class VectorDriver: LoggingSimple
    {
        // [FormerlySerializedAs("range")] // 
        // [SerializeField, InspectorName("remap range")]
        // protected Vector2 remapRange = new Vector2(0, 1);
        
        protected abstract void OnUpdateVector(Vector4 value);

        public void UpdateVector(Vector4 value)
        {
            // var floatValue = Mathf.LerpUnclamped(remapRange.x, remapRange.y, normalizedValue);
            OnUpdateVector(value);
        }

        protected Vector4 cachedValue = Vector4.negativeInfinity;
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public virtual void ApplyVectorValue(Vector4 value)
        {
            _EnsureInit();
            cachedValue = value;
        }
#endif
    }
}