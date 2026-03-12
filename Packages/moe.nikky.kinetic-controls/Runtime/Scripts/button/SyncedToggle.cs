using System;
using nikkyai.common;
using nikkyai.driver;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC;
using VRC.SDKBase;

namespace nikkyai.button
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SyncedToggle : BaseSyncedBehaviour
    {
        [Tooltip(
             "The button will initialize into this value, toggle this for elements that should be enabled by default"),
         SerializeField]
        private bool defaultValue = false;

        // [Header("UI")] // header
        // [SerializeField]
        // private string label;
        // [SerializeField]
        // private string label2;

        
        [Header("Drivers")] // header
        [SerializeField] private Transform valueIndicator;

        [SerializeField] private Transform isAuthorizedIndicator;

        protected override string LogPrefix => $"{nameof(SyncedToggle)} {name}";

        [Header("State")] // header
        [SerializeField, UdonSynced]
        private bool synced = true;

        public override bool Synced
        {
            get => synced;
            set
            {
                if (!isAuthorized) return;

                var prevValue = _syncedState;
                TakeOwnership();
                Log($"set synced to {value}");
                synced = value;
                Log($"set normalized to {_syncedState} => {prevValue}");
                _syncedState = prevValue;
                
                RequestSerialization();
            }
        }

        [UdonSynced, FieldChangeCallback(nameof(SyncedState))]
        private bool _syncedState = false;

        public bool SyncedState
        {
            get => _syncedState;
            set {
                _syncedState = value;
                
                Log($"SyncedState set to {_syncedState}");
            
                for (var i = 0; i < _valueBoolDrivers.Length; i++)
                {
                    _valueBoolDrivers[i].UpdateBool(_syncedState);
                }
            }
        }
        
        public const int EVENT_UPDATE = 0;
        public const int EVENT_COUNT = 1;

        protected override int EventCount => EVENT_COUNT;

        // private BoolDriver[] _boolDrivers = { };
        
        private BoolDriver[] _valueBoolDrivers = { };
        private BoolDriver[] _isAuthorizedBoolDrivers = { };
        void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            DisableInteractive = true;

            _valueBoolDrivers = valueIndicator.GetComponentsInChildren<BoolDriver>();
            Log($"found {_valueBoolDrivers.Length} bool drivers");
            if (isAuthorizedIndicator)
            {
                _isAuthorizedBoolDrivers = isAuthorizedIndicator.GetComponentsInChildren<BoolDriver>();
                Log($"found {_isAuthorizedBoolDrivers.Length} auth indicator bool drivers");
            }

            Log($"setting default value {defaultValue}");
            SyncedState = defaultValue;
            
            OnDeserialization();
        }

        protected override void AccessChanged()
        {
            DisableInteractive = !isAuthorized;
            
            for (var i = 0; i < _isAuthorizedBoolDrivers.Length; i++)
            {
                _isAuthorizedBoolDrivers[i].UpdateBool(isAuthorized);
            }
            // _UpdateState();
        }

        public void SetState(bool newValue)
        {
            if (!isAuthorized) return;
            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }

            SyncedState = newValue;

            if (synced)
            {
                RequestSerialization();
            }
            // OnDeserialization();
        }

        public void Reset()
        {
            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }

            _syncedState = defaultValue;
            if (synced)
            {
                RequestSerialization();
            }
            // OnDeserialization();
        }

        public override void Interact()
        {
            _Interact();
        }

        public void _Interact()
        {
            if (!isAuthorized) return;

            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }

            SyncedState = !SyncedState;
            // _UpdateState();
            if (synced)
            {
                RequestSerialization();
            }
        }

        // private void _UpdateState()
        // {
        //     Log($"_UpdateState {_syncedState}");
        //     
        //     for (var i = 0; i < _valueBoolDrivers.Length; i++)
        //     {
        //         _valueBoolDrivers[i].UpdateBool(_syncedState);
        //     }
        // }

        public override void OnDeserialization()
        {
            // _UpdateState();
        }

        [NonSerialized] private string prevLabel;
        [NonSerialized] private string prevLabel2;
        [NonSerialized] private TextMeshPro prevTMPLabel;

        // [Header("Editor Only")] // header
        // [SerializeField] private TMP_FontAsset fontAsset;
#if UNITY_EDITOR && !COMPILER_UDONSHARP

        private void OnValidate()
        {
            if (Application.isPlaying) return;
            UnityEditor.EditorUtility.SetDirty(this);

            // TODO: check on localTransforms too
            // if (label != prevLabel || label2 != prevLabel2 || tmpLabel != prevTMPLabel)
            // {
            //     // To prevent trying to apply the theme to often, as without it every single change in the scene causes it to be applied
            //     prevLabel = label;
            //     prevLabel2 = label2;
            //     prevTMPLabel = tmpLabel;
            //
            //     ApplyValues();
            // }
        }

        [ContextMenu("Apply Values")]
        public void ApplyValues()
        {
            if (Application.isPlaying) return;
            UnityEditor.EditorUtility.SetDirty(this);

            // if (!string.IsNullOrEmpty(label))
            // {
            //     InteractionText = label;
            //     this.MarkDirty();
            //     // this.MarkDirty();
            //     if (tmpLabel != null)
            //     {
            //         var text = (label.Trim() + "\n" + label2.Trim()).Trim('\n', ' ');
            //         tmpLabel.text = text;
            //         tmpLabel.MarkDirty();
            //     }
            // }
        }

        [ContextMenu("Assign Defaults")]
        public void AssignDefaults()
        {
            if (Application.isPlaying) return;
            UnityEditor.EditorUtility.SetDirty(this);

            // if (button == null)
            // {
            //     button = gameObject;
            // }
            //
            // if (buttonCollider == null)
            // {
            //     buttonCollider = button.GetComponent<Collider>();
            // }

            this.MarkDirty();
        }
#endif
    }
}