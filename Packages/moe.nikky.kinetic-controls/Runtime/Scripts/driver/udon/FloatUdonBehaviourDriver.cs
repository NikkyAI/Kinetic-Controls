using UnityEngine;
using VRC.Udon;

namespace nikkyai.driver.udon
{
    public class FloatUdonBehaviourDriver : FloatDriver
    {
        [Header("External Behaviours")] // header
        [SerializeField]
        private UdonBehaviour[] externalBehaviours;

        [SerializeField]
        private string floatField;
        [SerializeField]
        private string eventName;

        protected override string LogPrefix => nameof(FloatUdonBehaviourDriver);

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
            for (var i = 0; i < externalBehaviours.Length; i++)
            {
                externalBehaviours[i].SetProgramVariable(floatField, value);
            }

            if (eventName.Length > 0)
            {
                for (var i = 0; i < externalBehaviours.Length; i++)
                {
                    externalBehaviours[i].SendCustomEvent(eventName);
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