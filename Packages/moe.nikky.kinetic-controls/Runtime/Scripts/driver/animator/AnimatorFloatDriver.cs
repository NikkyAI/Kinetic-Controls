using nikkyai.common;
using UnityEditor;
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
        protected override bool UpdateInEditor => true;
#endif
    }
}
