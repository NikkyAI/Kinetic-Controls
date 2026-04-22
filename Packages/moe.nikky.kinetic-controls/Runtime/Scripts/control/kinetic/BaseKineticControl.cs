using System;
using System.ComponentModel;
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

        [FormerlySerializedAs("pickupReset")] //
        [FormerlySerializedAs("handleResetTransform")]
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
        [UsedImplicitly]
        public bool useContactsInVRLocal
        {
            get => handle.useContactsInVRLocal;
            set
            {
                handle.UseContactsInVRLocal = value;
            }
        }

        public bool UseContactsInVRLocal
        {
            get => handle.useContactsInVRLocal;
            set
            {
                handle.UseContactsInVRLocal = value;
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
        }

        protected float LastSyncedValueNormalized = 0;

        protected Transform HandleResetTransform => handleReset;

        [Header("Kinetic Control - Internals")] // header
        protected VRC_Pickup pickup;
        protected Rigidbody rigidBody;
        protected bool PickupHasObjectSync;
        protected VRCPlayerApi LocalPlayer;
        protected bool IsHeldLocally;
        protected bool IsInVR;

        private bool _inLeftTrigger;
        private bool _inRightTrigger;

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetupHandle()
        {
            Log("SetupHandle");
            if (Utilities.IsValid(handle))
            {
                handle.Register(this);

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


        private void SetupPickup()
        {
            Log("SetupPickup");
            if (Utilities.IsValid(handle))
            {
                // Log($"getting pickup from handle: {handle.name}");
                pickup = handle.GetComponent<VRC_Pickup>();
            }

            if (!Utilities.IsValid(pickup))
            {
                // Log($"getting pickup from self: {name}");
                pickup = gameObject.GetComponent<VRC_Pickup>();
                // _pickup.pickupable = !_isDesktop;
            }

            Log($"pickup is {pickup}");

//             if (Utilities.IsValid(Pickup))
//             {
//                 if (!IsInVR)
//                 {
//                     //TODO: define pickup ranges for desktop and VR
// #if COMPILER_UDONSHARP
//                     Pickup.proximity = 5f;
// #endif
//                 }
//             }
        }

        private void SetupPickupRigidBody()
        {
            Log("SetupPickupRigidbody");
            if (Utilities.IsValid(pickup))
            {
                rigidBody = pickup.GetComponent<Rigidbody>();
                rigidBody.useGravity = false;
                rigidBody.isKinematic = false;
                rigidBody.drag = 10f;
                rigidBody.angularDrag = 5f;
                PickupHasObjectSync = pickup.GetComponent<VRCObjectSync>() != null ||
                                      pickup.GetComponent("MMMaellon.SmartObjectSync") != null;
            }
            else
            {
                LogError($"no pickup found in {handle.name}");
            }
        }

        protected override void _Init()
        {
            base._Init();
            Log("Base KineticControl Init");
            SetupHandle();
            SetupPickup();
            SetupPickupRigidBody();

            LocalPlayer = Networking.LocalPlayer;
            if (Utilities.IsValid(LocalPlayer))
            {
                IsInVR = LocalPlayer.IsUserInVR();
            }

            // UseContactsInVRLocal = !UseContactsInVR;
            useContactsInVRLocal = UseContactsInVR;

            SyncedValueNormalized = defaultValueNormalized;
        }

        public void _OnPickup()
        {
            if (!IsAuthorized)
            {
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

            TakeOwnership();

            IsHeldLocally = true;
            SyncedIsBeingManipulated = true;
            // this.SendCustomEventDelayedFrames(nameof(FollowPickup), 1);
            _OnFollowPickup();
        }

        public void _OnDrop()
        {
            if (!IsAuthorized)
            {
                return;
            }

            Log("_OnDrop");
            TakeOwnership();

            IsHeldLocally = false;
            SyncedIsBeingManipulated = false;

            //if (IsInVR && useContactsInVRLocal)
            //{
            //    return;
            //}


            if (!IsInVR)
            {
                if (Utilities.IsValid(debugDesktopRaytrace))
                {
                    debugDesktopRaytrace.gameObject.SetActive(false);
                }
            }

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
        [HideInInspector] public ContactSenderProxy LeftSender, RightSender;

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
            if (!IsAuthorized) return;
            Log($"OnLeftContactEnter received {LeftSender.usage}");
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
            if (!IsAuthorized) return;
            Log($"OnRightContactEnter received {RightSender.usage}");
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
            if (!IsAuthorized) return;
            Log($"Left Contact Exit");
            _inLeftTrigger = false;
            UpdateHandlePosition();
            // _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 1f, 1f, 0.2f);
        }

        public void OnRightContactExit()
        {
            if (!IsAuthorized) return;
            Log($"Right Contact Exit");
            _inRightTrigger = false;
            UpdateHandlePosition();
            // _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 1f, 1f, 0.2f);
        }


        public void _OnFollowCollider()
        {
            if (!IsAuthorized) return;
            // if (_isDesktop) return; // should not be required

            var followLeft = handle.LeftGrabbed && _inLeftTrigger;
            var followRight = handle.RightGrabbed && _inRightTrigger;

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
        }

        public void UpdateHandlePosition()
        {
            if (rigidBody)
            {
                rigidBody.angularVelocity = Vector3.zero;
                rigidBody.velocity = Vector3.zero;
            }

            if (Utilities.IsValid(HandleResetTransform))
            {
                // parentConstraint.GlobalWeight = 1;
                if (Utilities.IsValid(handle))
                {
                    handle.transform.SetPositionAndRotation(
                        HandleResetTransform.position,
                        HandleResetTransform.rotation
                    );
                }
                else
                {
                    LogWarning("failed to update handle position");
                }
            }
            else
            {
                LogWarning($"handle reset is not valid on {name}");
            }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
            if (Utilities.IsValid(rigidBody))
            {
                rigidBody.MarkDirty();
            }

            if (Utilities.IsValid(handle))
            {
                handle.transform.MarkDirty();
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
            SetupHandle();
            SetupPickup();
            SetupPickupRigidBody();

            handle.EditorACL = AccessControl;
            handle.EditorDebugLog = DebugLog;
            handle.EditorEnforceACL = EnforceACL;

            // OnDeserialization();
            UpdateHandlePosition();
        }
#endif
    }
}