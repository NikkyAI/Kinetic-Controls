using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace nikkyai.common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class BoolDriver: LoggingSimple
    {
        public abstract void OnUpdateBool(bool value);

        // [SerializeField] private Vector2Int selectedIdMatch = Vector2Int.up;
        // defaults for Modern UI selector
        // ReSharper disable once InconsistentNaming
        [HideInInspector, UsedImplicitly] public int selectionId;
        [UsedImplicitly]
        public void _SelectionChanged()
        {
            if (selectionId == 0)
            {
                OnUpdateBool(false);
            } else if(selectionId == 1)
            {
                OnUpdateBool(true);
            }
        }
#if UNITY_EDITOR && !COMPILER_UDONSHARP

        public virtual void ApplyBoolValue(bool value)
        {
            _EnsureInit();
        }
#endif
    }
}