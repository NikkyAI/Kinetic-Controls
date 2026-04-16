using JetBrains.Annotations;
using nikkyai.ArrayExtensions;
using nikkyai.common;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC;
using VRC.SDKBase;

namespace nikkyai.control.interact
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ToggleButton : BaseSyncedControl
    {
        [Header("Toggle")] // header
        [Tooltip(
             "The button will initialize into this value, toggle this for elements that should be enabled by default"),
         SerializeField]
        private bool defaultValue = false;

        [Header("State")] // header
        [SerializeField, UdonSynced]
        private bool synced = true;

        [Header("Drivers")] // header
        [FormerlySerializedAs("boolStatedDrivers")] //
        [FormerlySerializedAs("valueIndicator")] //
        [SerializeField]
        private Transform boolStateDrivers;

        // [FormerlySerializedAs("isAuthorizedIndicator")] // 
        // [SerializeField]
        // private Transform boolAuthorizedDrivers;

        protected override string LogPrefix => nameof(ToggleButton) + " " + name;

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
                Log($"set value to {_syncedState} => {prevValue}");
                _syncedState = prevValue;

                RequestSerialization();
            }
        }

        [UdonSynced, FieldChangeCallback(nameof(SyncedState))]
        private bool _syncedState = false;

        public bool SyncedState
        {
            get => _syncedState;
            private set
            {
                _syncedState = value;

                Log($"SyncedState set to {_syncedState}");

                for (var i = 0; i < _valueBoolDrivers.Length; i++)
                {
                    _valueBoolDrivers[i].OnUpdateBool(_syncedState);
                }
            }
        }

        // public const int EVENT_UPDATE = 0;
        // public const int EVENT_COUNT = 1;
        //
        // protected override int EventCount => EVENT_COUNT;

        // private BoolDriver[] _boolDrivers = { };

        private BoolDriver[] _valueBoolDrivers = { };

        void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            base._Init();
            Log("Initializing");
            DisableInteractive = true;

            _valueBoolDrivers = _valueBoolDrivers.AddRange(transform.GetComponents<BoolDriver>());
            if (Utilities.IsValid(boolStateDrivers))
            {
                Log($"loading bool drivers");
                _valueBoolDrivers = _valueBoolDrivers.AddRange(
                    boolStateDrivers.GetComponentsInChildren<BoolDriver>()
                );
            }
            Log($"found {_valueBoolDrivers.Length} bool drivers");
            

            Log($"setting default value {defaultValue}");
            SyncedState = defaultValue;

            OnDeserialization();
        }

        protected override void AccessChanged()
        {
            Log($"AccessChanged: {isAuthorized}");
            DisableInteractive = !isAuthorized;
        }

        [UsedImplicitly]
        public void SetState(bool newValue)
        {
            if (!isAuthorized) return;
            TakeOwnership();

            SyncedState = newValue;

            if (synced)
            {
                RequestSerialization();
            }

            OnDeserialization();
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
            Log("OnInteract");
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
            // OnDeserialization();
        }

        private void _UpdateState()
        {
            Log($"_UpdateState {_syncedState}");
            
            for (var i = 0; i < _valueBoolDrivers.Length; i++)
            {
                _valueBoolDrivers[i].OnUpdateBool(_syncedState);
            }
        }

        public override void OnDeserialization()
        {
            // _UpdateState();
        }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        [ContextMenu("Assign Defaults")]
        public void AssignDefaults()
        {
            if (Application.isPlaying) return;
            // UnityEditor.EditorUtility.SetDirty(this);

            var candidates = transform.GetComponentsInChildren<Transform>();
            if (boolStateDrivers == null)
            {
                foreach (var candidate in candidates)
                {
                    if (candidate.name == "Bool State Drivers")
                    {
                        boolStateDrivers = candidate;
                        Log("Found and assigned Bool State Drivers");
                        UnityEditor.EditorUtility.SetDirty(this);
                        break;
                    }
                }
            }

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