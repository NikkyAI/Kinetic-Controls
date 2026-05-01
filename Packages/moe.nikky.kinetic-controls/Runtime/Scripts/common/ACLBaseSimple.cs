using System;
using System.ComponentModel;
using nikkyai.attribute;
using nikkyai.Editor;
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
    [RequireComponent(typeof(PreProcessEditorHelper))]
#endif
    public abstract class ACLBaseSimple : ACLBase
    {
        [Header("Logging")] // header
        [SerializeField]
        private DebugLog debugLog;

        protected override DebugLog DebugLog
        {
            get => debugLog;
            set => debugLog = value;
        }
        
        // protected AccessControl accessControl;
        // protected virtual AccessControl AccessControl { get; set; }
        // protected abstract bool EnforceACL { get; set; }

        [Header("Access Control")] // header
        [SerializeField]
        private bool enforceACL = true;

        protected override bool EnforceACL
        {
            get => enforceACL;
            set => enforceACL = value;
        }

        [Tooltip("ACL used to check who can use the toggle")] //
        [SerializeField]
        private AccessControl accessControl;

        protected override AccessControl AccessControl
        {
            get => accessControl;
            set => accessControl = value;
        }

        [SerializeField] //
        [Tooltip("object containing bool drivers, drivers will be updated with current auth status")]
        private GameObject boolAuthorizedDrivers;

        protected override GameObject BoolAuthorizedDrivers
        {
            get => boolAuthorizedDrivers;
            set => boolAuthorizedDrivers = value;
        }

        [SerializeField] 
        [attribute.ReadOnly]
        [NonReorderable]
        protected BoolDriver[] authorizedDrivers = { };

        protected override BoolDriver[] AuthorizedDrivers
        {
            get => authorizedDrivers;
            set => authorizedDrivers = value;
        }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public override bool OnPreprocess()
        {
            if (!base.OnPreprocess())
            {
                return false;
            }
            FindBoolAuthDrivers();

            return true;
        }
#endif
    }
}