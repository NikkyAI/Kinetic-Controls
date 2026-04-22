using System;
using nikkyai.common;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;

namespace nikkyai.control.interact
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SelectorCallback : ACLBase
    {
        [Header("Selector Callback")] [SerializeField]
        public GameObject boolToggleDriver;
        
        [Header("Internals")]
        public Selector selector;
        public int index = -1;

        protected override string LogPrefix => $"{nameof(SelectorCallback)} : {name}";

        // public const int EVENT_INTERACT = 0;
        // public const int EVENT_RELEASE = 1;
        // const int EVENT_COUNT = 2;

        // protected override int EventCount => EVENT_COUNT;

        void Start()
        {
            _EnsureInit();
        }

        protected override void AccessChanged()
        {
            DisableInteractive = !IsAuthorized;
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
    }
}