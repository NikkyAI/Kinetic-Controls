
using nikkyai.common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class IntUdonDriver : IntDriver
{
    [Header("External Behaviours")] // header
    [SerializeField]
    private UdonBehaviour[] externalBehaviours;

    [SerializeField]
    private string intField;
    [SerializeField]
    private string eventName;

    protected override string LogPrefix => nameof(IntUdonDriver);

    void Start()
    {
        _EnsureInit();
    }

    public override void OnUpdateInt(int value)
    {
        for (var i = 0; i < externalBehaviours.Length; i++)
        {
            var ext = externalBehaviours[i];
            if (Utilities.IsValid(ext))
            {
                ext.SetProgramVariable(intField, value);
            }
        }

        if (eventName.Length > 0)
        {
            for (var i = 0; i < externalBehaviours.Length; i++)
            {
                var ext = externalBehaviours[i];
                if (Utilities.IsValid(ext))
                {
                    ext.SendCustomEvent(eventName);
                }
            }
        }
    }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    public override void ApplyIntValue(int value)
    {
        base.ApplyIntValue(value);
        OnUpdateInt(value);
    }
#endif
}
