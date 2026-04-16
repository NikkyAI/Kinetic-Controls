using nikkyai.common;
using nikkyai.control;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace nikkyai.driver.control.kinetic
{
    public class TriggerResetFader : TriggerDriver
    {
        [FormerlySerializedAs("_smoothedBehaviours")] //
        [SerializeField] private BaseSmoothedControl[] smoothedBehaviours = { };

        void Start()
        {
            _EnsureInit();
        }

        protected override string LogPrefix => nameof(TriggerResetFader);

        public override void OnTrigger()
        {
            if (!enabled) return;
            Log("triggered reset");
            for (var i = 0; i < smoothedBehaviours.Length; i++)
            {
                var behaviour = smoothedBehaviours[i];
                if (Utilities.IsValid(behaviour))
                { 
                    Log($"resetting {behaviour.name}");
                    behaviour.Reset();
                }
                else
                {
                    LogError($"behaviour {i} was not valid");
                }
            }
        }
    }
}
