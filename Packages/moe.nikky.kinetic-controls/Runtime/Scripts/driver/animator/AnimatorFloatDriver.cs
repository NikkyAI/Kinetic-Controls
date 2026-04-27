using nikkyai.common;
using UnityEngine;

namespace nikkyai.driver.animator
{
    public class AnimatorFloatDriver : FloatDriver
    {
        [SerializeField] private Animator animator;
        [SerializeField] string floatParameterName;

        protected override string LogPrefix => nameof(AnimatorFloatDriver);
        protected override void OnUpdateFloat(float value)
        {
            if (!enabled) return;
            animator.SetFloat(floatParameterName, value);
        }
        
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public override void ApplyFloatValue(float value)
        {
            base.ApplyFloatValue(value);
            UpdateFloatRescale(value);
        }
#endif
    }
}
