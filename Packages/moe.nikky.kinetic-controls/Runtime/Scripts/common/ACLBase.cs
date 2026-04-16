using System;
using System.ComponentModel;
using Texel;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using VRC;
using VRC.SDKBase;
using VRC.Udon.Common.Enums;

namespace nikkyai.common
{
    public abstract class ACLBase : LoggerBase
    {
        protected BoolDriver[] IsAuthorizedBoolDrivers = { };

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

        [Tooltip("ACL used to check who can use the toggle")] //
        [SerializeField]
        private AccessControl accessControl;

        protected AccessControl AccessControl
        {
            get => accessControl;
            private set => accessControl = value;
        }

        [FormerlySerializedAs("boolAuthorizedDrivers")]
        [SerializeField] //
        [Description("object containing bool drivers, will be updated with current auth status")]
        [InspectorName("boolAuthorizedDrivers")]
        private Transform boolAuthorizedDriversTransform;

        // [SerializeField] private bool editorIsAuthorized = false;

        protected bool isAuthorized = false;

        protected override void _Init()
        {
            base._Init();

            FindBoolAuthDrivers();

            // Log($"queueing up LateInitACL");
            SendCustomEventDelayedFrames(nameof(_PostInitACL), 1);
        }

        private void FindBoolAuthDrivers()
        {
            if (Utilities.IsValid(boolAuthorizedDriversTransform))
            {
                Log($"loading auth drivers");
                IsAuthorizedBoolDrivers = boolAuthorizedDriversTransform.GetComponentsInChildren<BoolDriver>();
                Log($"found {IsAuthorizedBoolDrivers.Length} auth bool drivers");
            }
        }

        public void _PostInitACL()
        {
            if (EnforceACL)
            {
                if (AccessControl)
                {
                    // Log($"registering events on {AccessControl}");
                    AccessControl._Register(AccessControl.EVENT_VALIDATE, this, nameof(_OnValidate));
                    AccessControl._Register(AccessControl.EVENT_ENFORCE_UPDATE, this, nameof(_OnValidate));

                    _OnValidate();
                }
                else
                {
                    LogError($"No ACL set on {name}");
                    isAuthorized = false;
                    AccessChanged();
                }
            }
            else
            {
                Log("not using ACL, setting isAuthorized to true");
                isAuthorized = true;
                AccessChanged();
            }
        }

        public void _OnValidate()
        {
            bool oldAuth = isAuthorized;
            isAuthorized = AccessControl._LocalHasAccess();
            if (isAuthorized != oldAuth)
            {
                var localPlayer = Networking.LocalPlayer;
                var localName = "???";
                if (Utilities.IsValid(localPlayer))
                {
                    localName = localPlayer.displayName;
                }

                Log($"setting isAuthorized to {isAuthorized} for {localName}");

                Log($"updating {IsAuthorizedBoolDrivers.Length} drivers");
                for (var i = 0; i < IsAuthorizedBoolDrivers.Length; i++)
                {
                    IsAuthorizedBoolDrivers[i].OnUpdateBool(isAuthorized);
                }

                AccessChanged();
            }
        }

        protected abstract void AccessChanged();

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        // protected override int ValidationHash =>
        //     HashCode.Combine(base.ValidationHash, AccessControl, boolAuthorizedDriversTransform, editorIsAuthorized);
        //
        // public override void OnValidateApplyValues()
        // {
        //     if (Application.isPlaying) return;
        //     base.OnValidateApplyValues();
        //
        //     FindBoolAuthDrivers();
        //     Log($"updating {IsAuthorizedBoolDrivers.Length} drivers");
        //     for (var i = 0; i < IsAuthorizedBoolDrivers.Length; i++)
        //     {
        //         IsAuthorizedBoolDrivers[i].UpdateBool(editorIsAuthorized);
        //     }
        //     
        // }
        public virtual AccessControl EditorACL
        {
            get => AccessControl;
            set
            {
                // if (value != null)
                // {
                //     Log($"Setting AccessControl to {value} on {name}");
                // }
                // else
                // {
                //     Log($"Setting AccessControl to null on {name}");
                // }

                if (AccessControl != value)
                {
                    EditorUtility.SetDirty(this);
                }

                AccessControl = value;
            }
        }

        public virtual bool EditorEnforceACL
        {
            get => EnforceACL;
            set
            {
                // Log($"Setting EnforceACL to {value} on {name}");
                EnforceACL = value;

                if (EnforceACL != value)
                {
                    EditorUtility.SetDirty(this);
                }
            }
        }

#endif
    }
}