using nikkyai.common;
using nikkyai.driver;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace nikkyai
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CyclingFloat : LoggerBase
    {
        [Range(0,1)] public float offset = 0f;

        [SerializeField] private Transform floatDriverHolder;

        // [SerializeField] private float modulo = 1.0f;
        [SerializeField] private float scale = 1f;
        // private float _targetValue = 0f;
        private float _smoothedCurrent = 0f;
        private FloatDriver[] _floatDrivers = {};

        [Header("Smoothing")] // header
        [Tooltip("fraction of the distance covered within roughly 1s"),
         SerializeField, Min(0.05f),]
        private float smoothingRate = 0.1f;

        public float SmoothingRate
        {
            get => smoothingRate;
            set { smoothingRate = value; }
        }

        [Tooltip("amount of frames to skip when approaching target value," +
                 "higher number == less load, but more choppy smoothing"),
         SerializeField, Range(1,10)]
        private int smoothingUpdateInterval = 3;

        private float _rate = 0f;
        public float Rate
        {
            get => _rate;
            set => _rate = value;
        }

        void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            base._Init();
            _floatDrivers = floatDriverHolder.GetComponentsInChildren<FloatDriver>();
        }

        private float _lastServerTime = 0;
        private int _frames = 0;

        private float _velocity;
        private void Update()
        {
            var time = (float) Networking.GetServerTimeInSeconds();
            var deltaTime = (time - _lastServerTime);
            _lastServerTime = time;
            // var deltaTime = Time.deltaTime;

            var targetValue = (time * _rate * scale) % 360f;
            _smoothedCurrent += (deltaTime * _rate * scale) % 360f;
            //Log($"time: {time}");
            //Log($"deltaTime: {deltaTime}");
            //Log($"target: {targetValue}");
            //Log($"before: {_smoothedCurrent}");

            // Log($"adding : {Time.deltaTime * scale}");
            // _smoothedCurrent %= modulo;

            _frames++;
            if (Mathf.Approximately(targetValue, _smoothedCurrent))
            {
                // Log($"{_smoothedCurrent} == {targetValue}");
                // _smoothedCurrent = _targetValue;
            }
            else // if(_frames >= smoothingUpdateInterval)
            {
                var diff = targetValue - _smoothedCurrent;
                // Log($"smoothing {_smoothedCurrent} -> {targetValue} Delta: {diff}");
                _frames = 0;

                // if (targetValue > _smoothedCurrent)
                // {
                //      _smoothedCurrent = 0.1f * deltaTime + _smoothedCurrent;
                // }
                // else
                // {
                //     _smoothedCurrent = -0.1f * deltaTime  + _smoothedCurrent;
                // }

                _smoothedCurrent = Mathf.SmoothDampAngle(
                    current: _smoothedCurrent,
                    target: targetValue,
                    currentVelocity: ref _velocity,
                    smoothTime: 0.1f, 
                    maxSpeed: 10f,
                    deltaTime: deltaTime // * smoothingUpdateInterval
                );
                // maybe Mathf.LerpAngle
                // _smoothedCurrent = Mathf.Lerp(
                //     targetValue,
                //     _smoothedCurrent,
                //     Mathf.Exp(-smoothingRate * deltaTime)
                // );
            }
            // Log($"after: {_smoothedCurrent}");
            //
            var value = (float) (offset + (_smoothedCurrent / 360f * _rate)) % 1f;
            // Log($"value w/ offset: {value}");
            for (var i = 0; i < _floatDrivers.Length; i++)
            {
                _floatDrivers[i].UpdateFloatRescale(value);
            }
        }
    
         protected override string LogPrefix => nameof(CyclingFloat);
//         protected override void UpdateFloat(float value)
//         {
//             if (!enabled) return;
//             _rate = value;
//         }
// #if UNITY_EDITOR && !COMPILER_UDONSHARP
//         public override void ApplyFloatValue(float value)
//         {
//             _floatDrivers = floatDriverHolder.GetComponentsInChildren<FloatDriver>();
//             UpdateFloat(value);
//         }
// #endif
    }
}
