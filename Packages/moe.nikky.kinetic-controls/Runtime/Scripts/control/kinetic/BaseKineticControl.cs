using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using nikkyai.Utils;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC;
using VRC.Dynamics;
using VRC.SDK3.Components;
using VRC.SDKBase;

// ReSharper disable ForCanBeConvertedToForeach

namespace nikkyai.control.kinetic
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public abstract class BaseKineticControl : BaseSmoothedControl
    {
        [Header("Kinetic Control")] //
        [SerializeField, UdonSynced] //
        [FormerlySerializedAs("valueSynced")]//
        protected bool synced = true;

        [Header("Kinetic Control - MIDI - Requires VRC_MidiListener Component")] //
        [SerializeField, Description("Requires a VRC MIDI Listened with CC enabled")]
        protected bool midiEnabled = true;
        [SerializeField, Range(0,15)]
        protected int midiChannel = 0;
        [SerializeField, Range(0,127)]
        protected int midiNumber = 0;
        [SerializeField, Range(0,127)]
        protected int midiInputRangeStart = 0;
        [SerializeField, Range(0,127)]
        protected int midiInputRangeEnd = 127;
        
        [Header("Kinetic Control - Components")] //
        [SerializeField]
        internal Handle handle;

        [Tooltip("should be the same as targetIndicator or a child, handle will be reset to the given transform position / rotation on release")] //
        [SerializeField]
        private Transform handleReset;

        [Header("Kinetic Control - Debug")] // header
        [FormerlySerializedAs("debugRaytrace")]
        [SerializeField]
        protected Transform debugDesktopRaytrace;

        public override bool Synced
        {
            get => synced;
            set
            {
                if (!IsAuthorized) return;

                var prevValue = SyncedValueNormalized;
                TakeOwnership();
                Log($"set synced to {value}");
                synced = value;
                Log($"set value to {SyncedValueNormalized} => {prevValue}");
                SyncedValueNormalized = prevValue;

                RequestSerialization();
                OnDeserialization();
            }
        }

        protected abstract bool UseContactsInVR { get; }

        // [HideInInspector, FieldChangeCallback(nameof(UseContactsInVRLocal))]
        // [UsedImplicitly]
        // public bool useContactsInVRLocal
        // {
        //     get => handle.useContactsInVRLocal;
        //     set
        //     {
        //         handle.UseContactsInVRLocal = value;
        //     }
        // }

        public virtual bool HandleUseContactsInVR
        {
            get => handle.useContactsInVR;
            set => handle.UseContactsInVR = value;
            // Log($"UseContactsInVR (vr: {IsInVR}) {useContactsInVRLocal} -> {value}");
            // useContactsInVRLocal = value;
            //
            // if (Utilities.IsValid(pickup))
            // {
            //     if (IsInVR)
            //     {
            //         if (useContactsInVRLocal)
            //         {
            //             pickup.pickupable = false;
            //             pickup.proximity = -1f;
            //             pickup.InteractionText = "error";
            //             //todo: reference to contactsender to edit it
            //             // _contactReceiver.contentTypes = DynamicsUsageFlags.Avatar;
            //         }
            //         else
            //         {
            //             pickup.pickupable = true;
            //             pickup.proximity = 1f;
            //             pickup.InteractionText = "Grab to adjust";
            //             //todo: reference to contactsender to edit it
            //             //_contactReceiver.contentTypes = DynamicsUsageFlags.Nothing;
            //         }
            //     }
            //     else
            //     {
            //         pickup.pickupable = true;
            //         pickup.InteractionText = "Grab to adjust";
            //         pickup.proximity = 5f; // add serialized field to configure range
            //         //todo: reference to contactsender to edit it
            //         // _contactReceiver.contentTypes = DynamicsUsageFlags.Nothing;
            //     }
            // }
            // else
            // {
            //     LogError($"no pickup found in {name}");
            // }
        }

        private float _lastSyncedValueNormalized = 0;

        // [Header("Kinetic Control - Internals")] // header
        // protected VRC_Pickup Pickup;
        // protected Rigidbody RigidBody;
        // protected bool PickupHasObjectSync;

        // protected bool IsHeldLocally;
        // protected bool IsInVR;

        // private bool _inLeftTrigger;
        // private bool _inRightTrigger;
        //
        // private bool _isTrackingLeft, _isTrackingRight;
        // private bool _isRunningContactLoop;
        // private ContactSenderProxy _leftSender, _rightSender;

        private void SetupHandle()
        {
            Log("SetupHandle");
            if (Utilities.IsValid(handle))
            {
                handle._EnsureInit();
                handle.Register(this);

                handle.handleReset = handleReset;
                HandleUseContactsInVR = UseContactsInVR;

                // _contactReceiver = faderHandle.GetComponent<ContactReceiver>();

#if UNITY_EDITOR && !COMPILER_UDONSHARP
                handle.EditorACL = AccessControl;
                handle.EditorEnforceACL = EnforceACL;
                handle.EditorDebugLog = DebugLog;
#endif
            }
            else
            {
                LogError($"missing handle in {name}");
            }
        }


        // private void SetupPickup()
        // {
        //     Log("SetupPickup");
        //     if (Utilities.IsValid(handle))
        //     {
        //         // Log($"getting pickup from handle: {handle.name}");
        //         Pickup = handle.GetComponent<VRC_Pickup>();
        //     }
        //
        //     if (!Utilities.IsValid(Pickup))
        //     {
        //         // Log($"getting pickup from self: {name}");
        //         Pickup = gameObject.GetComponent<VRC_Pickup>();
        //         // _pickup.pickupable = !_isDesktop;
        //     }
        //
        //     Log($"pickup is {Pickup}");
        // }
        //
        // private void SetupPickupRigidBody()
        // {
        //     Log("SetupPickupRigidbody");
        //     if (Utilities.IsValid(Pickup))
        //     {
        //         RigidBody = Pickup.GetComponent<Rigidbody>();
        //         RigidBody.useGravity = false;
        //         RigidBody.isKinematic = false;
        //         RigidBody.drag = 10f;
        //         RigidBody.angularDrag = 5f;
        //         PickupHasObjectSync = Pickup.GetComponent<VRCObjectSync>() != null ||
        //                               Pickup.GetComponent("MMMaellon.SmartObjectSync") != null;
        //     }
        //     else
        //     {
        //         LogError($"no pickup found in {handle.name}");
        //     }
        // }

        protected override void _Init()
        {
            base._Init();
            Log("Base KineticControl Init");
            SetupHandle();
            // SetupPickup();
            // SetupPickupRigidBody();

            // LocalPlayer = Networking.LocalPlayer;
            // if (Utilities.IsValid(Networking.LocalPlayer))
            // {
            //     _isInVR = Networking.LocalPlayer.IsUserInVR();
            // }

            // UseContactsInVRLocal = !UseContactsInVR;
            // UseContactsInVRLocal = UseContactsInVR;

            SyncedValueNormalized = defaultValueNormalized;
        }

        // public void _OnPickup()
        // {
        //     if (!IsAuthorized)
        //     {
        //         return;
        //     }
        //
        //     Log("_OnPickup");
        //     if (IsHeldLocally)
        //     {
        //         Log("already being adjusted");
        //         return;
        //     }
        //
        //     // if (IsInVR && useContactsInVRLocal)
        //     // {
        //     //     LogWarning("dropping pickup");
        //     //     pickup.Drop();
        //     //     return;
        //     // }
        //
        //     TakeOwnership();
        //
        //     IsHeldLocally = true;
        //     SyncedIsBeingManipulated = true;
        //     // this.SendCustomEventDelayedFrames(nameof(FollowPickup), 1);
        //     _OnFollowPickup();
        // }
        //
        // public void _OnDrop()
        // {
        //     if (!IsAuthorized)
        //     {
        //         return;
        //     }
        //
        //     Log("_OnDrop");
        //     TakeOwnership();
        //
        //     IsHeldLocally = false;
        //     SyncedIsBeingManipulated = false;
        //
        //     //if (IsInVR && useContactsInVRLocal)
        //     //{
        //     //    return;
        //     //}
        //
        //
        //     if (!IsInVR)
        //     {
        //         if (Utilities.IsValid(debugDesktopRaytrace))
        //         {
        //             debugDesktopRaytrace.gameObject.SetActive(false);
        //         }
        //     }
        //
        //     if (synced)
        //     {
        //         RequestSerialization();
        //     }
        //
        //     OnDeserialization();
        //
        //     UpdateHandlePosition();
        //     // Log("handle released, resetting position");
        //     // SendCustomNetworkEvent(NetworkEventTarget.All, nameof(UpdatePickupPosition));
        // }


        public abstract void FollowPickup();

        protected abstract float PosToNormalized(Vector3 relativePos);

        // public void _OnFollowPickup()
        // {
        //     if (!IsHeldLocally) return;
        //
        //     FollowPickup();
        //
        //     if (IsHeldLocally)
        //     {
        //         this.SendCustomEventDelayedFrames(nameof(_OnFollowPickup), 0);
        //     }
        //
        //     if (synced)
        //     {
        //         TakeOwnership();
        //         RequestSerialization();
        //     }
        //
        //     OnDeserialization();
        // }

        // private Vector3 _getLeftFingerPos()
        // {
        //     if (_leftSender != null)
        //     {
        //         return _leftSender.position;
        //     }
        //
        //     LogWarning("getting left finger bone position (remove nonserialized from left/right Sender)");
        //     Vector3 leftHandPos = LocalPlayer.GetBonePosition(HumanBodyBones.LeftIndexDistal);
        //     if (leftHandPos == Vector3.zero)
        //     {
        //         leftHandPos = LocalPlayer.GetBonePosition(HumanBodyBones.LeftIndexIntermediate);
        //     }
        //
        //     return leftHandPos;
        // }
        //
        // private Vector3 _getRightFingerPos()
        // {
        //     if (_rightSender != null)
        //     {
        //         return _rightSender.position;
        //     }
        //
        //     LogWarning("getting right finger bone position (remove nonserialized from left/right Sender)");
        //     Vector3 rightHandPos = LocalPlayer.GetBonePosition(HumanBodyBones.RightIndexDistal);
        //     if (rightHandPos == Vector3.zero)
        //     {
        //         rightHandPos = LocalPlayer.GetBonePosition(HumanBodyBones.RightIndexIntermediate);
        //     }
        //
        //     return rightHandPos;
        // }

        // public void OnLeftContactEnter(ContactSenderProxy leftSender)
        // {
        //     if (!IsAuthorized) return;
        //     // _leftSender = leftSender;
        //     Log($"OnLeftContactEnter received {leftSender.usage}");
        //     Log($"Left Contact Enter");
        //     
        //     if (!IsInVR)
        //     {
        //         Log("not in vr");
        //         return;
        //     }
        //
        //     if (IsInVR && !UseContactsInVRLocal)
        //     {
        //         Log("not using contacts");
        //         return;
        //     }
        //
        //     if (!_inLeftTrigger)
        //     {
        //         Log($"VR contact with target at {SyncedValueNormalized}");
        //         if (!_isRunningContactLoop)
        //         {
        //             Log("starting FollowCollider");
        //             this.SendCustomEventDelayedFrames(nameof(_OnFollowCollider), 1);
        //             _isRunningContactLoop = true;
        //         }
        //         else
        //         {
        //             LogWarning("loop already running");
        //         }
        //     }
        //
        //     TakeOwnership();
        //
        //     _inLeftTrigger = true;
        //     //_localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 1f, 1f, 0.2f);
        // }
        //
        // public void OnRightContactEnter(ContactSenderProxy rightSender)
        // {
        //     if (!IsAuthorized) return;
        //     // _rightSender = rightSender;
        //     Log($"OnRightContactEnter received {rightSender.usage}");
        //     Log($"Right Contact Enter");
        //
        //     if (!IsInVR)
        //     {
        //         Log("not in vr");
        //         return;
        //     }
        //
        //     if (IsInVR && !UseContactsInVRLocal)
        //     {
        //         Log("not using contacts");
        //         return;
        //     }
        //
        //     if (!_inRightTrigger)
        //     {
        //         Log($"VR contact with target at {SyncedValueNormalized}");
        //         if (!_isRunningContactLoop)
        //         {
        //             Log("starting FollowCollider");
        //             this.SendCustomEventDelayedFrames(nameof(_OnFollowCollider), 1);
        //             _isRunningContactLoop = true;
        //         }
        //         else
        //         {
        //             LogWarning("loop already running");
        //         }
        //     }
        //
        //     TakeOwnership();
        //
        //     _inRightTrigger = true;
        //     //_localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 1f, 1f, 0.2f);
        // }

        // public void OnLeftContactExit()
        // {
        //     if (!IsAuthorized) return;
        //     Log($"Left Contact Exit");
        //     _inLeftTrigger = false;
        //     // _leftSender = null;
        //     UpdateHandlePosition();
        //     // _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 1f, 1f, 0.2f);
        // }
        //
        // public void OnRightContactExit()
        // {
        //     if (!IsAuthorized) return;
        //     Log($"Right Contact Exit");
        //     _inRightTrigger = false;
        //     // _rightSender = null;
        //     UpdateHandlePosition();
        //     // _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 1f, 1f, 0.2f);
        // }
        //
        // public void _OnFollowCollider()
        // {
        //     if (!IsAuthorized) return;
        //     // if (_isDesktop) return; // should not be required
        //
        //     var followLeft = handle.LeftGrabbed && _inLeftTrigger;
        //     var followRight = handle.RightGrabbed && _inRightTrigger;
        //
        //     if (_inLeftTrigger || _inRightTrigger)
        //     {
        //         this.SendCustomEventDelayedFrames(nameof(_OnFollowCollider), 1);
        //         _isRunningContactLoop = true;
        //     }
        //     else
        //     {
        //         Log("stopping follow collider loop");
        //         _isRunningContactLoop = false;
        //     }
        //
        //     if (_isTrackingLeft && !followLeft)
        //     {
        //         Log($"VR Drop with target at {SyncedValueNormalized}");
        //         _isTrackingLeft = false;
        //
        //         LocalPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 1f, 1f, 0.2f);
        //         return;
        //     }
        //
        //     if (_isTrackingRight && !followRight)
        //     {
        //         Log($"VR Drop with target at {SyncedValueNormalized}");
        //         _isTrackingRight = false;
        //
        //         LocalPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 1f, 1f, 0.2f);
        //         return;
        //     }
        //
        //     if (followLeft)
        //     {
        //         if (!_isTrackingLeft)
        //         {
        //             _isTrackingLeft = true;
        //             LocalPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 1f, 1f, 0.2f);
        //         }
        //     }
        //     else if (followRight)
        //     {
        //         if (!_isTrackingRight)
        //         {
        //             _isTrackingRight = true;
        //             LocalPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 1f, 1f, 0.2f);
        //         }
        //     }
        //
        //     if (followLeft || followRight)
        //     {
        //         SyncedIsBeingManipulated = true;
        //         Vector3 globalFingerPos = _inRightTrigger ? handle.RightFingerPos() : handle.LeftFingerPos();
        //         // var localFingerPos = transform.InverseTransformPoint(globalFingerPos);
        //
        //         SyncedValueNormalized = PosToNormalized(globalFingerPos);
        //
        //         if (synced)
        //         {
        //             RequestSerialization();
        //         }
        //
        //         OnDeserialization();
        //     }
        // }

        public void OnMoveHandle(Vector3 absolutePosition)
        {
            SyncedIsBeingManipulated = true;
            SyncedValueNormalized = PosToNormalized(absolutePosition);
            if (synced)
            {
                TakeOwnership();
                RequestSerialization();
            }
            OnDeserialization();
        }
        public void OnDropHandle()
        {
            SyncedIsBeingManipulated = false;
        }

//         public void UpdateHandlePosition()
//         {
//             handle.ResetTransform();
//             // handle.FreezeRigidBody();
//             // if (RigidBody)
//             // {
//             //     RigidBody.angularVelocity = Vector3.zero;
//             //     RigidBody.velocity = Vector3.zero;
//             // }
//
// //             if (Utilities.IsValid(handleReset))
// //             {
// //                 // parentConstraint.GlobalWeight = 1;
// //                 if (Utilities.IsValid(handle))
// //                 {
// //                     handle.transform.SetPositionAndRotation(
// //                         handleReset.position,
// //                         handleReset.rotation
// //                     );
// //                 }
// //                 else
// //                 {
// //                     LogWarning("failed to update handle position");
// //                 }
// //             }
// //             else
// //             {
// //                 LogWarning($"handle reset is not valid on {name}");
// //             }
// //
// // #if UNITY_EDITOR && !COMPILER_UDONSHARP
// //             // if (Utilities.IsValid(RigidBody))
// //             // {
// //             //     RigidBody.MarkDirty();
// //             // }
// //
// //             if (Utilities.IsValid(handle))
// //             {
// //                 handle.transform.MarkDirty();
// //             }
// // #endif
//         }

        public override void OnDeserialization()
        {
            if (!Mathf.Approximately(SyncedValueNormalized, _lastSyncedValueNormalized))
            {
                _UpdateTargetValue(SyncedValueNormalized);

                _lastSyncedValueNormalized = SyncedValueNormalized;
            }
        }
        
        // public override void MidiNoteOn(int channel, int number, int velocity)
        // {
        //     if (!IsAuthorized) return;
        //     base.MidiNoteOn(channel, number, velocity);
        //     if (!midiEnabled) return;
        //     
        //     Log($"MidiNoteOn({channel}, {number}, {velocity})");
        // }
        //
        // public override void MidiNoteOff(int channel, int number, int velocity)
        // {
        //     if (!IsAuthorized) return;
        //     base.MidiNoteOff(channel, number, velocity);
        //     if (!midiEnabled) return;
        //     
        //     Log($"MidiNoteOff({channel}, {number}, {velocity})");
        // }

        public override void MidiControlChange(int channel, int number, int value)
        {
            if (!IsAuthorized) return;
            base.MidiControlChange(channel, number, value);
            if (!midiEnabled) return;
            
            Log($"MidiControlChange({channel}, {number}, {value})");
            if (channel == midiChannel && number == midiNumber)
            {
                float normalizedValue = Mathf.InverseLerp(midiInputRangeStart, midiInputRangeEnd, value);
                Log($"normalized value: {normalizedValue}");
                SetValue(normalizedValue);
            }
        }
        public void DebugDesktopRaytrace(bool debugActive)
        {
            if (Utilities.IsValid(debugDesktopRaytrace))
            {
                debugDesktopRaytrace.gameObject.SetActive(debugActive);
            }
        }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        protected override void OnValidate()
        {
            if (Application.isPlaying) return;
            base.OnValidate();

            if (
                ValidationCache.ShouldRunValidation(
                    this,
                    HashCode.Combine(
                        AccessControl,
                        DebugLog,
                        EnforceACL
                    )
                )
            )
            {
                ApplyValues();
            }
        }
        
        public override void ApplyValues()
        {
            base.ApplyValues();
            _EnsureInit();
            // SetupPickup();
            // SetupPickupRigidBody();

            handle.EditorACL = AccessControl;
            handle.EditorDebugLog = DebugLog;
            handle.EditorEnforceACL = EnforceACL;
            handle.handleReset = handleReset;

            // OnDeserialization();
            handle.ResetTransform();
        }
#endif
    }
}