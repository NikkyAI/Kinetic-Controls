using System.ComponentModel;
using nikkyai.ArrayExtensions;
using nikkyai.common;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;

namespace nikkyai.control.interact
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TriggerButton : ACLBase
    {
        [Header("Trigger - MIDI - Requires VRC_MidiListener Component")] //
        [SerializeField, Description("Requires a VRC MIDI Listened with NoteOn enabled")]
        protected bool midiEnabled = true;
        [SerializeField, Range(0,15)]
        protected int midiChannel = 0;
        [SerializeField, Range(0,127)]
        protected int midiNumber = 0;
        [SerializeField, Range(0,127)]
        protected int midiMinVelocity = 127;

        [Header("Drivers")] // header
        [FormerlySerializedAs("triggerDriversObj")]
        [Description("default: self")]
        [SerializeField] private GameObject triggerDrivers;

        protected override string LogPrefix => $"{nameof(TriggerButton)} {name}";
    
        private TriggerDriver[] _triggerDrivers = { };

        void Start()
        {
            _EnsureInit();   
        }

        protected override void _Init()
        {
            base._Init();
            // if (triggerDrivers == null)
            // {
            //     triggerDrivers = this.gameObject;
            // }

            _triggerDrivers = _triggerDrivers.AddRange(
                gameObject.GetComponents<TriggerDriver>()
            );
            if (Utilities.IsValid(triggerDrivers))
            {
                _triggerDrivers = _triggerDrivers.AddRange(
                        triggerDrivers.GetComponentsInChildren<TriggerDriver>()
                );
            }
            Log($"Found {_triggerDrivers.Length} trigger drivers");
        }

        protected override void AccessChanged()
        {
            Log($"AccessChanged: {IsAuthorized}");
            DisableInteractive = !IsAuthorized;
        }

        public override void Interact()
        {
            if (!IsAuthorized) return;
            Log("Trigger executing");
            for (var i = 0; i < _triggerDrivers.Length; i++)
            {
                _triggerDrivers[i].OnTrigger();
            }
        }
        //TODO: call network event on trigger ?
        
        public override void MidiNoteOn(int channel, int number, int velocity)
        {
            if (!IsAuthorized) return;
            base.MidiNoteOn(channel, number, velocity);
            if (!midiEnabled) return;
            
            Log($"MidiNoteOn({channel}, {number}, {velocity})");
            if (channel == midiChannel && number == midiNumber && velocity >= midiMinVelocity)
            {
                Log("midi triggered");
                for (var i = 0; i < _triggerDrivers.Length; i++)
                {
                    _triggerDrivers[i].OnTrigger();
                }
            }
        }

        // public override void MidiNoteOff(int channel, int number, int velocity)
        // {
        //     if (!IsAuthorized) return;
        //     base.MidiNoteOff(channel, number, velocity);
        //     if (!midiEnabled) return;
        //     
        //     Log($"MidiNoteOff({channel}, {number}, {velocity})");
        // }
        
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        
        // protected override void OnValidate()
        // {
        //     base.OnValidate();
        //     if (triggerDrivers == null && Utilities.IsValid(triggerDrivers))
        //     {
        //         triggerDrivers = triggerDrivers.gameObject;
        //         this.MarkDirty();
        //     }
        // }
        
        [ContextMenu("Assign Defaults")]
        public void AssignDefaults()
        {
            if (Application.isPlaying) return;
            // UnityEditor.EditorUtility.SetDirty(this);

            var candidates = gameObject.GetComponentsInChildren<GameObject>();
            if (triggerDrivers == null)
            {
                foreach (var candidate in candidates)
                {
                    if (candidate.name == "Trigger Drivers")
                    {
                        triggerDrivers = candidate;
                        Log("Found and assigned Trigger Drivers");
                        UnityEditor.EditorUtility.SetDirty(this);
                        break;
                    }
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);

            this.MarkDirty();
        }
#endif
    }
}
