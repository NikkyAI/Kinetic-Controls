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
    public class Handle : ACLBase
    {
        [FormerlySerializedAs("fader")] //
        [FormerlySerializedAs("touchFader")] //
        [SerializeField]
        //
        internal BaseKineticControl controlBehaviour;

        protected override string LogPrefix
        {
            get
            {
                if (Utilities.IsValid(controlBehaviour))
                {
                    return $"{nameof(Handle)} {controlBehaviour.name}";
                }
                else
                {
                    return $"{nameof(Handle)}";
                }
            }
        }

        // private VRCContactReceiver _receiver;
        // private VRC_Pickup _pickup;
        private VRCPlayerApi _localPlayer;

        void Start()
        {
            _EnsureInit();
        }

        private bool _isInVR;

        protected override void _Init()
        {
            _localPlayer = Networking.LocalPlayer;
            if (Utilities.IsValid(_localPlayer))
            {
                _isInVR = _localPlayer.IsUserInVR();
            }

            // _receiver = GetComponent<VRCContactReceiver>();
            // _pickup = GetComponent<VRC_Pickup>();
            DisableInteractive = _isInVR;
        }

        protected override void AccessChanged()
        {
            DisableInteractive = !isAuthorized || _isInVR;
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
                controlBehaviour.LeftSender = contactInfo.contactSender;
                controlBehaviour.OnLeftContactEnter();
                return;
            }

            if (contactInfo.matchingTags.Contains("FingerIndexR"))
            {
                controlBehaviour.RightSender = contactInfo.contactSender;
                controlBehaviour.OnRightContactEnter();
                return;
            }
        }

        public override void OnContactExit(ContactExitInfo contactInfo)
        {
            if (!_isInVR) return;
            if (!contactInfo.contactSender.player.isLocal) return;
            if (!isAuthorized) return;
            Log($"Contact Exit");

            if (contactInfo.contactSender == controlBehaviour.LeftSender)
            {
                Log($"Contact Exit Left");
                controlBehaviour.LeftSender = null;
                controlBehaviour.OnLeftContactExit();
            }

            if (contactInfo.contactSender == controlBehaviour.RightSender)
            {
                Log($"Contact Exit Right");
                controlBehaviour.RightSender = null;
                controlBehaviour.OnRightContactExit();
            }
        }


        public override void OnPickup()
        {
            if (!isAuthorized)
                return;

            if (Utilities.IsValid(controlBehaviour))
            {
                controlBehaviour._OnPickup();
            }
            else
            {
                LogError("OnPickup: controller not set");
            }
        }

        public override void OnDrop()
        {
            if (!isAuthorized)
                return;

            if (Utilities.IsValid(controlBehaviour))
            {
                controlBehaviour._OnDrop();
            }
            else
            {
                LogError("OnDrop: controller not set");
            }
        }
    }
}