using nikkyai.common;
using UdonSharp;

namespace nikkyai.driver
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class FloatDriver: LoggerBase
    {
        public abstract void UpdateFloat(float value);
        
#if UNITY_EDITOR && !COMPILER_UDONSHARP

        public virtual void ApplyFloatValue(float value)
        {
        }
#endif
    }
}