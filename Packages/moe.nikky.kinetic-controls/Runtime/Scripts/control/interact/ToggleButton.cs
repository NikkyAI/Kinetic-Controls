using System.ComponentModel;
using JetBrains.Annotations;
using nikkyai.common;
using nikkyai.extensions;
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
        [SerializeField, UdonSynced]
        private bool synced = true;

        [Header("Toggle - MIDI - Requires VRC_MidiListener Component")] //
        [SerializeField, Description("Requires a VRC MIDI Listened with NoteOn enabled")]
        protected bool midiEnabled = true;
        [SerializeField, Range(0,15)]
        protected int midiChannel = 0;
        [SerializeField, Range(0,127)]
        protected int midiNumber = 0;
        [SerializeField, Range(0,127)]
        protected int midiMinVelocity = 127;

        [FormerlySerializedAs("boolStateDriversObj")]
        [Header("Drivers")] // header
        [SerializeField]
        private GameObject boolStateDrivers;

        // [FormerlySerializedAs("isAuthorizedIndicator")] // 
        // [SerializeField]
        // private Transform boolAuthorizedDrivers;

        protected override string LogPrefix => nameof(ToggleButton);

        public override bool Synced
        {
            get => synced;
            set
            {
                if (!IsAuthorized) return;

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

            // _valueBoolDrivers = _valueBoolDrivers.AddRange(gameObject.GetComponents<BoolDriver>());
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
            Log($"AccessChanged: {IsAuthorized}");
            DisableInteractive = !IsAuthorized;
        }

        [UsedImplicitly]
        public void SetState(bool newValue)
        {
            if (!IsAuthorized) return;

            SyncedState = newValue;

            if (synced)
            {
                TakeOwnership();
                RequestSerialization();
            }

            OnDeserialization();
        }

        public void Reset()
        {
            if (!IsAuthorized) return;
            SyncedState = defaultValue;
            if (synced)
            {
                TakeOwnership();
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
            if (!IsAuthorized) return;

            SyncedState = !SyncedState;
            // _UpdateState();
            if (synced)
            {
                TakeOwnership();
                RequestSerialization();
            }
            // OnDeserialization();
        }

        // private void _UpdateState()
        // {
        //     Log($"_UpdateState {_syncedState}");
        //     
        //     for (var i = 0; i < _valueBoolDrivers.Length; i++)
        //     {
        //         _valueBoolDrivers[i].OnUpdateBool(_syncedState);
        //     }
        // }

        public override void OnDeserialization()
        {
            // _UpdateState();
        }
        
        public override void MidiNoteOn(int channel, int number, int velocity)
        {
            if (!IsAuthorized) return;
            base.MidiNoteOn(channel, number, velocity);
            if (!midiEnabled) return;
            
            Log($"MidiNoteOn({channel}, {number}, {velocity})");
            if (channel == midiChannel && number == midiNumber && velocity >= midiMinVelocity)
            {
                Log("midi triggered");
                _Interact();
            }
        }
        
#if UNITY_EDITOR && !COMPILER_UDONSHARP

        // protected override void OnValidate()
        // {
        //     base.OnValidate();
        //     if (boolStateDrivers == null && Utilities.IsValid(boolStateDriversTransform))
        //     {
        //         boolStateDrivers = boolStateDriversTransform.gameObject;
        //         this.MarkDirty();
        //     }
        // }

        [ContextMenu("Assign Defaults")]
        public void AssignDefaults()
        {
            if (Application.isPlaying) return;
            // UnityEditor.EditorUtility.SetDirty(this);

            var candidates = gameObject.GetComponentsInChildren<Transform>();
            if (boolStateDrivers == null)
            {
                foreach (var candidate in candidates)
                {
                    if (candidate.name == "Bool State Drivers")
                    {
                        boolStateDrivers = candidate.gameObject;
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