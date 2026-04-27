using nikkyai.common;
using UnityEngine;
using VRC.SDKBase;

namespace nikkyai.driver.converter
{
    public class IntToBoolArray : IntDriver
    {
        [SerializeField] private GameObject[] boolDrivers = { };

        private BoolDriver[][] _boolDrivers = { };
    
        protected override string LogPrefix => nameof(IntToBoolArray);
    
        void Start()
        {
            _EnsureInit();    
        }

        protected override void _Init()
        {
            base._Init();

            _boolDrivers = new BoolDriver[boolDrivers.Length][];
            for (var index = 0; index < boolDrivers.Length; index++)
            {
                var boolDriver = boolDrivers[index];
                if (Utilities.IsValid(boolDriver))
                {
                    _boolDrivers[index] = boolDriver.GetComponentsInChildren<BoolDriver>();
                }
            }
        }

        private int oldValue = -1;

        public override void OnUpdateInt(int value)
        {
        
            var newDrivers = _boolDrivers[value];
            if (newDrivers != null)
            {
                for (var i = 0; i < newDrivers.Length; i++)
                {
                    newDrivers[i].OnUpdateBool(true);
                }
            }

            var oldDrivers = _boolDrivers[oldValue];
            if (oldDrivers != null)
            {
                for (var i = 0; i < oldDrivers.Length; i++)
                {
                    oldDrivers[i].OnUpdateBool(false);
                }
            }

            oldValue = value;
        }
    }
}
