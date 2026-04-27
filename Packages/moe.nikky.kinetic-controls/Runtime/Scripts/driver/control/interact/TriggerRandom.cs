using nikkyai.common;
using nikkyai.control.interact;
using UnityEngine;

namespace nikkyai.driver.control.interact
{
    public class TriggerRandom : TriggerDriver
    {
        protected override string LogPrefix => nameof(TriggerRandom);
    
        [SerializeField] private Selector[] targetToggles;
        [SerializeField] private int minIndex = 0;
        [SerializeField] private int maxIndex = 16;

        public override void OnTrigger()
        {
            Log("Trigger Random");
            foreach (var exclusiveToggle in targetToggles)
            {
                // assign random index to exclusiveToggle
                int newIndex = exclusiveToggle.SyncedIndex;
                while (newIndex == exclusiveToggle.SyncedIndex)
                {
                    newIndex = Random.Range(minIndex, maxIndex);
                }
                // if (!Networking.IsOwner(exclusiveToggle.gameObject))
                // {
                //     Networking.SetOwner(Networking.LocalPlayer, exclusiveToggle.gameObject);
                // }
                exclusiveToggle.SyncedIndex = newIndex;
                exclusiveToggle.UpdateSyncedIndex();
            }
        }
    }
}
