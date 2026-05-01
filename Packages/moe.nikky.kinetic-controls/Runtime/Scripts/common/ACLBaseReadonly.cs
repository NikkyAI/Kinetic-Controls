#define READONLY

using System;
using System.ComponentModel;
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
    public abstract class ACLBaseReadonly : ACLBase
    {
        [Header("Logging")] // header
        [ReadOnly]
        private DebugLog debugLog;

        protected override DebugLog DebugLog
        {
            get => debugLog;
            set => debugLog = value;
        }

        [Header("Access Control")] // header
        [SerializeField]
        [ReadOnly]
        private bool enforceACL = true;

        protected override bool EnforceACL
        {
            get => enforceACL;
            set => enforceACL = value;
        }

        [Tooltip("ACL used to check who can use the toggle")] //
        [SerializeField]
        [ReadOnly]
        private AccessControl accessControl;

        protected override AccessControl AccessControl
        {
            get => accessControl;
            set => accessControl = value;
        }

        [SerializeField] //
        [Tooltip("object containing bool drivers, drivers will be updated with current auth status")]
        [ReadOnly]
        private GameObject boolAuthorizedDrivers;

        protected override GameObject BoolAuthorizedDrivers
        {
            get => boolAuthorizedDrivers;
            set => boolAuthorizedDrivers = value;
        }
        
        [SerializeField] 
        [ReadOnly]
        [NonReorderable]
        protected BoolDriver[] authorizedDrivers = { };

        protected override BoolDriver[] AuthorizedDrivers
        {
            get => authorizedDrivers;
            set => authorizedDrivers = value;
        }
    }
}