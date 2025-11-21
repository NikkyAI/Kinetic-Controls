using nikkyai.toggle.common;
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
        protected override string LogPrefix => nameof(InteractCallback);

        #endregion

        // public const int EVENT_INTERACT = 0;
        // public const int EVENT_RELEASE = 1;
        // const int EVENT_COUNT = 2;
        // protected override int EventCount => EVENT_COUNT;

        void Start()
        {
            _EnsureInit();
        }

        private bool _isInVR;
        protected override void _Init()
        {
            _isInVR = Networking.LocalPlayer.IsUserInVR();
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
            touchFader._OnTriggerEnter(other.name);
        }

        public void OnTriggerExit(Collider other)
        {
            if (!isAuthorized) return;
            // touchFader.SetProgramVariable("other", other);
            touchFader._OnTriggerExit(other.name);
        }
    }
}