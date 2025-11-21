using nikkyai.driver;
using UnityEngine;
using UnityEngine.Serialization;
using VRC;

namespace nikkyai.Kinetic_Controls.driver
{
    public class FloatSmoothingRateDriver : FloatDriver
    {
        [Header("External Behaviours")] // header
        [FormerlySerializedAs("faders")]
        [SerializeField]
        private PickupFader[] pickupFaders;
        [SerializeField]
        private TouchFader[] touchFaders;
        [SerializeField]
        private PickupLever[] levers;

        protected override string LogPrefix => nameof(FloatSmoothingRateDriver);

        void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            base._Init();
            
            //TODO: check if all fields are valid
            // or find the TMP component
        }

        public override void UpdateFloat(float value)
        {
            foreach (var fader in pickupFaders)
            {
                fader.SmoothingRate = value;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
                fader.MarkDirty();
#endif
            }
            foreach (var fader in touchFaders)
            {
                fader.SmoothingRate = value;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
                fader.MarkDirty();
#endif
            }

            foreach (var lever in levers)
            {
                lever.SmoothingRate = value;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
                lever.MarkDirty();
#endif
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