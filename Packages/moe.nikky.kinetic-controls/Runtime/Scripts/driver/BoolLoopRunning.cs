using nikkyai.common;
using UnityEngine;

namespace nikkyai.driver
{
    public class BoolLoopRunning : BoolDriver
    {
        [SerializeField] private LoopTrigger loopTrigger;
        protected override string LogPrefix => nameof(BoolLoopRunning);
    
        void Start()
        {
            _EnsureInit();
        }

        public override void UpdateBool(bool value)
        {
            if (!enabled) return;
        
            loopTrigger.enabled = value;
        }
    }
}
