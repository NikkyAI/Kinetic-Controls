using nikkyai.driver;
using nikkyai.Kinetic_Controls;
using UdonSharp;
using UnityEngine;
using VRC;
using VRC.SDKBase;
using VRC.Udon;

namespace nikkyai.Kinetic_Controls.driver
{
    public class IntSmoothingFramesDriver : IntDriver
    {
        [Header("External Behaviours")] // header
        [SerializeField]
        private BaseSmoothedBehaviour[] smoothedBehaviours;

        protected override string LogPrefix => nameof(IntSmoothingFramesDriver);

        void Start()
        {
            _EnsureInit();
        }

        public override void UpdateInt(int value)
        {
            if (value <= 0)
            {
                LogError("value must be greater than 0");
                return;
            }

            foreach (var behaviour in smoothedBehaviours)
            {
                behaviour.SmoothingFrames = value;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
                behaviour.MarkDirty();
#endif
            }
        }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public override void ApplyIntValue(int value)
        {
            UpdateInt(value);
        }
#endif
    }
}