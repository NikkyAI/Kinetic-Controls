#define HIDE_INSPECTOR

using System;
using System.Runtime.CompilerServices;
using nikkyai.common;
using nikkyai.extensions;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC;
using VRC.Dynamics;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace nikkyai.control.kinetic
{
    [RequireComponent(typeof(VRC_Pickup))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Handle : ACLBaseReadonly
    {
        [SerializeField]
        [ReadOnly]
        private BaseKineticControl[] controlBehaviours = { };

        [FormerlySerializedAs("handleReset")]
        [Header("Handle - Internals")]
        [Tooltip(
            "should be the same as targetIndicator or a child, " +
            "handle will be reset to the given transform position / rotation on release")]
        [SerializeField]
#if HIDE_INSPECTOR
        [HideInInspector]
#endif
        public Transform resetTransform;

        [SerializeField]
        [FieldChangeCallback(nameof(UseContactsInVR))]
#if HIDE_INSPECTOR
        [HideInInspector]
#endif
        public bool useContactsInVR = true;

        [SerializeField]
#if HIDE_INSPECTOR
        [HideInInspector]
#endif
        internal VRC_Pickup pickup;

        [SerializeField]
#if HIDE_INSPECTOR
        [HideInInspector]
#endif
        private Rigidbody rigidBody;

        private ContactSenderProxy _leftSender, _rightSender;

        private bool _isHeldLocally = false;
        private bool _leftGrabbed = false, _rightGrabbed = false;

        private bool _pickupHasObjectSync = false;

        // public bool PickupHasObjectSync => _pickupHasObjectSync;
        private bool _inLeftTrigger = false, _inRightTrigger = false;

        private bool _isTrackingLeft = false, _isTrackingRight = false;
        private bool _isRunningContactLoop = false;

        public bool UseContactsInVR
        {
            get => useContactsInVR;
            set
            {
                Log($"UseContactsInVR (vr: {IsInVR}) {useContactsInVR} -> {value}");
                useContactsInVR = value;

                if (!Utilities.IsValid(pickup))
                {
                    pickup = GetComponent<VRC_Pickup>();
                }

                if (IsInVR)
                {
                    if (useContactsInVR)
                    {
                        Log("disable pickup");
                        // _pickup.pickupable = false;
                        pickup.proximity = -1f;
                        pickup.InteractionText = "error";
                        //todo: reference to contactsender to edit it
                        // _contactReceiver.contentTypes = DynamicsUsageFlags.Avatar;
                    }
                    else
                    {
                        Log("enable pickup");
                        //  _pickup.pickupable = true;
                        pickup.proximity = 1f;
                        pickup.InteractionText = "Grab to adjust";
                        //todo: reference to contactsender to edit it
                        //_contactReceiver.contentTypes = DynamicsUsageFlags.Nothing;
                        OnLeftContactExit();
                        OnRightContactExit();
                        _leftGrabbed = false;
                        _rightGrabbed = false;
                        // foreach (var baseKineticControl in _controlBehaviour)
                        // {
                        //     baseKineticControl.OnLeftContactExit();
                        //     baseKineticControl.OnRightContactExit();
                        // }
                    }
                }
                else
                {
                    Log("enable pickup");
                    // _pickup.pickupable = true;
                    pickup.InteractionText = "Grab to adjust";
                    pickup.proximity = 5f; // add serialized field to configure range
                    //todo: reference to contactsender to edit it
                    // _contactReceiver.contentTypes = DynamicsUsageFlags.Nothing;
                }

                AccessChanged();
            }
        }
        
        protected override string LogPrefix => nameof(Handle);

        void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            base._Init();

            // SetupPickup();
            // SetupPickupRigidbody();

            // _localPlayer = Networking.LocalPlayer;
            // if (Utilities.IsValid(Networking.LocalPlayer))
            // {
            //     _isInVR = Networking.LocalPlayer.IsUserInVR();
            // }
            _isHeldLocally = false;


            _leftGrabbed = false;
            _rightGrabbed = false;

            AccessChanged();
        }

        protected override void AccessChanged()
        {
            // DisableInteractive = !isAuthorized || _isInVR;
            if (!Utilities.IsValid(pickup))
            {
                pickup = GetComponent<VRC_Pickup>();
            }

            pickup.pickupable = IsAuthorized && (!IsInVR || useContactsInVR);
        }

        // private bool _isInteracting = false;
        //
        // public override void Interact()
        // {
        //     if (!isAuthorized) return;
        //     _isInteracting = true;
        //     touchFader._HandleInteract();
        // }
        //
        // public override void InputUse(bool value, VRC.Udon.Common.UdonInputEventArgs args)
        // {
        //     if (!_isInteracting) return;
        //     if (!isAuthorized) return;
        //     if (!value)
        //     {
        //         touchFader._HandleRelease();
        //     }
        // }


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

            if (IsInVR && useContactsInVR)
            {
                LogWarning("dropping pickup, using contacts instead");
                pickup.Drop();
                return;
            }

            if (!IsAuthorized)
            {
                return;
            }

            Log("_OnPickup");
            if (_isHeldLocally)
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
            _isHeldLocally = true;
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

            _isHeldLocally = false;
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
            if (!_isHeldLocally) return;
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
            if (_isHeldLocally)
            {
                this.SendCustomEventDelayedFrames(nameof(_OnFollowPickup), 0);
            }
        }

        public override void InputGrab(bool value, UdonInputEventArgs args)
        {
            if (!IsInVR) return;
            if (!UseContactsInVR) return;

            // Log($"InputGrab({value}, {args.handType})");
            if (value)
            {
                // if (!_leftGrabbed && !_rightGrabbed)
                // {
                //     Log("starting FollowCollider");
                //     this.SendCustomEventDelayedFrames(nameof(_OnFollowCollider), 1);
                // }

                if (args.handType == HandType.LEFT)
                {
                    if (!_leftGrabbed)
                    {
                        Log("Left Grabbed");
                    }

                    _leftGrabbed = true;
                }

                if (args.handType == HandType.RIGHT)
                {
                    if (!_rightGrabbed)
                    {
                        Log("Right Grabbed");
                    }

                    _rightGrabbed = true;
                }
            }
            else
            {
                if (args.handType == HandType.LEFT)
                {
                    if (_leftGrabbed)
                    {
                        Log($"Left Released");
                    }

                    _leftGrabbed = false;
                }

                if (args.handType == HandType.RIGHT)
                {
                    if (_rightGrabbed)
                    {
                        Log($"Right Released");
                    }

                    _rightGrabbed = false;
                }
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            if (!IsAuthorized) return;
            // touchFader._OnTriggerEnter(other.GetInstanceID());
        }

        public void OnTriggerExit(Collider other)
        {
            if (!IsAuthorized) return;
            // touchFader._OnTriggerExit(other.GetInstanceID());
        }

        public override void OnContactEnter(ContactEnterInfo contactInfo)
        {
            if (!IsInVR) return;
            if (!contactInfo.contactSender.player.isLocal) return;
            if (!IsAuthorized) return;

            Log($"Contact Enter {contactInfo.contactPoint} {contactInfo.matchingTags.Length}");
            for (var i = 0; i < contactInfo.matchingTags.Length; i++)
            {
                var matchingTag = contactInfo.matchingTags[i];
                Log(matchingTag);
            }

            if (contactInfo.matchingTags.Contains("FingerIndexL"))
            {
                _leftSender = contactInfo.contactSender;
                OnLeftContactEnter(contactInfo.contactSender);

                return;
            }

            if (contactInfo.matchingTags.Contains("FingerIndexR"))
            {
                _rightSender = contactInfo.contactSender;
                OnRightContactEnter(contactInfo.contactSender);

                return;
            }
        }

        public override void OnContactExit(ContactExitInfo contactInfo)
        {
            if (!IsInVR) return;
            if (!IsAuthorized) return;
            if (!contactInfo.contactSender.player.isLocal) return;

            if (contactInfo.matchingTags.Contains("FingerIndexL"))
            {
                Log("Contact Exit Left");
                _leftSender = null;
                OnLeftContactExit();
            }


            if (contactInfo.matchingTags.Contains("FingerIndexR"))
            {
                Log("Contact Exit Right");
                _rightSender = null;
                OnRightContactExit();
            }
        }


        public Vector3 LeftFingerPos()
        {
            if (_leftSender != null)
            {
                return _leftSender.position;
            }

            LogWarning("getting left finger bone position");
            Vector3 leftHandPos = LocalPlayer.GetBonePosition(HumanBodyBones.LeftIndexDistal);
            if (leftHandPos == Vector3.zero)
            {
                leftHandPos = LocalPlayer.GetBonePosition(HumanBodyBones.LeftIndexIntermediate);
            }

            return leftHandPos;
        }

        public Vector3 RightFingerPos()
        {
            if (_rightSender != null)
            {
                return _rightSender.position;
            }

            LogWarning("getting right finger bone position");
            Vector3 rightHandPos = LocalPlayer.GetBonePosition(HumanBodyBones.RightIndexDistal);
            if (rightHandPos == Vector3.zero)
            {
                rightHandPos = LocalPlayer.GetBonePosition(HumanBodyBones.RightIndexIntermediate);
            }

            return rightHandPos;
        }


        public void _OnFollowCollider()
        {
            if (!IsAuthorized) return;
            // if (_isDesktop) return; // should not be required

            var followLeft = _leftGrabbed && _inLeftTrigger;
            var followRight = _rightGrabbed && _inRightTrigger;

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
                // Log($"VR Drop with target at {SyncedValueNormalized}");
                Log($"VR stop tracking left");
                _isTrackingLeft = false;

                LocalPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 1f, 1f, 0.2f);
                return;
            }

            if (_isTrackingRight && !followRight)
            {
                // Log($"VR Drop with target at {SyncedValueNormalized}");
                Log($"VR stop tracking right");
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
                // SyncedIsBeingManipulated = true;
                Vector3 globalFingerPos = _inRightTrigger ? RightFingerPos() : LeftFingerPos();
                // var localFingerPos = transform.InverseTransformPoint(globalFingerPos);

                foreach (var baseKineticControl in controlBehaviours)
                {
                    baseKineticControl.OnMoveHandle(globalFingerPos);
                }
                // SyncedValueNormalized = PosToNormalized(globalFingerPos);

                // if (synced)
                // {
                //     RequestSerialization();
                // }

                // OnDeserialization();
            }
        }


        private void OnLeftContactEnter(ContactSenderProxy leftSender)
        {
            if (!IsAuthorized) return;
            // _leftSender = leftSender;
            Log($"OnLeftContactEnter received {leftSender.usage}");
            Log($"Left Contact Enter");

            if (!IsInVR)
            {
                Log("not in vr");
                return;
            }

            if (IsInVR && !UseContactsInVR)
            {
                Log("not using contacts");
                return;
            }

            if (!_inLeftTrigger)
            {
                Log($"VR contact left");
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

            foreach (var baseKineticControl in controlBehaviours)
            {
                baseKineticControl.TakeOwnership();
            }
            // TakeOwnership();

            _inLeftTrigger = true;
            //_localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 1f, 1f, 0.2f);
        }

        private void OnRightContactEnter(ContactSenderProxy rightSender)
        {
            if (!IsAuthorized) return;
            // _rightSender = rightSender;
            Log($"OnRightContactEnter received {rightSender.usage}");
            Log($"Right Contact Enter");

            if (!IsInVR)
            {
                Log("not in vr");
                return;
            }

            if (IsInVR && !UseContactsInVR)
            {
                Log("not using contacts");
                return;
            }

            if (!_inRightTrigger)
            {
                Log($"VR contact right");
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

            // TakeOwnership();
            foreach (var baseKineticControl in controlBehaviours)
            {
                baseKineticControl.TakeOwnership();
            }

            _inRightTrigger = true;
            //_localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 1f, 1f, 0.2f);
        }

        private void OnLeftContactExit()
        {
            if (!IsAuthorized) return;
            Log($"Left Contact Exit");
            _inLeftTrigger = false;
            _leftSender = null;
            ResetTransform();
            // foreach (var baseKineticControl in _controlBehaviour)
            // {
            //     baseKineticControl.UpdateHandlePosition();
            // }
            // _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 1f, 1f, 0.2f);
        }

        private void OnRightContactExit()
        {
            if (!IsAuthorized) return;
            Log($"Right Contact Exit");
            _inRightTrigger = false;
            _rightSender = null;
            ResetTransform();
            // foreach (var baseKineticControl in _controlBehaviour)
            // {
            //     baseKineticControl.UpdateHandlePosition();
            // }
            // _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 1f, 1f, 0.2f);
        }

        public void ResetTransformIfNotManipulated()
        {
            if (!_pickupHasObjectSync && !_isHeldLocally)
            {
                ResetTransform();
            }
        }

        public void ResetTransform()
        {
            FreezeRigidBody();
            // if (RigidBody)
            // {
            //     RigidBody.angularVelocity = Vector3.zero;
            //     RigidBody.velocity = Vector3.zero;
            // }

            if (Utilities.IsValid(resetTransform))
            {
                // parentConstraint.GlobalWeight = 1;
                transform.SetPositionAndRotation(
                    resetTransform.position,
                    resetTransform.rotation
                );
            }
            else
            {
                LogWarning($"handle reset is not valid on {name}");
            }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
            // if (Utilities.IsValid(RigidBody))
            // {
            //     RigidBody.MarkDirty();
            // }

            transform.MarkDirty();
#endif
        }

        public void FreezeRigidBody()
        {
            if (Utilities.IsValid(rigidBody))
            {
                rigidBody.velocity = Vector3.zero;
            }
            else
            {
                LogError($"Rigid body is not valid");
            }
        }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public void Register(BaseKineticControl baseKineticControl)
        {
            Log($"registering {baseKineticControl}");
            controlBehaviours = controlBehaviours.AddUnique(baseKineticControl);
        }
        

        internal void SetupPickup()
        {
            Log("SetupPickup");
            pickup = GetComponent<VRC_Pickup>();
            Log($"pickup is {pickup}");
            if (Utilities.IsValid(pickup))
            {
                _pickupHasObjectSync = pickup.GetComponent<VRCObjectSync>() != null ||
                                       pickup.GetComponent("MMMaellon.SmartObjectSync") != null;
            }
            else
            {
                LogError($"no pickup found");
            }
        }

        internal void SetupPickupRigidbody()
        {
            Log("SetupPickupRigidbody");
            if (Utilities.IsValid(pickup))
            {
                rigidBody = pickup.GetComponent<Rigidbody>();
                rigidBody.useGravity = false;
                rigidBody.isKinematic = false;
                rigidBody.drag = 10f;
                rigidBody.angularDrag = 5f;
                rigidBody.MarkDirty();
            }
            else
            {
                LogError("no pickup found");
            }
        }
#endif
    }
}