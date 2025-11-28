using nikkyai.driver;
using UnityEngine;
using UnityEngine.Serialization;
using VRC;

namespace nikkyai.Kinetic_Controls.driver
{
    public class BoolSyncedDriver : BoolDriver
    {
        [Header("External Behaviours")] // header
        [SerializeField]
        private BaseSyncedBehaviour[] syncedBehaviours;

        protected override string LogPrefix { get; }

        public override void UpdateBool(bool value)
        {

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
