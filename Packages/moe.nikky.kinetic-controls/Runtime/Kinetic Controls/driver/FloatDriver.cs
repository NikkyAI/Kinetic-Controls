using nikkyai.kineticcontrols.common;
using UdonSharp;

namespace nikkyai.kineticcontrols.driver
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public abstract class FloatDriver: LoggerBase
    {
        public abstract void UpdateFloat(float value);
        
#if UNITY_EDITOR && !COMPILER_UDONSHARP

        public virtual void ApplyValue(float value)
        {
        }
#endif
    }
}