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
    public class Selector : ACLBaseSimple
    {
        [Header("Selector")] // header
        [SerializeField]
        [Min(0)]
        private int defaultIndex = 0;
        
        [SerializeField]
        private bool clickOnActiveDisables = false;
        
        [SerializeField]
        [Min(0)]
        [Tooltip("index to select when deactivated by clickign on current active, requires clickOnActiveDisables")]
        private int disabledIndex = 0;
        
        [SerializeField] 
        [Tooltip("remaps index to value for indices that exist")]
        private int[] remapValues = { };
        
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

        // [SerializeField] private GameObject selectorCallbacks;

        [Header("Drivers")] // header
        [FormerlySerializedAs("intSelectedDrivers")]
        [SerializeField] private GameObject intDrivers;
        
        protected override string LogPrefix => nameof(Selector);

        //TODO: replace with Texel.InteractTrigger and handle ACL centrally ???
        private SelectorCallback[] _interactCallbacks = { };
        private IntDriver[] _intDrivers = { };
        private BoolDriver[][] _boolDrivers = { };
        // private BoolDriver[] _isAuthorizedBoolDrivers = { };

        [Header("State")] // header
        [SerializeField, UdonSynced]
        private bool synced = true;

        public override bool Synced
        {
            get => synced;
            set 
            {
                if (!IsAuthorized) return;

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
            // Log("SetupComponents");
            if (Utilities.IsValid(intDrivers))
            {
                Log("getting int drivers");
                _intDrivers = intDrivers.GetComponentsInChildren<IntDriver>();
            }
            if (Utilities.IsValid(gameObject))
            {
                Log("getting interact callbacks");
                _interactCallbacks = gameObject.GetComponentsInChildren<SelectorCallback>();
            }
            if (Utilities.IsValid(_interactCallbacks))
            {
                Log($"Found {_interactCallbacks.Length} selector buttons");
            }
            else
            {
                LogWarning("found no interact callbacks");
            }
            _boolDrivers = new BoolDriver[_interactCallbacks.Length][];
            
            for (var i = 0; i < _interactCallbacks.Length; i++)
            {
                var callback = _interactCallbacks[i];
                callback.selector = this;
                callback.index = i;
                var boolToggleDriver = callback.boolToggleDriver;
                if (boolToggleDriver == null)
                {
                    boolToggleDriver = callback.gameObject;
                }
                _boolDrivers[i] = boolToggleDriver.GetComponentsInChildren<BoolDriver>();
                Log($"Found {_boolDrivers[i].Length} bool drivers for selector button {i}");
                foreach (var boolDriver in _boolDrivers[i])
                {
                    boolDriver._EnsureInit();
                    boolDriver.OnUpdateBool(i == defaultIndex);
                }
            }
            SyncedIndex = defaultIndex;
        }

        protected override void AccessChanged()
        {
            for (var i = 0; i < _interactCallbacks.Length; i++)
            {
                _interactCallbacks[i].DisableInteractive = !IsAuthorized;
            }
        }

        // [NonSerialized] private int _interactIndex;
        public void _OnInteract(int index)
        {
            if (!IsAuthorized) return;

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
        /*[NonSerialized]*/ private int prevDefault = -1;
        /*[NonSerialized]*/ private int[] prevRemap = { };
        /*[NonSerialized]*/ private AccessControl prevAccessControl;
        /*[NonSerialized]*/ private bool prevEnforceACL;
        /*[NonSerialized]*/ private DebugLog prevDebugLog;
        /*[NonSerialized]*/ private bool childrenInitialized = false;
        // ReSharper restore InconsistentNaming
#if UNITY_EDITOR && !COMPILER_UDONSHARP

        protected override void OnValidate()
        {
            if (Application.isPlaying) return;
            base.OnValidate();
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
                interactCallback.index = index;
                interactCallback.EditorACL = AccessControl;
                interactCallback.EditorDebugLog = DebugLog;
                interactCallback.EditorEnforceACL = EnforceACL;
                interactCallback.MarkDirty();
            }
        }
#endif
    }
}