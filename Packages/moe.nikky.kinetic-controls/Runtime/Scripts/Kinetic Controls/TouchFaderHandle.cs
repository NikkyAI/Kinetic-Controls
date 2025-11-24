using nikkyai.common;
using Texel;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace nikkyai.Kinetic_Controls
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class TouchFaderHandle : ACLBase
    {
        [SerializeField] internal TouchFader touchFader;

        public Collider leftHandCollider;
        public Collider rightHandCollider;
        #region ACL
        [Header("Access Control")] // header
        public bool enforceACL;

        protected override bool EnforceACL
        {
            get => enforceACL;
            set => enforceACL = value;
        }

        [Tooltip("ACL used to check who can use the toggle")] [SerializeField]
        public AccessControl accessControl;

        protected override AccessControl AccessControl
        {
            get => accessControl;
            set => accessControl = value;
        }

        #endregion

        #region Debug
        
        [Header("Debug")] // header
        [SerializeField]
        private DebugLog debugLog;

        protected override DebugLog DebugLog
        {
            get => debugLog;
            set => debugLog = value;
        }
        protected override string LogPrefix => nameof(TouchFaderHandle);

        #endregion

        private VRCPlayerApi _localPlayer;
        private bool _inLeftTrigger;
        private bool _inRightTrigger;

        void Start()
        {
            _EnsureInit();
        }

        private bool _isInVR;
        protected override void _Init()
        {
            _localPlayer = Networking.LocalPlayer;
            _isInVR = _localPlayer.IsUserInVR();
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
            touchFader._HandleInteract();
        }

        public override void InputUse(bool value, VRC.Udon.Common.UdonInputEventArgs args)
        {
            if (!_isInteracting) return;
            if (!isAuthorized) return;
            if (!value)
            {
                touchFader._HandleRelease();
            }
        }
        
        public void OnTriggerEnter(Collider other)
        {
            if (!isAuthorized) return;
            touchFader._OnTriggerEnter(other.GetInstanceID());
            // if (other == leftHandCollider && !_inLeftTrigger &&!_inRightTrigger)
            // {
            //     Log($"Left Trigger Enter");
            //     _inLeftTrigger = true;
            //     _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 1f, 1f, 0.2f);
            //                                           
            //     touchFader._OnCollisionStart();
            //
            //     // TakeOwnership();
            // }
            //
            // if (other == rightHandCollider && !_inRightTrigger&&!_inRightTrigger)
            // {
            //     Log($"Right Trigger Enter");
            //     _inRightTrigger = true;
            //     _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 1f, 1f, 0.2f);
            //                                            
            //     touchFader._OnCollisionStart();
            //         
            //     // TakeOwnership();
            // }
        }

        public void OnTriggerExit(Collider other)
        {
            if (!isAuthorized) return;
            touchFader._OnTriggerExit(other.GetInstanceID());
            
            // if (other == leftHandCollider && _inLeftTrigger)
            // {
            //     Log($"Left Trigger Exit");
            //     _inLeftTrigger = false;
            //     _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 1f, 1f, 0.2f);
            //     touchFader._OnCollisionEnd();
            // }
            //
            // if (other == rightHandCollider && _inRightTrigger)
            // {
            //     Log($"Right Trigger Exit");
            //     _inRightTrigger = false;
            //     _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 1f, 1f, 0.2f);
            //     touchFader._OnCollisionEnd();
            // }
        }
    }
}