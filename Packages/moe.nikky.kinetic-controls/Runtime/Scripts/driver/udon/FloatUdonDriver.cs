using nikkyai.common;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace nikkyai.driver.udon
{
    public class FloatUdonDriver : FloatDriver
    {
        [Header("External Behaviours")] // header
        [SerializeField]
        private UdonBehaviour[] externalBehaviours;

        [SerializeField]
        private string floatField;
        [SerializeField]
        private string eventName;

        protected override string LogPrefix => nameof(FloatUdonDriver);

        void Start()
        {
            _EnsureInit();
        }

        protected override void OnUpdateFloat(float value)
        {
            for (var i = 0; i < externalBehaviours.Length; i++)
            {
                var ext = externalBehaviours[i];
                if (Utilities.IsValid(ext))
                {
                    ext.SetProgramVariable(floatField, value);
                }
            }

            if (eventName.Length > 0)
            {
                for (var i = 0; i < externalBehaviours.Length; i++)
                {
                    var ext = externalBehaviours[i];
                    if (Utilities.IsValid(ext))
                    {
                        ext.SendCustomEvent(eventName);
                    }
                }
            }
        }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public override void ApplyFloatValue(float value)
        {
            base.ApplyFloatValue(value);
            UpdateFloatRescale(value);
        }
#endif
    }
}