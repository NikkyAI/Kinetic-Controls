using nikkyai.button;
using nikkyai.common;
using UnityEngine;
using VRC.SDKBase;

namespace nikkyai.driver
{
    public class TriggerRandom : TriggerDriver
    {
        protected override string LogPrefix => nameof(TriggerRandom);

    
        [SerializeField] private Selector[] targetToggles;
        [SerializeField] private int minIndex = 0;
        [SerializeField] private int maxIndex = 16;

        public override void Trigger()
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
            }
        }
    }
}
