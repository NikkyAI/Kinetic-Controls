using System;
using JetBrains.Annotations;
using nikkyai.common;
using UdonSharp;

namespace nikkyai.driver
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class IntDriver: LoggerBase
    {
        public abstract void UpdateInt(int value);
        
        // modern ui defaults for modern UI selector
        [NonSerialized, UsedImplicitly] public int selectedId;
        public void _SelectionChanged()
        {
            UpdateInt(selectedId);
        }
#if UNITY_EDITOR && !COMPILER_UDONSHARP

        public virtual void ApplyIntValue(int value)
        {
        }
#endif
    }
}