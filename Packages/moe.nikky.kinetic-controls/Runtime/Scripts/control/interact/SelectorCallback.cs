using System;
using nikkyai.common;
using nikkyai.Editor;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace nikkyai.control.interact
{
#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [RequireComponent(typeof(PreProcessEditorHelper))]
#endif
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SelectorCallback : LoggingSimple
    {
        [Header("Selector Callback")] [SerializeField]
        public GameObject boolToggleDriver;

        [Header("Internals")] 
        [SerializeField] [ReadOnly] public Selector selector;
        [SerializeField] [ReadOnly] public int index = -1;

        [SerializeField, ReadOnly] internal BoolDriver[] boolDrivers = { };

        protected override string LogPrefix => nameof(SelectorCallback);

        // public const int EVENT_INTERACT = 0;
        // public const int EVENT_RELEASE = 1;
        // const int EVENT_COUNT = 2;

        // protected override int EventCount => EVENT_COUNT;

        void Start()
        {
            _EnsureInit();
        }

        private bool IsAuthorized { get; set; } = false;

        internal void OnAccessChanged(bool isAuthorized)
        {
            IsAuthorized = isAuthorized;
            DisableInteractive = !isAuthorized;
        }

        // private bool _isInteracting = false;
        public override void Interact()
        {
            // if (_isInteracting) return;
            if (!IsAuthorized) return;
            // _isInteracting = true;
            Log($"interact on {index}");
            selector._OnInteract(index);
            // _UpdateHandlers(EVENT_INTERACT, index);
        }

        // public override void InputUse(bool value, VRC.Udon.Common.UdonInputEventArgs args)
        // {
        //     if (!_isInteracting) return;
        //     if (!isAuthorized) return;
        //     if (!value)
        //     {
        //         _isInteracting = false;
        //         // _UpdateHandlers(EVENT_RELEASE, index);
        //     }
        // }
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public override bool OnPreprocess()
        {
            if (Utilities.IsValid(boolToggleDriver))
            {
                boolDrivers = boolToggleDriver.GetComponentsInChildren<BoolDriver>();
            }

            return true;
        }
#endif
    }
}