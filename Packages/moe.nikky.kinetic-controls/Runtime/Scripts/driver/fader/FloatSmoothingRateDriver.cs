using nikkyai.common;
using nikkyai.Kinetic_Controls;
using UnityEngine;
using UnityEngine.Serialization;
using VRC;
using VRC.SDKBase;

namespace nikkyai.driver.fader
{
    public class FloatSmoothingRateDriver : FloatDriver
    {
        [Header("External Behaviours")] // header
        [FormerlySerializedAs("faders")]
        [SerializeField]
        private BaseSmoothedBehaviour[] smoothedBehaviours;

        protected override string LogPrefix => nameof(FloatSmoothingRateDriver);

        void Start()
        {
            _EnsureInit();
        }

        protected override void UpdateFloat(float value)
        {
            if (!enabled) return;
            if (value <= 0f)
            {
                LogError("value must be greater than 0");
                return;
            }

            foreach (var behaviour in smoothedBehaviours)
            {
                if (Utilities.IsValid(behaviour))
                {
                    behaviour.SmoothingRate = value;
#if UNITY_EDITOR && !COMPILER_UDONSHARP
                    behaviour.MarkDirty();
#endif
                }

            }
        }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public override void ApplyFloatValue(float value)
        {
            UpdateFloat(value);
        }
#endif
    }
}