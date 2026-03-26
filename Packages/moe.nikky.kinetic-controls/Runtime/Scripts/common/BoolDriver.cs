using System;
using JetBrains.Annotations;
using UdonSharp;

namespace nikkyai.common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class BoolDriver: LoggerBase
    {
        public abstract void UpdateBool(bool value);
        
        [NonSerialized, UsedImplicitly] public int selectedId;
        [UsedImplicitly]
        public void _SelectionChanged()
        {
            UpdateBool(selectedId != 0);
        }
#if UNITY_EDITOR && !COMPILER_UDONSHARP

        public virtual void ApplyBoolValue(bool value)
        {
        }
#endif
    }
}