using System;
using nikkyai.ArrayExtensions;
using nikkyai.common;
using Texel;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDKBase;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;

namespace nikkyai.Kinetic_Controls
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TouchFaderHandle : ACLBase
    {
        [FormerlySerializedAs("touchFader")]
        [SerializeField] internal TouchFaderWithHandle touchFaderWithHandle;

        protected override string LogPrefix => nameof(TouchFaderHandle);

        private VRCContactReceiver _receiver;
        private VRCPlayerApi _localPlayer;
        //private bool _inLeftTrigger;
        //private bool _inRightTrigger;

        void Start()
        {
            _EnsureInit();
        }

        private bool _isInVR;

        protected override void _Init()
        {
            _localPlayer = Networking.LocalPlayer;
            _isInVR = _localPlayer.IsUserInVR();
            _receiver = GetComponent<VRCContactReceiver>();
            DisableInteractive = _isInVR;
        }

        protected override void AccessChanged()
        {
            DisableInteractive = !isAuthorized || _isInVR;
        }

        private bool _isInteracting = false;

        public override void Interact()
        {
            if (!isAuthorized) return;
            _isInteracting = true;
            touchFaderWithHandle._HandleInteract();
        }

        public override void InputUse(bool value, VRC.Udon.Common.UdonInputEventArgs args)
        {
            if (!_isInteracting) return;
            if (!isAuthorized) return;
            if (!value)
            {
                touchFaderWithHandle._HandleRelease();
            }
        }

        [NonSerialized] public bool rightGrabbed;
        [NonSerialized] public bool leftGrabbed;

        public override void InputGrab(bool value, UdonInputEventArgs args)
        {
            if (!_isInVR) return;
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
                    if (!leftGrabbed)
                    {
                        // Log($"LeftGrabbed()");
                    }

                    leftGrabbed = true;
                }

                if (args.handType == HandType.RIGHT)
                {
                    if (!rightGrabbed)
                    {
                        // Log($"RightGrabbed()");
                    }

                    rightGrabbed = true;
                }
            }
            else
            {
                if (args.handType == HandType.LEFT)
                {
                    if (leftGrabbed)
                    {
                        // Log($"LeftReleased()");
                    }


                    leftGrabbed = false;
                }

                if (args.handType == HandType.RIGHT)
                {
                    if (rightGrabbed)
                    {
                        // Log($"RightReleased()");
                    }

                    rightGrabbed = false;
                }
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            if (!isAuthorized) return;
            // touchFader._OnTriggerEnter(other.GetInstanceID());
        }

        public void OnTriggerExit(Collider other)
        {
            if (!isAuthorized) return;
            // touchFader._OnTriggerExit(other.GetInstanceID());
        }

        
        // [NonSerialized] public ContactSenderProxy _leftSender, _rightSender;
        public override void OnContactEnter(ContactEnterInfo contactInfo)
        {
            if (!_isInVR) return;
            if (!contactInfo.contactSender.player.isLocal) return;
            if (!isAuthorized) return;

            Log($"Contact Enter {contactInfo.contactPoint} {contactInfo.matchingTags.Length}");
            for (var i = 0; i < contactInfo.matchingTags.Length; i++)
            {
                var matchingTag = contactInfo.matchingTags[i];
                Log(matchingTag);
            }

            if (contactInfo.matchingTags.Contains("FingerIndexL"))
            {
                touchFaderWithHandle.leftSender = contactInfo.contactSender;
                touchFaderWithHandle.OnLeftContactEnter();
                return;
            }

            if (contactInfo.matchingTags.Contains("FingerIndexR"))
            {
                touchFaderWithHandle.rightSender = contactInfo.contactSender;
                touchFaderWithHandle.OnRightContactEnter();
                return;
            }
        }

        public override void OnContactExit(ContactExitInfo contactInfo)
        {
            if (!_isInVR) return;
            if (!contactInfo.contactSender.player.isLocal) return;
            if (!isAuthorized) return;
            Log($"Contact Exit");

            if (contactInfo.contactSender == touchFaderWithHandle.leftSender)
            {
                Log($"Contact Exit Left");
                touchFaderWithHandle.leftSender = null;
                touchFaderWithHandle.OnLeftContactExit();
            }

            if (contactInfo.contactSender == touchFaderWithHandle.rightSender)
            {
                Log($"Contact Exit Right");
                touchFaderWithHandle.rightSender = null;
                touchFaderWithHandle.OnRightContactExit();
            }
        }
    }
}