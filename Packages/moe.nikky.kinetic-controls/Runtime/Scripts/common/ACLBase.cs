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
#if UNITY_EDITOR && !COMPILER_UDONSHARP
    // [RequireComponent(typeof(PreProcessEditorHelper))]
#endif
    public abstract class ACLBase : Logging
    {
        
        protected abstract bool EnforceACL
        {
            get;
            set;
        }

        protected abstract AccessControl AccessControl
        {
            get ;
            set ;
        }

        protected abstract GameObject BoolAuthorizedDrivers
        {
            get;
            set;
        }
        
        // [Header("Access Control")]
        // [SerializeField] 
        // [ReadOnly]
        // protected BoolDriver[] authorizedDrivers = { };

        protected abstract BoolDriver[] AuthorizedDrivers
        {
            get;
            set;
        }

        // [SerializeField] //
        // [Tooltip("object containing bool drivers, will be updated with current auth status")]
        // //
        // [FormerlySerializedAs("boolAuthorizedDrivers")] //
        // private Transform boolAuthorizedDriversTransform;
        

        // [SerializeField] private bool editorIsAuthorized = false;

        private bool _isAuthorized = false;
        protected bool IsAuthorized => _isAuthorized;

        protected override void _Init()
        {
            base._Init();

            // FindBoolAuthDrivers();

            // Log($"queueing up LateInitACL");
            SendCustomEventDelayedFrames(nameof(_PostInitACL), 1);
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

                Log($"setting isAuthorized to {IsAuthorized} for {LocalPlayerName}");

                Log($"updating {AuthorizedDrivers.Length} drivers");
                for (var i = 0; i < AuthorizedDrivers.Length; i++)
                {
                    AuthorizedDrivers[i].OnUpdateBool(IsAuthorized);
                }

                AccessChanged();
            }
        }
        //
        // private VRCPlayerApi _localPlayer;
        // protected VRCPlayerApi LocalPlayer => _localPlayer;
        // private bool _isInVR;
        // protected bool IsInVR => _isInVR;
        // private string _localName = "???";
        // public override void OnPlayerJoined(VRCPlayerApi player)
        // {
        //     base.OnPlayerJoined(player);
        //     if (player == Networking.LocalPlayer)
        //     {
        //         _localPlayer = player;
        //         _localName = player.displayName;
        //         _isInVR = player.IsUserInVR();
        //     }
        // }

        protected abstract void AccessChanged();

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        internal void FindBoolAuthDrivers()
        {
            if (Utilities.IsValid(BoolAuthorizedDrivers))
            {
                // Log($"loading auth drivers");
                AuthorizedDrivers = BoolAuthorizedDrivers.GetComponentsInChildren<BoolDriver>();
                // Log($"found {AuthorizedDrivers.Length} auth bool drivers");
            }
        }
#endif

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


        public AccessControl EditorACL
        {
            get => AccessControl;
            set
            {
                if (AccessControl != value)
                {
                    this.MarkDirty();
                }

                AccessControl = value;
            }
        }

        public bool EditorEnforceACL
        {
            get => EnforceACL;
            set
            {
                if (EnforceACL != value)
                {
                    this.MarkDirty();
                }
                // Log($"Setting EnforceACL to {value} on {name}");
                EnforceACL = value;

            }
        }

        public GameObject EditorBoolAuthorizedDrivers
        {
            get => BoolAuthorizedDrivers;
            set
            {
                if (BoolAuthorizedDrivers != value)
                {
                    this.MarkDirty();
                }
                // Log($"Setting BoolAuthorizedDrivers to {value} on {name}");
                BoolAuthorizedDrivers = value;
            }
        }

#endif
    }
}