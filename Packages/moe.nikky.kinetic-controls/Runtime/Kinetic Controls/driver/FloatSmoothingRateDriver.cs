using UnityEngine;
using VRC;

namespace nikkyai.kineticcontrols.driver
{
    public class FloatSmoothingRateDriver : FloatDriver
    {
        [Header("External Behaviours")] // header
        [SerializeField]
        private PickupFader[] faders;
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
            foreach (var fader in faders)
            {
                fader.SmoothingRate = value;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
                fader.MarkDirty();
                fader.MarkDirty();
#endif
            }

            foreach (var lever in levers)
            {
                lever.SmoothingRate = value;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
                lever.MarkDirty();
                lever.MarkDirty();
#endif
            }
        }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public override void ApplyValue(float value)
        {
            UpdateFloat(value);
            
        }
#endif
    }
}