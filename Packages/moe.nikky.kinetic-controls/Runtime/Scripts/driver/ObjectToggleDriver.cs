using UnityEngine;
using VRC;

namespace nikkyai.driver
{
    public class ObjectToggleDriver : BoolDriver
    {
        [SerializeField] private GameObject[] targetsOn = { };
        [SerializeField] private GameObject[] targetsOff = { };
        protected override string LogPrefix => $"ObjectToggleDriver {name}";

        public override void UpdateBool(bool value)
        {
            if (!enabled) return;
            foreach (var obj in targetsOn)
            {
                if (obj)
                {
                    obj.SetActive(value);
                }
            }

            foreach (var obj in targetsOff)
            {
                if (obj)
                {
                    obj.SetActive(!value);
                }
            }
        }
        
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public override void ApplyBoolValue(bool value)
        {
            UpdateBool(value);
            this.MarkDirty();
        }
#endif
    }
}
