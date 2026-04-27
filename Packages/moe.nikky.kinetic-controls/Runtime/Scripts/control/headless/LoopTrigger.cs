using nikkyai.common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace nikkyai.control.headless
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LoopTrigger : LoggingSimple
    {
        protected override string LogPrefix => nameof(LoopTrigger);

        [SerializeField] [Min(5f)] private Vector2 delay = new Vector2(20.0f, 30.0f);

        [SerializeField] private bool onlyInstanceMaster = false;
        [SerializeField] private GameObject triggerDrivers;

        private TriggerDriver[] _triggerDrivers = { };

        private float _minDelay, _maxDelay;

        void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            if (delay.x < delay.y)
            {
                _minDelay = delay.x;
                _maxDelay = delay.y;
            }
            else
            {
                _minDelay = delay.y;
                _maxDelay = delay.x;
            }

            if (triggerDrivers == null)
            {
                triggerDrivers = gameObject;
            }

            _triggerDrivers = triggerDrivers.GetComponentsInChildren<TriggerDriver>();
            Log($"found {_triggerDrivers.Length} trigger drivers");
        }

        private bool _toggleState = false;

        private bool _timerRunning = false;
        public bool TimerRunning
        {
            get => _timerRunning;
            set
            {
                Log($"timer set {_toggleState} -> {value}");

                if (!_toggleState && value)
                {
                    _toggleState = true;

                    // start timer
                    if (!_timerRunning)
                    {
                        TriggerTimer();
                    }
                    else
                    {
                        LogWarning("Timer already running");
                    }
                }

                if (!value && _toggleState)
                {
                    _toggleState = false;
                }
            }
        }

        public void TriggerTimer()
        {
            Log("timer triggered");
            _timerRunning = false;

            if (!onlyInstanceMaster || Networking.IsMaster)
            {
                Log($"running triggers {_toggleState}");
                if (_toggleState)
                {
                    foreach (var triggerDriver in _triggerDrivers)
                    {
                        triggerDriver.OnTrigger();
                    }
                }
            }

            if (!_timerRunning && _toggleState)
            {
                // call timer on a delay
                _timerRunning = true;
                float nextDelay = Random.Range(_minDelay, _maxDelay);
                SendCustomEventDelayedSeconds(nameof(TriggerTimer), nextDelay);
            }
            else
            {
                LogWarning("too many timers already running or smth else broke");
            }
        }

        public override void OnMasterTransferred(VRCPlayerApi newMaster)
        {
            if (Utilities.IsValid(newMaster))
            {
                Log($"New master: {newMaster.displayName}");
            }
        }
    }
}