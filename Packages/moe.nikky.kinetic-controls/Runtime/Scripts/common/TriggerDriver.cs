using UdonSharp;

namespace nikkyai.common
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