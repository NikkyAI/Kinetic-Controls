using nikkyai.common;
using nikkyai.Kinetic_Controls;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace nikkyai.driver.fader
{
    public class TriggerResetFader : TriggerDriver
    {
        [FormerlySerializedAs("_smoothedBehaviours")] //
        [SerializeField] private BaseSmoothedBehaviour[] smoothedBehaviours = { };

        void Start()
        {
            _EnsureInit();
        }

        protected override string LogPrefix => nameof(TriggerResetFader);

        public override void Trigger()
        {
            if (!enabled) return;
            for (var i = 0; i < smoothedBehaviours.Length; i++)
            {
                var behaviour = smoothedBehaviours[i];
                if (Utilities.IsValid(behaviour))
                { 
                    smoothedBehaviours[i].Reset();
                }
            }
        }
    }
}
