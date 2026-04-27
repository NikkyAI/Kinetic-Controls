using nikkyai.common;
using UnityEngine;

namespace nikkyai.driver.animator
{
    public class AnimatorIntDriver : IntDriver
    {
        [SerializeField] private Animator animator;
        [SerializeField] private string intParameterName;
        protected override string LogPrefix => nameof(AnimatorIntDriver);

        public override void OnUpdateInt(int value)
        {
            if (!enabled) return;
            animator.SetInteger(intParameterName, value);
        }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public override void ApplyIntValue(int value)
        {
            base.ApplyIntValue(value);
            OnUpdateInt(value);
        }
#endif
    }
}
