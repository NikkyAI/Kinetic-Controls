using nikkyai.common;
using UdonSharp;

namespace nikkyai.driver
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class TriggerDriver: LoggerBase
    {
        public abstract void Trigger();

        public virtual void Release()
        {
            
        }
    }
}