using nikkyai.common;
using nikkyai.control;
using UnityEngine;
using VRC;
using VRC.SDKBase;

namespace nikkyai.driver.control.kinetic
{
    public class IntSmoothingFramesDriver : IntDriver
    {
        [Header("External Behaviours")] // header
        [SerializeField]
        private BaseSmoothedControl[] smoothedBehaviours = {};

        protected override string LogPrefix => nameof(IntSmoothingFramesDriver);

        void Start()
        {
            _EnsureInit();
        }

        public override void OnUpdateInt(int value)
        {
            if (!enabled) return;
            if (value <= 0)
            {
                LogError("value must be greater than 0");
                return;
            }

            foreach (var behaviour in smoothedBehaviours)
            {
                if (Utilities.IsValid(behaviour))
                {
                    behaviour.SmoothingFrames = value;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
                    behaviour.MarkDirty();
#endif
                }
            }
        }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public override void ApplyIntValue(int value)
        {
            OnUpdateInt(value);
        }
#endif
    }
}