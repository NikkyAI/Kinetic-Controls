using System;
using System.Linq;
using System.Runtime.CompilerServices;
using nikkyai.common;
using Texel;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC;
using VRC.SDKBase;

namespace nikkyai.control.interact
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Selector : BaseSyncedControl
    {
        [Header("Selector")] // header
        [SerializeField, Min(0)] private int defaultIndex = 0;
        [SerializeField] private bool clickOnActiveDisables = false;
        [SerializeField, Min(0)] private int disabledIndex = 0;
        [SerializeField] private int[] remapValues = { };
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int RemapIndex(int index)
        {
            if (index >= 0 && index < remapValues.Length)
            {
                return remapValues[index];
            }
            else
            {
                return index;
            }
        }

        [Header("Drivers")] // header
        [FormerlySerializedAs("drivers")]
        [SerializeField] private Transform intSelectedDrivers;
        [FormerlySerializedAs("isAuthorizedIndicator")]
        [SerializeField] private Transform boolAuthorizedDrivers;
        
        protected override string LogPrefix => $"{nameof(Selector)} {name}";

        //TODO: replace with Texel.InteractTrigger and handle ACL centrally
        private SelectorCallback[] _interactCallbacks = { };
        private IntDriver[] _intDrivers = { };
        private BoolDriver[][] _boolDrivers = { };
        private BoolDriver[] _isAuthorizedBoolDrivers = { };

        [Header("State")] // header
        [SerializeField, UdonSynced]
        private bool synced = true;

        public override bool Synced
        {
            get => synced;
            set
            {
                if (!isAuthorized) return;

                var prevValue = _syncedIndex;
                TakeOwnership();
                Log($"set synced to {value}");
                synced = value;
                Log($"set index to {_syncedIndex} => {prevValue}");
                _syncedIndex = prevValue;
                
                RequestSerialization();
            }
        }

        [UdonSynced, FieldChangeCallback(nameof(SyncedIndex))]
        private int _syncedIndex;

        public int SyncedIndex
        {
            get => _syncedIndex;
            set
            {
                var oldIndex = _syncedIndex;

                if (oldIndex != value)
                {
                    _syncedIndex = value;
                    Log($"index changed {oldIndex} => {_syncedIndex}");
                    var remappedValue = RemapIndex(_syncedIndex);
                    // if (remapValues.Length - 1 >= _syncedIndex)
                    // {
                    //     remappedValue = remapValues[_syncedIndex];
                    // }

                    
                    for (var i = 0; i < _intDrivers.Length; i++)
                    {
                        _intDrivers[i].OnUpdateInt(remappedValue);
                    }

                    if (_syncedIndex >= 0 && _syncedIndex < _boolDrivers.Length)
                    {
                        var newDrivers = _boolDrivers[_syncedIndex];
                        if (newDrivers != null)
                        {
                            for (var i = 0; i < newDrivers.Length; i++)
                            {
                                newDrivers[i].OnUpdateBool(true);
                            }
                        }
                    }

                    if (oldIndex >= 0 && oldIndex < _boolDrivers.Length)
                    {
                        var oldDrivers = _boolDrivers[oldIndex];
                        if (oldDrivers != null)
                        {
                            for (var i = 0; i < oldDrivers.Length; i++)
                            {
                                oldDrivers[i].OnUpdateBool(false);
                            }
                        }
                    }
                }
                // if (synced)
                // {
                //     Log("taking ownership and serializing");
                //     if (!Networking.IsOwner(gameObject))
                //     {
                //         Networking.SetOwner(Networking.LocalPlayer, gameObject);
                //     }
                //     RequestSerialization();
                // }
            }
        }

        public void UpdateSyncedIndex()
        {
            if (synced)
            {
                Log("taking ownership and serializing");
                if (!Networking.IsOwner(gameObject))
                {
                    Networking.SetOwner(Networking.LocalPlayer, gameObject);
                }
                RequestSerialization();
            }
        }

        private void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            base._Init();
            SetupComponents();
            // for (var i = 0; i < _interactCallbacks.Length; i++)
            // {
            //     Log($"register interact callback {i}");
            //     _interactCallbacks[i]._Register(
            //         eventIndex: SelectorCallback.EVENT_INTERACT,
            //         handler: this,
            //         eventName: nameof(_OnInteract),
            //         args: nameof(_interactIndex)
            //     );
            // }
        }

        private void SetupComponents()
        {
            _syncedIndex = defaultIndex;
            if (Utilities.IsValid(intSelectedDrivers))
            {
                _intDrivers = intSelectedDrivers.GetComponentsInChildren<IntDriver>();
                            
            }
            _interactCallbacks = GetComponentsInChildren<SelectorCallback>();
            Log($"Found {_interactCallbacks.Length} selector buttons");
            _boolDrivers = new BoolDriver[_interactCallbacks.Length][];
            
            for (var i = 0; i < _interactCallbacks.Length; i++)
            {
                var callback = _interactCallbacks[i];
                callback.selector = this;
                callback.Index = i;
                _boolDrivers[i] = callback.GetComponentsInChildren<BoolDriver>();
                Log($"Found {_boolDrivers[i].Length} bool drivers for selector button {i}");
            }
            if (Utilities.IsValid(boolAuthorizedDrivers))
            {
                Log($"loading auth drivers");
                _isAuthorizedBoolDrivers = boolAuthorizedDrivers.GetComponentsInChildren<BoolDriver>();
            }
        }

        protected override void AccessChanged()
        {
            for (var i = 0; i < _interactCallbacks.Length; i++)
            {
                _interactCallbacks[i].DisableInteractive = !isAuthorized;
            }

            for (var i = 0; i < _isAuthorizedBoolDrivers.Length; i++)
            {
                _isAuthorizedBoolDrivers[i].OnUpdateBool(isAuthorized);
            }
        }

        // [NonSerialized] private int _interactIndex;
        public void _OnInteract(int index)
        {
            if (!isAuthorized) return;

            TakeOwnership();
            Log($"interact {index}");
            if(clickOnActiveDisables && SyncedIndex == index)
            {
                SyncedIndex = disabledIndex;
            }
            else
            {
                SyncedIndex = index;
            }

            UpdateSyncedIndex();
        }

        public override void OnDeserialization()
        {
        }

        public void Reset()
        {
            SyncedIndex = defaultIndex;
            UpdateSyncedIndex();
        }

        // ReSharper disable InconsistentNaming
        [NonSerialized] private int prevDefault = -1;
        [NonSerialized] private int[] prevRemap = { };
        [NonSerialized] private AccessControl prevAccessControl;
        [NonSerialized] private bool prevEnforceACL;
        [NonSerialized] private DebugLog prevDebugLog;
        [NonSerialized] private bool childrenInitialized = false;
        // ReSharper restore InconsistentNaming
