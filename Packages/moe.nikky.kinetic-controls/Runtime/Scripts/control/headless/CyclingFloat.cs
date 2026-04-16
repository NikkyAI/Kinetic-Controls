using System;
using System.ComponentModel;
using JetBrains.Annotations;
using nikkyai.common;
using nikkyai.Utils;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;

namespace nikkyai.control.headless
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CyclingFloat : LoggerBase
    {
        [Header("Cycling Float")] //
        [Range(0, 1)]
        public float offset = 0f;

        [FormerlySerializedAs("rate")] [Range(-1, 1)]
        public float speed = 0f;

        [SerializeField, Range(0f, 1f)] public float maxSpeed = 10f;

        // private float _targetValue = 0f;
        [SerializeField] private Transform floatDriverHolder;

        private float _smoothedCurrent = 0f;
        private FloatDriver[] _floatDrivers = { };

        [Header("Smoothing")] // header
        [SerializeField, Min(0f)]
        public float smoothTime = 0.1f;
        //
        // [Tooltip("fraction of the distance covered within roughly 1s"),
        //  SerializeField, Min(0.05f),]
        // private float smoothingRate = 0.1f;
        //
        // public float SmoothingRate
        // {
        //     get => smoothingRate;
        //     set { smoothingRate = value; }
        // }

        [Tooltip("amount of frames to skip when approaching target value," +
                 "higher number == less load, but more choppy smoothing"),
         SerializeField, Range(1, 10)]
        private int smoothingUpdateInterval = 3;

        public bool debug = false;

        protected override string LogPrefix => nameof(CyclingFloat);

        void Start()
        {
            _EnsureInit();
        }


        protected override void _Init()
        {
            base._Init();
            _lastTime = Time.time;
            _floatDrivers = floatDriverHolder.GetComponentsInChildren<FloatDriver>();

            SendCustomEventDelayedFrames(nameof(OnUpdateCyclingValue), smoothingUpdateInterval);
            SendCustomEventDelayedFrames(nameof(PostInitResetValues), 5);
        }

        public void PostInitResetValues()
        {
            for (var i = 0; i < _floatDrivers.Length; i++)
            {
                _floatDrivers[i].UpdateFloatRescale(0f);
            }
        }

        public void Reset()
        {
            //TODO: reset rate?
            //throw new System.NotImplementedException();
        }

        // private int _frames = 0;

        private float _lastTime;
        private float _velocity;

        private float _lastValue = float.NegativeInfinity;

//        private int schedulecCount = 0;

        public void OnUpdateCyclingValue()
        {
            SendCustomEventDelayedFrames(nameof(OnUpdateCyclingValue), smoothingUpdateInterval);
            UpdateCyclingValue();
        }
        
        [UsedImplicitly]
        public void UpdateCyclingValue() {

            var time = Time.time;
            var deltaTime = time - _lastTime;
            _lastTime = time;

            var target = (speed * time) + offset;
            float delta = Mathf.Repeat(target - _smoothedCurrent, 1f);
            // if (delta > 0.5f)
            // {
            //     delta -= 1f;
            // }

            if (Mathf.Approximately(delta, 0f))
            {
                return;
            }

            if (target > _smoothedCurrent)
            {
                target = _smoothedCurrent + delta;
            }
            else if (target < _smoothedCurrent)
            {
                target = _smoothedCurrent + delta - 1f;
            }
            else
            {
                return;
            }

            if (debug)
            {
                Log($"delta {delta:0.00}");
                Log($"before {_smoothedCurrent:0.00} => {target:0.00f}");
            }

            _smoothedCurrent = SmoothDamp(
                current: _smoothedCurrent,
                target: target,
                currentVelocity: ref _velocity,
                smoothTime: smoothTime,
                maxSpeed: maxSpeed,
                deltaTime: deltaTime
            );
            if (debug)
            {
                Log($"velocity:  {_velocity:0.00}");
            }

            var value = Mathf.Repeat(_smoothedCurrent, 1f);
            if (!Mathf.Approximately(_lastValue, value))
            {
                for (var i = 0; i < _floatDrivers.Length; i++)
                {
                    _floatDrivers[i].UpdateFloatRescale(value);
                }

                _lastValue = value;
            }
        }


        // public bool Approximately(float a, float b)
        // {
        //     return (double)Mathf.Abs(b - a) <
        //            (double)Mathf.Max(1E-03f * Mathf.Max(Mathf.Abs(a), Mathf.Abs(b)), Mathf.Epsilon * 8f);
        // }

        public static float DeltaWrapping(float current, float target)
        {
            float delta = Mathf.Repeat(target - current, 1f);
            if ((double)delta > (0.5f))
                delta -= 1f;
            return delta;
        }

        // copied from https://github.com/Unity-Technologies/UnityCsReference/blob/2023.1/Runtime/Export/Math/Mathf.cs#L308
        // because udonsharp cannot pass ref values to native code
        static float SmoothDamp(
            float current,
            float target,
            ref float currentVelocity,
            float smoothTime,
            [DefaultValue("Mathf.Infinity")] float maxSpeed,
            [DefaultValue("Time.deltaTime")] float deltaTime
        )
        {
            // Based on Game Programming Gems 4 Chapter 1.10
            smoothTime = Mathf.Max(0.0001F, smoothTime);
            float omega = 2F / smoothTime;

            float x = omega * deltaTime;
            float exp = 1F / (1F + x + 0.48F * x * x + 0.235F * x * x * x);
            float change = current - target;
            float originalTo = target;

            // Clamp maximum speed
            float maxChange = maxSpeed * smoothTime;
            change = Mathf.Clamp(change, -maxChange, maxChange);
            target = current - change;

            float temp = (currentVelocity + omega * change) * deltaTime;
            currentVelocity = (currentVelocity - omega * temp) * exp;
            float output = target + (change + temp) * exp;

            // Prevent overshooting
            if (originalTo - current > 0.0F == output > originalTo)
            {
                output = originalTo;
                currentVelocity = (output - originalTo) / deltaTime;
            }

            return output;
        }

        // private int validationhashCycling = 0;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        protected override void OnValidate()
        {
            if (Application.isPlaying) return;
            base.OnValidate();

            if (
                ValidationCache.ShouldRunValidation(
                    this,
                    HashCode.Combine(
                        offset,
                        speed,
                        // maxSpeed,
                        // smoothTime,
                        // smoothingUpdateInterval,
                        floatDriverHolder
                    )
                )
            )
            {
                ApplyDefaultValues();
            }
            // var hash = HashCode.Combine(offset, floatDriverHolder);
            // if (hash != validationhashCycling)
            // {
            //     
            //     validationhashCycling =  hash;
            // }
        }

        [ContextMenu("Reset Values")]
        public void ApplyDefaultValues()
        {
            _floatDrivers = floatDriverHolder.GetComponentsInChildren<FloatDriver>();
            Log($"applying default to {_floatDrivers.Length} float drivers");
            for (var i = 0; i < _floatDrivers.Length; i++)
            {
                _floatDrivers[i].ApplyFloatValue(offset);
            }
        }
#endif
    }
}