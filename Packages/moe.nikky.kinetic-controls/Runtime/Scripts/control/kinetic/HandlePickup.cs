using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace nikkyai.control.kinetic
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [RequireComponent(typeof(VRC_Pickup))]
    public class HandlePickup : HandleAbstract
    {
        [SerializeField]
#if HIDE_INSPECTOR
        [HideInInspector]
#endif
        internal VRC_Pickup pickup;

        [SerializeField]
#if HIDE_INSPECTOR
        [HideInInspector]
#endif
        [ReadOnly]
        private bool pickupHasObjectSync = false;
        
        protected override string LogPrefix => nameof(HandleContact);
    
        void Start()
        {
            _EnsureInit();
        }
        
        protected override void AccessChanged()
        {
            base.AccessChanged();

            if (!IsInVR)
            {
                // desktop mode is handle with Interact()
                pickup.pickupable = false;
                return;
            }
            
            pickup.pickupable = IsAuthorized;
        }

        public override void ResetTransformIfNotManipulated()
        {
            if (!pickupHasObjectSync && !IsHeldLocally)
            {
                ResetTransform();
            }
        }
        
        public override void OnPickup()
        {
            Log("OnPickup");
            if (!IsAuthorized)
            {
                pickup.Drop();
                //resetting position

                ResetTransform();
                // for (var i = 0; i < _controlBehaviour.Length; i++)
                // {
                //     var cb = _controlBehaviour[i];
                //     if (Utilities.IsValid(cb))
                //     {
                //         cb.UpdateHandlePosition();
                //     }
                //     else
                //     {
                //         LogError($"OnPickup: controller {i} invalid");
                //         LogError($"invalid: {cb}");
                //     }
                // }

                return;
            }

            if (!IsAuthorized)
            {
                pickup.Drop();
                return;
            }

            Log("_OnPickup");
            if (IsHeldLocally)
            {
                Log("already being adjusted");
                return;
            }

            // if (IsInVR && useContactsInVRLocal)
            // {
            //     LogWarning("dropping pickup");
            //     pickup.Drop();
            //     return;
            // }
            IsHeldLocally = true;
            foreach (var baseKineticControl in controlBehaviours)
            {
                baseKineticControl.OnMoveHandle(transform.position);
            }
            // TakeOwnership();

            // SyncedIsBeingManipulated = true;
            // this.SendCustomEventDelayedFrames(nameof(FollowPickup), 1);
            _OnFollowPickup();
        }

        public override void OnDrop()
        {
            Log("OnDrop");
            if (!IsAuthorized)
                return;

            if (!IsAuthorized)
            {
                return;
            }

            Log("_OnDrop");

            foreach (var baseKineticControl in controlBehaviours)
            {
                baseKineticControl.TakeOwnership();
            }

            IsHeldLocally = false;
            foreach (var baseKineticControl in controlBehaviours)
            {
                baseKineticControl.OnDropHandle();
                // baseKineticControl.UpdateHandlePosition();
                if (!IsInVR)
                {
                    baseKineticControl.DebugDesktopRaytrace(false);
                }
            }

            ResetTransform();

            // Log("handle released, resetting position");
            // SendCustomNetworkEvent(NetworkEventTarget.All, nameof(UpdatePickupPosition));
        }
        
        
        public void _OnFollowPickup()
        {
            if (!IsHeldLocally) return;
            foreach (var baseKineticControl in controlBehaviours)
            {
                baseKineticControl.FollowPickup();
                // if (baseKineticControl.Synced)
                // {
                //     baseKineticControl.TakeOwnership();
                //     baseKineticControl.RequestSerialization();
                // }
                //
                // baseKineticControl.OnDeserialization();
            }

            // FollowPickup();
            if (IsHeldLocally)
            {
                this.SendCustomEventDelayedFrames(nameof(_OnFollowPickup), 0);
            }
        }

        
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        internal override void Setup()
        {
            base.Setup();
            SetupPickup();
        }

        private void SetupPickup()
        {
            Log("SetupPickup");
            pickup = GetComponent<VRC_Pickup>();
            Log($"pickup is {pickup}");
            if (Utilities.IsValid(pickup))
            {
                pickupHasObjectSync = pickup.GetComponent<VRCObjectSync>() != null ||
                                       pickup.GetComponent("MMMaellon.SmartObjectSync") != null;
            }
            else
            {
                LogError($"no pickup found");
            }
        }
#endif
    }
}
