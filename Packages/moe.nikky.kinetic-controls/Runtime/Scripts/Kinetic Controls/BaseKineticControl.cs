using System;
using nikkyai.common;
using UdonSharp;
using UnityEngine;
using VRC;
using VRC.Dynamics;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon.Common;

// ReSharper disable ForCanBeConvertedToForeach

namespace nikkyai.Kinetic_Controls
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public abstract class BaseKineticControl : BaseSmoothedBehaviour
    {

        [Header("State")] // header
        [SerializeField, UdonSynced]
        protected bool synced = true;

        [Header("Debug")] // header
        [SerializeField]
        protected Transform debugRaytrace;
        
        public override bool Synced
        {
            get => synced;
            set
            {
                if (!isAuthorized) return;

                var prevValue = SyncedValueNormalized;
                TakeOwnership();
                Log($"set synced to {value}");
                synced = value;
                Log($"set state to {SyncedValueNormalized} => {prevValue}");
                SyncedValueNormalized = prevValue;

                RequestSerialization();
            }
        }
        
        protected abstract bool UseContactsInVR { get; }
        [HideInInspector, FieldChangeCallback(nameof(UseContactsInVRLocal))]
        public bool useContactsInVRLocal = true;

        public bool UseContactsInVRLocal
        {
            get => useContactsInVRLocal;
            set
            {
                Log($"UseContactsInVRSynced {useContactsInVRLocal} -> {value}");
                useContactsInVRLocal = value;

                if (IsInVR)
                {
                    if (useContactsInVRLocal)
                    {
                        Pickup.pickupable = false;
                        // _contactReceiver.contentTypes = DynamicsUsageFlags.Avatar;
                    }
                    else
                    {
                        Pickup.pickupable = true;
                        // _contactReceiver.contentTypes = DynamicsUsageFlags.Nothing;
                    }
                }
                else
                {
                    Pickup.pickupable = true;
                    Pickup.proximity = 5f; // add serialized field to configure range
                    // _contactReceiver.contentTypes = DynamicsUsageFlags.Nothing;
                }
            }
        }

        [UdonSynced]
        // IMPORTANT, DO NOT DELETE
        protected float SyncedValueNormalized;

        [UdonSynced] // IMPORTANT, DO NOT DELETE
        protected bool SyncedIsBeingManipulated;

        protected override bool TargetIsBeingManipulated
        {
            get => SyncedIsBeingManipulated;
            set => SyncedIsBeingManipulated = value;
        }

        protected float LastSyncedValueNormalized = 0;

        protected abstract Handle Handle { get;  }
        protected abstract Transform HandleReset { get; }
        
        protected VRC_Pickup Pickup;
        protected Rigidbody RigidBody;
        protected bool PickupHasObjectSync;
        protected VRCPlayerApi LocalPlayer;
        protected bool IsHeldLocally;
        protected bool IsInVR;

        private bool _inLeftTrigger;
        private bool _inRightTrigger;

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetupFaderHandle()
        {
            if (Utilities.IsValid(Handle))
            {
                Handle.controlBehaviour = this;

                // _contactReceiver = faderHandle.GetComponent<ContactReceiver>();

#if UNITY_EDITOR && !COMPILER_UDONSHARP
                Handle.EditorACL = AccessControl;
                Handle.EditorEnforceACL = EnforceACL;
                Handle.EditorDebugLog = DebugLog;
                Handle.MarkDirty();
#endif
            }
            else
            {
                LogError($"missing handle in {name}");
            }
        }


        private void SetupPickup()
        {
            if (Utilities.IsValid(Handle))
            {
                if (!Utilities.IsValid(Pickup))
                {
                    Pickup = Handle.GetComponent<VRC_Pickup>();
                }
            }

            if (!Utilities.IsValid(Pickup))
            {
                Pickup = gameObject.GetComponent<VRC_Pickup>();
                // _pickup.pickupable = !_isDesktop;
            }

            if (Utilities.IsValid(Pickup))
            {
                if (!IsInVR)
                {
                    //TODO: define pickup ranges for desktop and VR
#if COMPILER_UDONSHARP
                    Pickup.proximity = 5f;
#endif
                }
            }
        }

        private void SetupPickupRigidBody()
        {
            if (Utilities.IsValid(Pickup))
            {
                RigidBody = Pickup.GetComponent<Rigidbody>();
                RigidBody.useGravity = false;
                RigidBody.isKinematic = false;
                PickupHasObjectSync = Pickup.GetComponent<VRCObjectSync>() != null ||
                                       Pickup.GetComponent("MMMaellon.SmartObjectSync") != null;
            }
            else
            {
                LogError($"no pickup found in {name}");
            }
        }

        protected override void _Init()
        {
            base._Init();
            SetupFaderHandle();
            SetupPickup();
            SetupPickupRigidBody();

            
            LocalPlayer = Networking.LocalPlayer;
            if (Utilities.IsValid(LocalPlayer))
            {
                IsInVR = LocalPlayer.IsUserInVR();
            }
            
            if (Networking.IsMaster)
            {
                UseContactsInVRLocal = UseContactsInVR;
            }
        }

        public void _OnPickup()
        {
            if (IsHeldLocally)
            {
                Log("already being adjusted");
                return;
            }

            if (IsInVR && useContactsInVRLocal)
            {
                return;
            }

            TakeOwnership();

            IsHeldLocally = true;
            SyncedIsBeingManipulated = true;
            // this.SendCustomEventDelayedFrames(nameof(FollowPickup), 1);
            _OnFollowPickup();
        }

        public void _OnDrop()
        {
            TakeOwnership();

            if (IsInVR && useContactsInVRLocal)
            {
                return;
            }

            IsHeldLocally = false;
            SyncedIsBeingManipulated = false;

            if (synced)
            {
                RequestSerialization();
            }

            OnDeserialization();

            UpdateHandlePosition();
            // Log("handle released, resetting position");
            // SendCustomNetworkEvent(NetworkEventTarget.All, nameof(UpdatePickupPosition));
        }

        private bool _isTrackingLeft, _isTrackingRight;
        private bool _isRunningContactLoop;
        [NonSerialized] public ContactSenderProxy LeftSender, RightSender;

        protected abstract void FollowPickup();
        
        protected abstract float RelativePosToNormalized(Vector3 relativePos);
        
        public void _OnFollowPickup()
        {
            if (!IsHeldLocally) return;

            FollowPickup();
            
            if (IsHeldLocally)
            {
                this.SendCustomEventDelayedFrames(nameof(_OnFollowPickup), 0);
            }

            if (synced)
            {
                RequestSerialization();
            }

            OnDeserialization();
        }

        private Vector3 _getLeftFingerPos()
        {
            if (LeftSender != null)
            {
                return LeftSender.position;
            }
            
            LogWarning("getting left finger bone position (remove nonserialized from left/right Sender)");
            Vector3 leftHandPos = LocalPlayer.GetBonePosition(HumanBodyBones.LeftIndexDistal);
            if (leftHandPos == Vector3.zero)
            {
                leftHandPos = LocalPlayer.GetBonePosition(HumanBodyBones.LeftIndexIntermediate);
            }

            return leftHandPos;
        }

        private Vector3 _getRightFingerPos()
        {
            if (RightSender != null)
            {
                return RightSender.position;
            }

            LogWarning("getting right finger bone position (remove nonserialized from left/right Sender)");
            Vector3 rightHandPos = LocalPlayer.GetBonePosition(HumanBodyBones.RightIndexDistal);
            if (rightHandPos == Vector3.zero)
            {
                rightHandPos = LocalPlayer.GetBonePosition(HumanBodyBones.RightIndexIntermediate);
            }

            return rightHandPos;
        }

        public void OnLeftContactEnter()
        {
            if (!isAuthorized) return;
            Log($"received {LeftSender.usage}");
            Log($"Left Contact Enter");

            if (!IsInVR)
            {
                return;
            }
            if (IsInVR && !useContactsInVRLocal)
            {
                return;
            }

            if (!_inLeftTrigger)
            {
                Log($"VR Pickup with target at {SyncedValueNormalized}");
                if (!_isRunningContactLoop)
                {
                    Log("starting FollowCollider");
                    this.SendCustomEventDelayedFrames(nameof(_OnFollowCollider), 1);
                    _isRunningContactLoop = true;
                }
                else
                {
                    LogWarning("loop already running");
                }
            }

            TakeOwnership();

            _inLeftTrigger = true;
            //_localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 1f, 1f, 0.2f);
        }

        public void OnRightContactEnter()
        {
            if (!isAuthorized) return;
            Log($"received {RightSender.usage}");
            Log($"Right Contact Enter");

            if (!IsInVR)
            {
                return;
            }
            if (IsInVR && !useContactsInVRLocal)
            {
                return;
            }

            if (!_inRightTrigger)
            {
                Log($"VR Pickup with target at {SyncedValueNormalized}");
                if (!_isRunningContactLoop)
                {
                    Log("starting FollowCollider");
                    this.SendCustomEventDelayedFrames(nameof(_OnFollowCollider), 1);
                    _isRunningContactLoop = true;
                }
                else
                {
                    LogWarning("loop already running");
                }
            }

            TakeOwnership();

            _inRightTrigger = true;
            //_localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 1f, 1f, 0.2f);
        }

        public void OnLeftContactExit()
        {
            if (!isAuthorized) return;
            Log($"Left Contact Exit");
            _inLeftTrigger = false;
            // _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 1f, 1f, 0.2f);
        }

        public void OnRightContactExit()
        {
            if (!isAuthorized) return;
            Log($"Right Contact Exit");
            _inRightTrigger = false;
            // _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 1f, 1f, 0.2f);
        }
        
        
        public void _OnFollowCollider()
        {
            if (!isAuthorized) return;
            // if (_isDesktop) return; // should not be required

            var followLeft = Handle.leftGrabbed && _inLeftTrigger;
            var followRight = Handle.rightGrabbed && _inRightTrigger;

            if (_inLeftTrigger || _inRightTrigger)
            {
                this.SendCustomEventDelayedFrames(nameof(_OnFollowCollider), 1);
                _isRunningContactLoop = true;
            }
            else
            {
                Log("stopping follow collider loop");
                _isRunningContactLoop = false;
            }

            if (_isTrackingLeft && !followLeft)
            {
                Log($"VR Drop with target at {SyncedValueNormalized}");
                _isTrackingLeft = false;

                LocalPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 1f, 1f, 0.2f);
                return;
            }

            if (_isTrackingRight && !followRight)
            {
                Log($"VR Drop with target at {SyncedValueNormalized}");
                _isTrackingRight = false;

                LocalPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 1f, 1f, 0.2f);
                return;
            }

            if (followLeft)
            {
                if (!_isTrackingLeft)
                {
                    _isTrackingLeft = true;
                    LocalPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 1f, 1f, 0.2f);
                }
            }
            else if (followRight)
            {
                if (!_isTrackingRight)
                {
                    _isTrackingRight = true;
                    LocalPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 1f, 1f, 0.2f);
                }
            }

            if (followLeft || followRight)
            {
                SyncedIsBeingManipulated = true;
                Vector3 globalFingerPos = _inRightTrigger ? _getRightFingerPos() : _getLeftFingerPos();
                var localFingerPos = transform.InverseTransformPoint(globalFingerPos);

                SyncedValueNormalized = RelativePosToNormalized(localFingerPos);

                if (synced)
                {
                    RequestSerialization();
                }

                OnDeserialization();
            }
            // else if(_syncedIsBeingManipulated)
            // {
            //     Log($"VR Drop with target at {_syncedValueNormalized}");
            //     _syncedIsBeingManipulated = false;
            //     
            //     if (synced)
            //     {
            //         RequestSerialization();
            //     }  
            //     if (_inLeftTrigger)
            //     {
            //         _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 1f, 1f, 0.2f);
            //     }
            //
            //     if (_inRightTrigger)
            //     {
            //         _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 1f, 1f, 0.2f);
            //     }
            // }
        }

        protected void UpdateHandlePosition()
        {
            if (RigidBody)
            {
                RigidBody.angularVelocity = Vector3.zero;
                RigidBody.velocity = Vector3.zero;
            }

            var handleReset = HandleReset;
            // parentConstraint.GlobalWeight = 1;
            if (Utilities.IsValid(Handle))
            {
                Handle.transform.SetPositionAndRotation(
                    handleReset.position,
                    handleReset.rotation
                );
            }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
            if (Utilities.IsValid(RigidBody))
            {
                RigidBody.MarkDirty();
            }

            if (Utilities.IsValid(Handle))
            {
                Handle.transform.MarkDirty();
            }
#endif
        }

        public override void OnDeserialization()
        {
            if (!Mathf.Approximately(SyncedValueNormalized, LastSyncedValueNormalized))
            {
                _UpdateTargetValue(SyncedValueNormalized);

                LastSyncedValueNormalized = SyncedValueNormalized;
            }
        }

#if UNITY_EDITOR && !COMPILER_UDONSHARP

        [ContextMenu("Apply Values")]
        public virtual void ApplyValues()
        {
            SetupFaderHandle();
            SetupPickup();
            SetupPickupRigidBody();
        }
#endif
    }
}