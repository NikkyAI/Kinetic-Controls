using nikkyai.common;
using nikkyai.control.headless;
using UnityEngine;

namespace nikkyai.driver.control.headless
{
    public class BoolLoopRunning : BoolDriver
    {
        [SerializeField] private LoopTrigger loopTrigger;
        protected override string LogPrefix => nameof(BoolLoopRunning);
    
        void Start()
        {
            _EnsureInit();
        }

        public override void OnUpdateBool(bool value)
        {
            if (!enabled) return;
        
            // Log($"time running: {value}");
            loopTrigger.TimerRunning = value;
        }
    }
}
