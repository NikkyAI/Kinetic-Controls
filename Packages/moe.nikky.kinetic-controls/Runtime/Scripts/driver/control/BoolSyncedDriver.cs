using nikkyai.common;
using nikkyai.control;
using UnityEngine;
using VRC;
using VRC.SDKBase;

namespace nikkyai.driver.control
{
    public class BoolSyncedDriver : BoolDriver
    {
        [Header("External Behaviours")] // header
        [SerializeField]
        private BaseBehaviour[] syncedBehaviours;

        protected override string LogPrefix => nameof(BoolSyncedDriver);

        public override void OnUpdateBool(bool value)
        {
            if (!enabled) return;

            foreach (var behaviour in syncedBehaviours)
            {
                if (Utilities.IsValid(behaviour))
                {
                    behaviour.Synced = value;
#if UNITY_EDITOR && !COMPILER_UDONSHARP
                    behaviour.MarkDirty();
#endif
                }
            }
        }


#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public override void ApplyBoolValue(bool value)
        {
            base.ApplyBoolValue(value);
            OnUpdateBool(value);
        }
#endif
    }
}

