using UnityEngine;

namespace nikkyai.driver.animator
{
    public class AnimatorFloatDriver : FloatDriver
    {
        [SerializeField] private Animator animator;
        [SerializeField] string floatParameterName;

        protected override string LogPrefix => $"AnimatorFloatDriver {name}";
        public override void UpdateFloat(float value)
        {
            if (!enabled) return;
            animator.SetFloat(floatParameterName, value);
        }
    }
}
