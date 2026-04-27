using nikkyai.common;
using nikkyai.control.interact;
using UnityEngine;
using VRC.SDKBase;

namespace nikkyai.driver.control.interact
{
    public class TriggerResetSelector : TriggerDriver
    {
        [SerializeField] private Selector[] selectors = {};
        void Start()
        {
            _EnsureInit();
        }

        protected override string LogPrefix => nameof(TriggerResetSelector);
    
        public override void OnTrigger()
        {
            for (var i = 0; i < selectors.Length; i++)
            {
                var selector = selectors[i];
                if (Utilities.IsValid(selector))
                {
                    selector.Reset();
                }
            }
        }
    }
}
