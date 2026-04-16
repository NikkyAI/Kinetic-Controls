using nikkyai.common;
using nikkyai.control.headless;
using UnityEngine;

namespace nikkyai.driver.control.headless
{
    public class TriggerResetFloatCycling : TriggerDriver
    {
        [SerializeField] private CyclingFloat cyclingFloat;
        void Start()
        {
            _EnsureInit();
        }

        protected override string LogPrefix => nameof(TriggerResetFloatCycling);
        public override void OnTrigger()
        {
            cyclingFloat.Reset();
        }
    }
}
