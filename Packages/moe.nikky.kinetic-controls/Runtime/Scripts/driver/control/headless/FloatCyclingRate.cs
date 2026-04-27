using nikkyai.common;
using nikkyai.control.headless;
using UnityEngine;

namespace nikkyai.driver.control.headless
{
    public class FloatCyclingRate : FloatDriver
    {
        [SerializeField] private CyclingFloat cyclingFloat;
        protected override string LogPrefix => nameof(FloatCyclingRate);
        void Start()
        {
            _EnsureInit();
        }


        protected override void OnUpdateFloat(float value)
        {
            if (!enabled) return;
        
            cyclingFloat.speed = value;
        }
    }
}
