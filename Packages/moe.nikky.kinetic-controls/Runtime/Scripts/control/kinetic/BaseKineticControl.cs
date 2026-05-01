#define READONLY

using System;
using nikkyai.attribute;
using nikkyai.Utils;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC;
using VRC.Dynamics;
using VRC.SDK3.Components;
using VRC.SDK3.Midi;
using VRC.SDKBase;

// ReSharper disable ForCanBeConvertedToForeach

namespace nikkyai.control.kinetic
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    // [RequireComponent(typeof(VRCMidiListener))]
    public abstract class BaseKineticControl : BaseSmoothedControl
    {
        [Header("Kinetic Control")] //

        [Header("Kinetic Control - MIDI - Requires VRC_MidiListener Component")] //
        [Tooltip("Requires a VRC MIDI Listened with CC enabled")]
        [SerializeField]
#if READONLY
        [ReadOnly]
#endif
        internal bool midiEnabled = true;
        [SerializeField]
#if READONLY
        [ReadOnly]
#endif
        [Range(0,15)]
        internal int midiChannel = 0;
        [SerializeField] 
#if READONLY
        [ReadOnly]
#endif
        [Range(0,127)]
        internal int midiNumber = 0;
        [SerializeField] 
#if READONLY
        [ReadOnly]
#endif
        [Range(0,127)]
        internal int midiInputRangeStart = 0;
        [SerializeField] 
#if READONLY
        [ReadOnly]
#endif
        [Range(0,127)]
        internal int midiInputRangeEnd = 127;

        [Header("Kinetic Control - Components")] //
        [SerializeField]
#if READONLY
        [ReadOnly]
#endif
        internal HandleAbstract handle;
        
        [Header("Kinetic Control - Debug")] // header
        [SerializeField]
#if READONLY
        [ReadOnly]
#endif
        internal Transform debugDesktopRaytrace;

        public override bool Synced
        {
            get => synced;
            set
            {
                if (!IsAuthorized) return;

                var prevValue = SyncedValueNormalized;
                TakeOwnership();
                Log($"set synced to {value}");
                synced = value;
                Log($"set value to {SyncedValueNormalized} => {prevValue}");
                SyncedValueNormalized = prevValue;

                RequestSerialization();
                OnDeserialization();
            }
        }

        private float _lastSyncedValueNormalized = 0;
        

        protected override void _Init()
        {
            base._Init();
            Log("Base KineticControl Init");
            if (Utilities.IsValid(handle))
            {
                handle.RegisterRuntime(this);
            }
            // SetupHandle();
            // SetupPickup();
            // SetupPickupRigidBody();

            // LocalPlayer = Networking.LocalPlayer;
            // if (Utilities.IsValid(Networking.LocalPlayer))
            // {
            //     _isInVR = Networking.LocalPlayer.IsUserInVR();
            // }

            // UseContactsInVRLocal = !UseContactsInVR;
            // UseContactsInVRLocal = UseContactsInVR;

            SyncedValueNormalized = defaultValueNormalized;
        }

        public abstract void FollowPickup();
        public abstract void FollowDesktop();

        protected abstract float PosToNormalized(Vector3 relativePos);

        public void OnMoveHandle(Vector3 absolutePosition)
        {
            SyncedIsBeingManipulated = true;
            SyncedValueNormalized = PosToNormalized(absolutePosition);
            if (synced)
            {
                TakeOwnership();
                RequestSerialization();
            }
            OnDeserialization();
        }
        public void OnDropHandle()
        {
            SyncedIsBeingManipulated = false;
        }

        public override void OnDeserialization()
        {
            if (!Mathf.Approximately(SyncedValueNormalized, _lastSyncedValueNormalized))
            {
                _UpdateTargetValue(SyncedValueNormalized);

                _lastSyncedValueNormalized = SyncedValueNormalized;
            }
        }
        
        // public override void MidiNoteOn(int channel, int number, int velocity)
        // {
        //     if (!IsAuthorized) return;
        //     base.MidiNoteOn(channel, number, velocity);
        //     if (!midiEnabled) return;
        //     
        //     Log($"MidiNoteOn({channel}, {number}, {velocity})");
        // }
        //
        // public override void MidiNoteOff(int channel, int number, int velocity)
        // {
        //     if (!IsAuthorized) return;
        //     base.MidiNoteOff(channel, number, velocity);
        //     if (!midiEnabled) return;
        //     
        //     Log($"MidiNoteOff({channel}, {number}, {velocity})");
        // }

        public override void MidiControlChange(int channel, int number, int value)
        {
            if (!IsAuthorized) return;
            base.MidiControlChange(channel, number, value);
            if (!midiEnabled) return;
            
            Log($"MidiControlChange({channel}, {number}, {value})");
            if (channel == midiChannel && number == midiNumber)
            {
                float normalizedValue = Mathf.InverseLerp(midiInputRangeStart, midiInputRangeEnd, value);
                Log($"normalized value: {normalizedValue}");
                SetValue(normalizedValue);
            }
        }
        public void DebugDesktopRaytrace(bool debugActive)
        {
            if (Utilities.IsValid(debugDesktopRaytrace))
            {
                debugDesktopRaytrace.gameObject.SetActive(debugActive);
            }
        }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
//         internal void SetupHandle() {
//             Log("SetupHandle");
//             if (Utilities.IsValid(handle))
//             {
//                 handle._EnsureInit();
//                 handle.Register(this);
//
//                 // if (Utilities.IsValid(handleReset))
//                 // {
//                 //     handle.handleReset = handleReset;
//                 // }
//                 // handle.UseContactsInVR = useContactsInVR;
//
//                 // _contactReceiver = faderHandle.GetComponent<ContactReceiver>();
//
// #if UNITY_EDITOR && !COMPILER_UDONSHARP
//                 handle.EditorACL = AccessControl;
//                 handle.EditorEnforceACL = EnforceACL;
//                 handle.EditorDebugLog = DebugLog;
// #endif
//             }
//             else
//             {
//                 LogError($"missing handle in {name}");
//             }
//         }
        
        // protected override void OnValidate()
        // {
        //     if (Application.isPlaying) return;
        //     base.OnValidate();
        //
        //     if (
        //         ValidationCache.ShouldRunValidation(
        //             this,
        //             HashCode.Combine(
        //                 AccessControl,
        //                 DebugLog,
        //                 EnforceACL
        //             )
        //         )
        //     )
        //     {
        //         ApplyValues();
        //     }
        // }
        //
        // public override void ApplyValues()
        // {
        //     base.ApplyValues();
        //     _EnsureInit();
        //     // SetupPickup();
        //     // SetupPickupRigidBody();
        //
        //     handle.EditorACL = AccessControl;
        //     handle.EditorDebugLog = DebugLog;
        //     handle.EditorEnforceACL = EnforceACL;
        //     // handle.handleReset = handleReset;
        //
        //     // OnDeserialization();
        //     handle.ResetTransform();
        // }
#endif
    }
}