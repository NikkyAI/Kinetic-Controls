using nikkyai.common;
using UnityEngine;
using VRC.SDKBase;

namespace nikkyai.driver.converter
{
    public class BoolToFloat : BoolDriver
    {
        [SerializeField] private float valueOff = 0f;
        [SerializeField] private float valueOn = 1f;

        [SerializeField] private GameObject floatDrivers;
    
        private FloatDriver[] _floatDrivers = {};
    
        protected override string LogPrefix => nameof(BoolToFloat);

        void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            if (Utilities.IsValid(floatDrivers))
            {
                _floatDrivers = floatDrivers.GetComponents<FloatDriver>();
            }
        }

        public override void OnUpdateBool(bool value)
        {
            if(!enabled) return;
            float floatValue = value ? valueOn : valueOff;
            for (var i = 0; i < _floatDrivers.Length; i++)
            {
                _floatDrivers[i].UpdateFloatRescale(floatValue);
            }
        }
    }
}
