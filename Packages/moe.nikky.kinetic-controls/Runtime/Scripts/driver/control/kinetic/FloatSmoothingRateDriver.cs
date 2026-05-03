using nikkyai.common;
using nikkyai.control;
using UnityEngine;
using UnityEngine.Serialization;
using VRC;
using VRC.SDKBase;

namespace nikkyai.driver.control.kinetic
{
    public class FloatSmoothingRateDriver : FloatDriver
    {
        [Header("Deprecated, use FloatSmoothingTimeDriver and FloatSmoothingMaxSpeedDriver instead")]
        [Header("External Behaviours")] // header
        [FormerlySerializedAs("faders")]
        [SerializeField]
        private BaseSmoothedControl[] smoothedBehaviours;

        protected override string LogPrefix => nameof(FloatSmoothingRateDriver);

        void Start()
        {
            _EnsureInit();
        }

        protected override void OnUpdateFloat(float value)
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
                    // commented to prevent breakage
                    // behaviour.SmoothingRate = value;
#if UNITY_EDITOR && !COMPILER_UDONSHARP
                    behaviour.MarkDirty();
#endif
                }

            }
        }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        protected override bool UpdateInEditor => true;
#endif
    }
}