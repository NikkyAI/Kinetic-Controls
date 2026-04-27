using UdonSharp;

namespace nikkyai.common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class TriggerDriver: LoggingSimple
    {
        public abstract void OnTrigger();
    }
}