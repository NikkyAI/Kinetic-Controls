using Texel;
using UnityEngine;
using VRC.SDKBase;

namespace nikkyai.common
{
    
    public abstract class ACLBase: LoggerBase
    {
        // protected AccessControl accessControl;
        // protected virtual AccessControl AccessControl { get; set; }
        // protected abstract bool EnforceACL { get; set; }
        
        [Header("Access Control")] // header
        [SerializeField]
        private bool enforceACL = true;

        protected bool EnforceACL
        {
            get => enforceACL;
            private set => enforceACL = value;
        }

        [Tooltip("ACL used to check who can use the toggle")] [SerializeField]
        private AccessControl accessControl;

        protected AccessControl AccessControl
        {
            get => accessControl;
            private set => accessControl = value;
        }
        
        protected bool isAuthorized;

        protected override void _PostInit()
        {
            base._PostInit();
            
            if (EnforceACL)
            {
                if (AccessControl)
                {
                    AccessControl._Register(AccessControl.EVENT_VALIDATE, this, nameof(_OnValidate));
                    AccessControl._Register(AccessControl.EVENT_ENFORCE_UPDATE, this, nameof(_OnValidate));

                    _OnValidate();
                }
                else
                {
                    LogError("No ACL set");
                    isAuthorized = false;
                    AccessChanged();
                }
            }
            else
            {
                isAuthorized = true;
                AccessChanged();
            }
        }

        public void _OnValidate()
        {
            bool oldAuth = this.isAuthorized;
            isAuthorized = AccessControl._LocalHasAccess();
            if (isAuthorized != oldAuth)
            {
                Log($"setting isAuthorized to {this.isAuthorized} for {Networking.LocalPlayer.displayName}");
                AccessChanged();
            }
        }

        protected abstract void AccessChanged();

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public virtual AccessControl EditorACL
        {
            get => AccessControl;
            set => AccessControl = value;
        }
        public virtual bool EditorEnforceACL
        {
            get => EnforceACL;
            set => EnforceACL = value;
        }
#endif
    }
}