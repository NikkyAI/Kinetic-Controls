using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace nikkyai.common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class IntDriver: LoggerBase
    {
        public abstract void OnUpdateInt(int value);
        
        // defaults for Modern UI selector
        // ReSharper disable once InconsistentNaming
        [HideInInspector, UsedImplicitly] public int selectionId;
        [UsedImplicitly]
        public void _SelectionChanged()
        {
            OnUpdateInt(selectionId);
        }

        protected int cachedValue = int.MinValue;
#if UNITY_EDITOR && !COMPILER_UDONSHARP

        // protected override int ValidationHash => HashCode.Combine(base.GetHashCode(), cachedValue);

        public virtual void ApplyIntValue(int value)
        {
            _EnsureInit();
            cachedValue = value;
            // OnUpdateInt(value);
            // OnValidateApplyValues();
        }
#endif
    }
}