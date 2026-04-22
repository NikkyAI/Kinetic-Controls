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

        [SerializeField] //
        [Description("object containing bool drivers, drivers will be updated with current auth status")]
        //
        [FormerlySerializedAs("boolAuthorizedDriversObj")] //
        private GameObject boolAuthorizedDrivers;

        // [SerializeField] //
        // [Description("object containing bool drivers, will be updated with current auth status")]
        // //
        // [FormerlySerializedAs("boolAuthorizedDrivers")] //
        // private Transform boolAuthorizedDriversTransform;
        

        // [SerializeField] private bool editorIsAuthorized = false;

        private bool _isAuthorized = false;
        protected bool IsAuthorized => _isAuthorized;

        protected override void _Init()
        {
            base._Init();

            FindBoolAuthDrivers();

            // Log($"queueing up LateInitACL");
            SendCustomEventDelayedFrames(nameof(_PostInitACL), 1);
        }

        private void FindBoolAuthDrivers()
        {
            if (Utilities.IsValid(boolAuthorizedDrivers))
            {
                Log($"loading auth drivers");
                IsAuthorizedBoolDrivers = boolAuthorizedDrivers.GetComponentsInChildren<BoolDriver>();
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
                    AccessControl._Register(AccessControl.EVENT_VALIDATE, this, nameof(_TXL_ACL_OnValidate));
                    AccessControl._Register(AccessControl.EVENT_ENFORCE_UPDATE, this, nameof(_TXL_ACL_OnValidate));

                    _TXL_ACL_OnValidate();
                }
                else
                {
                    LogError($"No ACL set on {name}");
                    _isAuthorized = false;
                    AccessChanged();
                }
            }
            else
            {
                Log("not using ACL, setting isAuthorized to true");
                _isAuthorized = true;
                AccessChanged();
            }
        }

        public void _TXL_ACL_OnValidate()
        {
            bool oldAuth = IsAuthorized;
            _isAuthorized = AccessControl._LocalHasAccess();
            if (IsAuthorized != oldAuth)
            {
                // TODO: move to Base class to reduce lookups
                // var localPlayer = Networking.LocalPlayer;
                // var localName = "???";
                // if (Utilities.IsValid(localPlayer))
                // {
                //     localName = localPlayer.displayName;
                // }

                Log($"setting isAuthorized to {IsAuthorized} for {_localName}");

                Log($"updating {IsAuthorizedBoolDrivers.Length} drivers");
                for (var i = 0; i < IsAuthorizedBoolDrivers.Length; i++)
                {
                    IsAuthorizedBoolDrivers[i].OnUpdateBool(IsAuthorized);
                }

                AccessChanged();
            }
        }
        
        private string _localName = "???";
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            base.OnPlayerJoined(player);
            if (player == Networking.LocalPlayer)
            {
                _localName = player.displayName;
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

        // protected override void OnValidate()
        // {
        //     base.OnValidate();
        //     if (Utilities.IsValid(boolAuthorizedDriversTransform))
        //     {
        //         boolAuthorizedDrivers = boolAuthorizedDriversTransform.gameObject;
        //         this.MarkDirty();
        //     }
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