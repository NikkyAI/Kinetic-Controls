using nikkyai.common;
using UnityEngine;
using VRC.SDKBase;

namespace nikkyai.driver.converter
{
    public class BoolToVector : BoolDriver
    {
        [SerializeField] private Vector4 valueOff = Vector4.zero;
        [SerializeField] private Vector4 valueOn = Vector4.one;

        [SerializeField] private GameObject vectorDrivers;
    
        private VectorDriver[] _vectorDrivers = {};
    
        protected override string LogPrefix => nameof(BoolToVector);

        void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            if (Utilities.IsValid(vectorDrivers))
            {
                _vectorDrivers = vectorDrivers.GetComponents<VectorDriver>();
            }
        }

        public override void OnUpdateBool(bool value)
        {
            var vectorValue = value ? valueOn : valueOff;
            for (var i = 0; i < _vectorDrivers.Length; i++)
            {
                _vectorDrivers[i].UpdateVector(vectorValue);
            }
        }
    }
}
