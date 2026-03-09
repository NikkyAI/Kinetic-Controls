using nikkyai.common;
using UnityEngine;

namespace nikkyai.driver
{
    public class FloatCyclingRate : FloatDriver
    {
        [SerializeField] private CyclingFloat cyclingFloat;
        protected override string LogPrefix => nameof(FloatCyclingRate);
        void Start()
        {
            _EnsureInit();
        }


        public override void UpdateFloat(float value)
        {
            if (!enabled) return;
        
            cyclingFloat.Rate = value;
        }
    }
}
