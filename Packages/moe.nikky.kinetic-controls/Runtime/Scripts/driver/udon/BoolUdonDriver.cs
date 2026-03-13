using nikkyai.common;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace nikkyai.driver.udon
{
    public class BoolUdonDriver : BoolDriver
    {
        [Header("External Behaviours")] // header
        [SerializeField]
        private UdonBehaviour[] externalBehaviours;

        [SerializeField]
        private string boolField;
        [SerializeField]
        private string eventName;

        protected override string LogPrefix => nameof(BoolUdonDriver);

        void Start()
        {
            _EnsureInit();
        }
    
    
        public override void UpdateBool(bool value)
        {
            for (var i = 0; i < externalBehaviours.Length; i++)
            {
                var ext = externalBehaviours[i];
                if (Utilities.IsValid(ext))
                {
                    ext.SetProgramVariable(boolField, value);
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
        public override void ApplyBoolValue(bool value)
        {
            UpdateBool(value);
            
        }
#endif
    }
}
