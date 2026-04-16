using nikkyai.common;
using UnityEngine;
using VRC;
using VRC.SDKBase;
using VRC.Udon;

namespace nikkyai.driver.udon
{
    public class ColorUdonDriver : ColorDriver
    {
        [Header("External Behaviours")] // header
        [SerializeField]
        private UdonBehaviour[] externalBehaviours;

        [SerializeField]
        private string colorField;
        [SerializeField]
        private string eventName;
    
        protected override string LogPrefix => nameof(ColorUdonDriver);
        void Start()
        {
            _EnsureInit();
        }

        public override void OnUpdateColor(Color value)
        {
            for (var i = 0; i < externalBehaviours.Length; i++)
            {
                var ext = externalBehaviours[i];
                if (Utilities.IsValid(ext))
                {
                    Log($"Setting program variable {colorField} to {value} on {ext}");
                    ext.SetProgramVariable(colorField, value);
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
        public override void ApplyColorValue(Color value)
        {
            base.ApplyColorValue(value);
            OnUpdateColor(value);
            foreach (var externalBehaviour in externalBehaviours)
            {
                externalBehaviour.MarkDirty();
            }
        }
#endif
    }
}
