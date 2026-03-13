using nikkyai.common;
using nikkyai.Kinetic_Controls;
using UnityEngine;
using VRC.SDKBase;

namespace nikkyai.driver.fader
{
    public class TriggerResetFader : TriggerDriver
    {
        [SerializeField] private BaseSmoothedBehaviour[] _smoothedBehaviours = { };

        void Start()
        {
            _EnsureInit();
        }

        protected override string LogPrefix => nameof(TriggerResetFader);

        public override void Trigger()
        {
            if (!enabled) return;
            for (var i = 0; i < _smoothedBehaviours.Length; i++)
            {
                var behaviour = _smoothedBehaviours[i];
                if (Utilities.IsValid(behaviour))
                { 
                    _smoothedBehaviours[i].Reset();
                }
            }
        }
    }
}
