using nikkyai.common;
using UnityEngine;

namespace nikkyai.driver.converter
{
    public class BoolToInt : BoolDriver
    {
        [SerializeField] 
        private int intOff = 0;
        [SerializeField]
        private int intOn = 1;
        [SerializeField] private GameObject intDrivers;
        private IntDriver[] _intDrivers = {};
        void Start()
        {
            _EnsureInit();
        }

        protected override string LogPrefix => nameof(BoolToInt);
        public override void OnUpdateBool(bool value)
        {
            if (!enabled) return;
            int intValue = value ? intOn : intOff;
            Log($"updating int: {value} -> {intValue} on {_intDrivers.Length} drivers");
            for (var i = 0; i < _intDrivers.Length; i++)
            {
                _intDrivers[i].UpdateIntRemap(intValue);
            }
        }
    }
}
