using nikkyai.common;
using VRC.SDKBase;

namespace nikkyai.control
{
    public abstract class BaseSyncedControl: ACLBase
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