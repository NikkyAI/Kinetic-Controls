using System;
using nikkyai.ArrayExtensions;
using nikkyai.common;
using UdonSharp;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace nikkyai.control.kinetic
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Handle : ACLBase
    {
        // internal BaseKineticControl controlBehaviour;
        [SerializeField] internal BaseKineticControl[] controlBehaviour = { };

        protected override string LogPrefix => $"{nameof(Handle)} @ {transform.parent.name}";

        [HideInInspector, FieldChangeCallback(nameof(UseContactsInVRLocal))]
        public bool useContactsInVRLocal = true;

        public bool UseContactsInVRLocal
        {
            get => useContactsInVRLocal;
            set
            {
                Log($"UseContactsInVR (vr: {_isInVR}) {useContactsInVRLocal} -> {value}");
                useContactsInVRLocal = value;

                if (!Utilities.IsValid(_pickup))
                {
                    _pickup = GetComponent<VRC_Pickup>();
                }

                if (_isInVR)
                {
                    if (useContactsInVRLocal)
                    {
                        // _pickup.pickupable = false;
                        _pickup.proximity = -1f;
                        _pickup.InteractionText = "error";
                        //todo: reference to contactsender to edit it
                        // _contactReceiver.contentTypes = DynamicsUsageFlags.Avatar;
                    }
                    else
                    {
                        //  _pickup.pickupable = true;
                        _pickup.proximity = 1f;
                        _pickup.InteractionText = "Grab to adjust";
                        //todo: reference to contactsender to edit it
                        //_contactReceiver.contentTypes = DynamicsUsageFlags.Nothing;
                    }
                }
                else
                {
                    // _pickup.pickupable = true;
                    _pickup.InteractionText = "Grab to adjust";
                    _pickup.proximity = 5f; // add serialized field to configure range
                    //todo: reference to contactsender to edit it
                    // _contactReceiver.contentTypes = DynamicsUsageFlags.Nothing;
                }

                AccessChanged();
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

        // [FormerlySerializedAs("_pickup")] //
        // [SerializeField]
        private VRC_Pickup _pickup;

        protected override void _Init()
        {
            base._Init();
            _localPlayer = Networking.LocalPlayer;
            if (Utilities.IsValid(_localPlayer))
            {
                _isInVR = _localPlayer.IsUserInVR();
            }

            AccessChanged();
        }

        protected override void AccessChanged()
        {
            // DisableInteractive = !isAuthorized || _isInVR;
            if (!Utilities.IsValid(_pickup))
            {
                _pickup = GetComponent<VRC_Pickup>();
            }

            _pickup.pickupable = isAuthorized && !_isInVR;
        }

        public void Register(BaseKineticControl baseKineticControl)
        {
            Log($"registering {baseKineticControl}");
            controlBehaviour = controlBehaviour.AddUnique(baseKineticControl);
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

        [NonSerialized] public bool RightGrabbed;
        [NonSerialized] public bool LeftGrabbed;

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
                    if (!LeftGrabbed)
                    {
                        // Log($"LeftGrabbed()");
                    }

                    LeftGrabbed = true;
                }

                if (args.handType == HandType.RIGHT)
                {
                    if (!RightGrabbed)
                    {
                        // Log($"RightGrabbed()");
                    }

                    RightGrabbed = true;
                }
            }
            else
            {
                if (args.handType == HandType.LEFT)
                {
                    if (LeftGrabbed)
                    {
                        // Log($"LeftReleased()");
                    }

                    LeftGrabbed = false;
                }

                if (args.handType == HandType.RIGHT)
                {
                    if (RightGrabbed)
                    {
                        // Log($"RightReleased()");
                    }

                    RightGrabbed = false;
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
                foreach (var cb in controlBehaviour)
                {
                    cb.LeftSender = contactInfo.contactSender;
                    cb.OnLeftContactEnter();
                }

                return;
            }

            if (contactInfo.matchingTags.Contains("FingerIndexR"))
            {
                foreach (var cb in controlBehaviour)
                {
                    cb.RightSender = contactInfo.contactSender;
                    cb.OnRightContactEnter();
                }

                return;
            }
        }

        public override void OnContactExit(ContactExitInfo contactInfo)
        {
            if (!_isInVR) return;
            if (!contactInfo.contactSender.player.isLocal) return;
            if (!isAuthorized) return;
            Log($"Contact Exit");

            foreach (var cb in controlBehaviour)
            {
                if (contactInfo.contactSender == cb.LeftSender)
                {
                    Log($"Contact Exit Left");
                    cb.LeftSender = null;
                    cb.OnLeftContactExit();
                }

                if (contactInfo.contactSender == cb.RightSender)
                {
                    Log($"Contact Exit Right");
                    cb.RightSender = null;
                    cb.OnRightContactExit();
                }
            }
        }

        public override void OnPickup()
        {
            Log("OnPickup");
            if (!isAuthorized)
            {
                _pickup.Drop();
                //resetting position

                for (var i = 0; i < controlBehaviour.Length; i++)
                {
                    var cb = controlBehaviour[i];
                    if (Utilities.IsValid(cb))
                    {
                        cb.UpdateHandlePosition();
                    }
                    else
                    {
                        LogError($"OnPickup: controller {i} invalid");
                        LogError($"invalid: {cb}");
                    }
                }

                return;
            }

            if (_isInVR && useContactsInVRLocal)
            {
                LogWarning("dropping pickup");
                _pickup.Drop();
                return;
            }

            for (var i = 0; i < controlBehaviour.Length; i++)
            {
                var cb = controlBehaviour[i];
                if (Utilities.IsValid(cb))
                {
                    cb._OnPickup();
                }
                else
                {
                    LogError($"OnPickup: controller {i} invalid");
                    LogError($"invalid: {cb}");
                }
            }
        }

        public override void OnDrop()
        {
            Log("OnDrop");
            if (!isAuthorized)
                return;

            for (var index = 0; index < controlBehaviour.Length; index++)
            {
                var cb = controlBehaviour[index];
                if (Utilities.IsValid(cb))
                {
                    cb._OnDrop();
                }
                else
                {
                    LogError($"OnDrop: controller {index} invalid");
                    LogError($"invalid: {cb}");
                }
            }
        }
    }
}