
using nikkyai.driver;
using nikkyai.Kinetic_Controls;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ResetFaderTrigger : TriggerDriver
{
    [SerializeField] private BaseSmoothedBehaviour[] _smoothedBehaviours = { };

    void Start()
    {
        _EnsureInit();
    }

    protected override string LogPrefix => nameof(ResetFaderTrigger);

    public override void Trigger()
    {
        if (!enabled) return;
        for (var i = 0; i < _smoothedBehaviours.Length; i++)
        {
            _smoothedBehaviours[i].Reset();
        }
    }
}
