using nikkyai.common;
using nikkyai.control.kinetic;
using UnityEngine;

namespace nikkyai.driver.control.kinetic
{
    public class BoolUseContactsInVR : BoolDriver
    {
        [Header("Faders and Levers")] // header
        [SerializeField]
        private BaseKineticControl[] controls;
        void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            base._Init();
            
        }

        protected override string LogPrefix => nameof(BoolUseContactsInVR);
    
        public override void OnUpdateBool(bool value)
        {
            for (var i = 0; i < controls.Length; i++)
            {
                var control = controls[i];
                control.UseContactsInVRLocal = value;
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
