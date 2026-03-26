using nikkyai.button;
using nikkyai.common;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace nikkyai.driver
{
    public class TriggerResetToggleButton : TriggerDriver
    {
        [SerializeField] private ToggleButton[] toggleButtons = { };
        
        void Start()
        {
            _EnsureInit();
        }

        protected override string LogPrefix => nameof(TriggerResetToggleButton);
        public override void Trigger()
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