#if UNITY_EDITOR && !COMPILER_UDONSHARP

        private void OnValidate()
        {
            if (Application.isPlaying) return;
            UnityEditor.EditorUtility.SetDirty(this);

            if (!childrenInitialized
                || prevAccessControl != AccessControl
                || prevEnforceACL != EnforceACL
                || prevDebugLog != DebugLog
               )
            {
                ApplyACLsAndLog();
                prevAccessControl = AccessControl;
                prevDebugLog = DebugLog;
                childrenInitialized = true;
            }

            if (prevDefault != defaultIndex
                || prevRemap.SequenceEqual(remapValues)
               )
            {
                ApplyValues();
                prevDefault = defaultIndex;
                prevRemap = remapValues;
            }
        }


        [ContextMenu("Apply Values")]
        public void ApplyValues()
        {
            SetupComponents();
            foreach (var intDriver in _intDrivers)
            {
                // var remappedValue = defaultIndex;
                // if (remapValues.Length - 1 >= defaultIndex)
                // {
                //     remappedValue = remapValues[defaultIndex];
                // }

                intDriver.ApplyIntValue(RemapIndex(defaultIndex));
                // intDriver.gameObject.MarkDirty();
            }

            for (var i = 0; i < _boolDrivers.Length; i++)
            {
                for (var j = 0; j < _boolDrivers[i].Length; j++)
                {
                    _boolDrivers[i][j].ApplyBoolValue(defaultIndex == i);
                    // _boolDrivers[i][j].gameObject.MarkDirty();
                }
            }
        }

        [ContextMenu("Apply ACLs and Log")]
        private void ApplyACLsAndLog()
        {
            var children = gameObject.GetComponentsInChildren<SelectorCallback>(true);
            for (var index = 0; index < children.Length; index++)
            {
                var interactCallback = children[index];
                interactCallback.Index = index;
                interactCallback.EditorACL = AccessControl;
                interactCallback.EditorDebugLog = DebugLog;
                interactCallback.EditorEnforceACL = EnforceACL;
                interactCallback.MarkDirty();
            }
        }
#endif
    }
}