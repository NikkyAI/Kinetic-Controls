using nikkyai.common;
using nikkyai.control.kinetic;
using UnityEngine;

namespace nikkyai.driver.control.kinetic
{
    public class BoolUseContactsInVR_Deprecated : BoolDriver
    {
        [Header("Faders and Levers - Deprecated, use Handle instead")] // header
        [SerializeField]
        private BaseKineticControl[] controls;
        [Header("Handles")] // header
        [SerializeField]
        private Handle[] handles;
        void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            base._Init();
            
        }

        protected override string LogPrefix => nameof(BoolUseContactsInVR_Deprecated);
    
        public override void OnUpdateBool(bool value)
        {
            // for (var i = 0; i < controls.Length; i++)
            // {
            //     var control = controls[i];
            //     control.handle.UseContactsInVR = value;
            // }
            // for (var i = 0; i < handles.Length; i++)
            // {
            //     var handle = handles[i];
            //     handle.UseContactsInVR = value;
            // }
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
