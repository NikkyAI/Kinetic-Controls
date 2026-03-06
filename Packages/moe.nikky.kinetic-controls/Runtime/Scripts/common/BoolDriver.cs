using nikkyai.common;
using UdonSharp;

namespace nikkyai.driver
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class BoolDriver: LoggerBase
    {
        public abstract void UpdateBool(bool value);
        
#if UNITY_EDITOR && !COMPILER_UDONSHARP

        public virtual void ApplyBoolValue(bool value)
        {
        }
#endif
    }
}