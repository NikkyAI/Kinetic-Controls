using nikkyai.common;
using nikkyai.driver;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Persistence;
using VRC.SDKBase;

namespace nikkyai.button
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LocalToggle : ACLBase
    {
        [Tooltip(
            "The button will initialize into this value, toggle this for elements that should be enabled by default")]
        [SerializeField]
        private bool defaultValue = false;

        [Header("Persistence")] // header
        [Tooltip("Turn on if this toggle should be saved using Persistence.")]
        [SerializeField]
        private bool usePersistence = false;

        [Tooltip(
            "Data Key that will be used to save / load this Setting, everything using Persistence should have a different Data Key.")]
        [SerializeField]
        private string dataKey = "CHANGE THIS";

        protected override string LogPrefix => nameof(LocalToggle);

        private bool _isOn;

        public bool IsOn => _isOn;

        public const int EVENT_UPDATE = 0;
        public const int EVENT_COUNT = 1;

        protected override int EventCount => EVENT_COUNT;
        private BoolDriver[] _boolDrivers = { };

        void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            _isOn = defaultValue;
            _boolDrivers = GetComponentsInChildren<BoolDriver>();
            _UpdateState();
        }

        protected override void AccessChanged()
        {
            DisableInteractive = !isAuthorized;
        }

        public override void OnPlayerRestored(VRCPlayerApi player)
        {
            if (!player.isLocal || !usePersistence) return;

            bool storedState = false;
            if (PlayerData.TryGetBool(player, dataKey, out bool boolValue))
            {
                storedState = boolValue;
                // persistenceLoaded = true;
            }
            else
            {
                return;
            }

            _isOn = storedState;
            _UpdateState();
        }

        public override void Interact()
        {
            if (!isAuthorized) return;

            _isOn = !_isOn;
            _UpdateState();
        }

        //TODO: migrate to bool driver for more modularity
        private void _UpdateState()
        {
            if (usePersistence)
            {
                PlayerData.SetBool(dataKey, _isOn);
            }
  
            for (var i = 0; i < _boolDrivers.Length; i++)
            {
                _boolDrivers[i].UpdateBool(_isOn);
            }

            _UpdateHandlers(EVENT_UPDATE);
        }
    }
}