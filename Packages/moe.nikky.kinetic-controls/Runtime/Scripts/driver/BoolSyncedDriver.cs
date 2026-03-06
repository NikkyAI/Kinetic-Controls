using nikkyai.common;
using UnityEngine;
using VRC;

namespace nikkyai.driver
{
    public class BoolSyncedDriver : BoolDriver
    {
        [Header("External Behaviours")] // header
        [SerializeField]
        private BaseSyncedBehaviour[] syncedBehaviours;

        protected override string LogPrefix { get; }

        public override void UpdateBool(bool value)
        {
            if (!enabled) return;

            foreach (var behaviour in syncedBehaviours)
            {
                behaviour.Synced = value;
#if UNITY_EDITOR && !COMPILER_UDONSHARP
                behaviour.MarkDirty();
#endif
            }
        }


#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public override void ApplyBoolValue(bool value)
        {
            UpdateBool(value);
        }
#endif
    }
}

