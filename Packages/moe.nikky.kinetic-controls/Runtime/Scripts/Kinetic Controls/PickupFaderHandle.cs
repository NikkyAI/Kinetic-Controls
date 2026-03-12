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
    public class PickupFaderHandle : ACLBase
    {
        [SerializeField] internal PickupFader pickupFader;

        protected override string LogPrefix => $"{nameof(PickupFaderHandle)} {pickupFader}";

        private VRCPlayerApi _localPlayer;

        void Start()
        {
            _EnsureInit();
        }

        private bool _isInVR;
        private VRC_Pickup _pickup;

        protected override void _Init()
        {
            _localPlayer = Networking.LocalPlayer;
            _isInVR = _localPlayer.IsUserInVR();
            _pickup = GetComponent<VRC_Pickup>();
        }

        protected override void AccessChanged()
        {
            DisableInteractive = !isAuthorized;
        }

        private bool _isInteracting = false;

        // public override void Interact()
        // {
        //     if (!isAuthorized) return;
        //     _isInteracting = true;
        //     pickupFader._HandleInteract();
        // }

        // public override void InputUse(bool value, VRC.Udon.Common.UdonInputEventArgs args)
        // {
        //     if (!_isInteracting) return;
        //     if (!isAuthorized) return;
        //     if (!value)
        //     {
        //         pickupFader._HandleRelease();
        //     }
        // }
        
        
        public override void OnPickup()
        {
            if (!isAuthorized)
                return;

            pickupFader._OnPickup();
        }

        public override void OnDrop()
        {
            if (!isAuthorized)
                return;

            pickupFader._OnDrop();
        }
    }
}