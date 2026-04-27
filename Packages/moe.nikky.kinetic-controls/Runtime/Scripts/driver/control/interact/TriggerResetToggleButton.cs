using nikkyai.common;
using nikkyai.control.interact;
using UnityEngine;
using VRC.SDKBase;

namespace nikkyai.driver.control.interact
{
    public class TriggerResetToggleButton : TriggerDriver
    {
        [SerializeField] private ToggleButton[] toggleButtons = { };
        
        void Start()
        {
            _EnsureInit();
        }

        protected override string LogPrefix => nameof(TriggerResetToggleButton);
        public override void OnTrigger()
        {
            if (!enabled) return;
            foreach (var toggleButton in toggleButtons)
            {
                if (Utilities.IsValid(toggleButton))
                { 
                    toggleButton.Reset();
                }
            }
        }
    }
}
