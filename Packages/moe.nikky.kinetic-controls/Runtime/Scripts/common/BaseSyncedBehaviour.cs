using VRC.SDKBase;

namespace nikkyai.common
{
    public abstract class BaseSyncedBehaviour: ACLBase
    {
        public abstract bool Synced { get; set; }
        
        public virtual void TakeOwnership()
        {
            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
        }
    }
}