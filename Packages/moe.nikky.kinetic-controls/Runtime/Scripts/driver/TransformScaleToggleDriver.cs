using UnityEngine;
using VRC;

namespace nikkyai.driver
{
    public class TransformScaleToggleDriver : BoolDriver
    {
        [SerializeField] private Transform[] targetsOn = { };
        [SerializeField] private Transform[] targetsOff = { };
        protected override string LogPrefix => $"TransformScaleToggleDriver {name}";

        public override void UpdateBool(bool value)
        {
            if (!enabled) return;
            foreach (var obj in targetsOn)
            {
                if (obj)
                {
                    obj.localScale = value ?  Vector3.one : Vector3.zero;
                }
            }

            foreach (var obj in targetsOff)
            {
                if (obj)
                {
                    obj.localScale = !value ?  Vector3.one : Vector3.zero;
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