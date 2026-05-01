#define READONLY

using System;
using System.Runtime.CompilerServices;
using nikkyai.attribute;
using nikkyai.common;
using nikkyai.extensions;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC;
using VRC.Dynamics;
using VRC.SDK3.Components;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace nikkyai.control.kinetic
{
    [RequireComponent(typeof(VRCContactReceiver))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class HandleContact : HandleAbstract
    {
        [SerializeField]
#if READONLY
        [ReadOnly]
#endif
        private VRCContactReceiver receiver;
        
        private ContactSenderProxy _leftSender, _rightSender;
        
        // private bool _isHeldLocally = false;
        private bool _leftGrabbed = false, _rightGrabbed = false;

        // public bool PickupHasObjectSync => _pickupHasObjectSync;
        private bool _inLeftTrigger = false, _inRightTrigger = false;

        private bool _isTrackingLeft = false, _isTrackingRight = false;
        private bool _isRunningContactLoop = false;
        protected override string LogPrefix => nameof(HandleContact);
    
        void Start()
        {
            _EnsureInit();
        }

        protected override void AccessChanged()
        {
            base.AccessChanged();

            // not exposed to udon
            // receiver.contentTypes = IsAuthorized ? DynamicsUsageFlags.Avatar : DynamicsUsageFlags.Nothing;
        }

        public override void ResetTransformIfNotManipulated()
        {
            ResetTransform();
        }
        
        public override void InputGrab(bool value, UdonInputEventArgs args)
        {
            if (!IsInVR) return;

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

        // public void OnTriggerEnter(Collider other)
        // {
        //     if (!IsAuthorized) return;
        //     // touchFader._OnTriggerEnter(other.GetInstanceID());
        // }
        //
        // public void OnTriggerExit(Collider other)
        // {
        //     if (!IsAuthorized) return;
        //     // touchFader._OnTriggerExit(other.GetInstanceID());
        // }

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
        
        
        
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        internal override void Setup()
        {
            base.Setup();
            SetupContactSReceiver();
        }

        private void SetupContactSReceiver()
        {
            receiver = GetComponent<VRCContactReceiver>();
        }
#endif
    }
}
