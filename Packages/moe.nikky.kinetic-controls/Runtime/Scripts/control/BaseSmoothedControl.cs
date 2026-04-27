#define HIDE_INSPECTOR

using System;
using System.ComponentModel;
using nikkyai.common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace nikkyai.control
{
    public abstract class BaseSmoothedControl : ACLBaseReadonly
    {
        protected abstract float MinPosOrRot { get; }

        protected abstract float MaxPosOrRot { get; }

        #region default value

        [Header("Base Smoothed Control")]
        [SerializeField, UdonSynced] //
#if HIDE_INSPECTOR
        [HideInInspector]
#endif
        internal bool synced = true;

        [SerializeField]
        [Tooltip("The range of values that this behaviour will send to any attached float drivers")]
#if HIDE_INSPECTOR
        [HideInInspector]
#endif
        internal Vector2 outputRange = new Vector2(0, 1);

        [SerializeField]
        [Range(0, 1)]
#if HIDE_INSPECTOR
        [HideInInspector]
#endif
        internal float defaultValueNormalized = 0.25f;
        [SerializeField]
#if HIDE_INSPECTOR
        [HideInInspector]
#endif
        internal float defaultValue = 0;

        internal float MinValue => outputRange.x;
        internal float MaxValue => outputRange.y;

        #endregion

        #region drivers

        [Header("Base Smoothed Control - Drivers")] // header
        [SerializeField]
#if HIDE_INSPECTOR
        [HideInInspector]
#endif
        internal GameObject floatTargetValueDrivers;

        [SerializeField]
#if HIDE_INSPECTOR
        [HideInInspector]
#endif
        internal GameObject floatSmoothedValueDrivers;

        [SerializeField, ReadOnly] public FloatDriver[] _targetValueFloatDrivers = Array.Empty<FloatDriver>();
        [SerializeField, ReadOnly] internal FloatDriver[] _smoothedValueFloatDrivers = Array.Empty<FloatDriver>();

        #endregion

        #region value smoothing

        [Header("Base Smoothed Control - Smoothing")] // header
        [Tooltip(
             "smoothes out value updates over time, may impact CPU frametimes AND cause more updates to FloatDrivers")]
        [SerializeField]
#if HIDE_INSPECTOR
        [HideInInspector]
#endif
        internal bool enableValueSmoothing = true;

        public bool ValueSmoothing
        {
            get => enableValueSmoothing;
            set => enableValueSmoothing = value;
        }

        [Tooltip("amount of frames to skip when approaching target value," +
                 "higher number == less load, but more choppy smoothing")]
        [SerializeField]
        [Range(1, 10)]
#if HIDE_INSPECTOR
        [HideInInspector]
#endif
        internal int smoothingUpdateInterval = 3;

        public int SmoothingFrames
        {
            get => smoothingUpdateInterval;
            set => smoothingUpdateInterval = value;
        }

        // [Tooltip("fraction of the distance covered within roughly 1s"),
        //  SerializeField, Min(0.05f),]
        // private float smoothingRate = 0.5f;
        //
        // public float SmoothingRate
        // {
        //     get => smoothingRate;
        //     set => smoothingRate = value;
        // }

        [Tooltip("higher values -> faster synchronization with the target maxSpeed")]
        [SerializeField]
        [Range(0f, 2.5f)]
#if HIDE_INSPECTOR
        [HideInInspector]
#endif
        public float smoothingTime = 0.1f;

        [Tooltip("Maximum speed that smoothing can move at (see Unity Mathf.SmoothDamp maxSpeed parameter)")]
        [SerializeField]
        [Range(0f, 1f)]
#if HIDE_INSPECTOR
        [HideInInspector]
#endif
        public float smoothingMaxSpeed = 0.25f;

        protected float smoothingTargetNormalized;
        protected float smoothedCurrentNormalized;

        protected bool IsCyclic = false;

        private const float Epsilon = 0.005f;
        private bool _valueInitialized = false;
        private bool _isSmoothing = false;
        private float _lastFrameTime = 0;

        #endregion


        [UdonSynced]
        // IMPORTANT, DO NOT DELETE
        protected float SyncedValueNormalized;

        [UdonSynced] // IMPORTANT, DO NOT DELETE
        protected bool SyncedIsBeingManipulated;

        protected abstract void UpdateTargetIndicator(float clampedPosOrRotEuler);

        protected abstract void UpdateValueIndicator(float clampedPosOrRotEuler);

        // protected void InitValueSmoothing()
        // {
        //     // NOTE: maybe we can get away without it ?
        //     // smoothedCurrentNormalized = _normalizedDefault;
        //     // smoothingTargetNormalized = _normalizedDefault;
        //     enableValueSmoothing = enableValueSmoothing && smoothingUpdateInterval > 0;
        // }

        internal void FindDrivers()
        {
            Log($"Searching for float value drivers in {floatSmoothedValueDrivers}");
            if (Utilities.IsValid(floatSmoothedValueDrivers))
            {
                Log($"searching for drivers in {floatSmoothedValueDrivers.gameObject}");
                _smoothedValueFloatDrivers =
                    floatSmoothedValueDrivers.gameObject.GetComponentsInChildren<FloatDriver>();
                Log($"found {_smoothedValueFloatDrivers.Length} drivers for value");
            }
            else
            {
                LogError("missing object for float value drivers");
            }

            Log($"Searching for float target drivers in {floatTargetValueDrivers}");
            if (Utilities.IsValid(floatTargetValueDrivers))
            {
                _targetValueFloatDrivers = floatTargetValueDrivers.GetComponentsInChildren<FloatDriver>();
                Log($"found {_targetValueFloatDrivers.Length} drivers for target");
            }
            else
            {
                LogError("missing object for float target drivers");
            }

            if (_smoothedValueFloatDrivers != null)
            {
                Log($"found {_smoothedValueFloatDrivers.Length} drivers for value");
            }

            if (_targetValueFloatDrivers != null)
            {
                Log($"found {_targetValueFloatDrivers.Length} drivers for target");
            }
        }

        private void SetupSmoothedControlValues()
        {
            //TODO: move into running in editor ?

            if (_smoothedValueFloatDrivers != null)
            {
                Log($"found {_smoothedValueFloatDrivers.Length} drivers for value");
            }

            if (_targetValueFloatDrivers != null)
            {
                Log($"found {_targetValueFloatDrivers.Length} drivers for target");
            }

            defaultValueNormalized = Mathf.Clamp01(defaultValueNormalized);
            smoothedCurrentNormalized = defaultValueNormalized;
            smoothingTargetNormalized = defaultValueNormalized;
        }

        protected override void _Init()
        {
            base._Init();

            FindDrivers();
            SetupSmoothedControlValues();

            defaultValueNormalized = Mathf.Clamp01(defaultValueNormalized);
            smoothedCurrentNormalized = defaultValueNormalized;
            smoothingTargetNormalized = defaultValueNormalized;
        }

        protected void _UpdateTargetValue(float normalizedTargetValue)
        {
            // Log($"update target value {normalizedTargetValue}");
            var clampedPosRotEuler = Mathf.Lerp(MinPosOrRot, MaxPosOrRot, normalizedTargetValue);
            UpdateTargetIndicator(clampedPosRotEuler);
            var floatValue = Mathf.Lerp(MinValue, MaxValue, normalizedTargetValue);
            for (var i = 0; i < _targetValueFloatDrivers.Length; i++)
            {
                _targetValueFloatDrivers[i].UpdateFloatRescale(floatValue);
            }

            // immediate update
            if (!enableValueSmoothing)
            {
                // for (var i = 0; i < _floatDrivers.Length; i++)
                // {
                //     _floatDrivers[i].UpdateFloat(floatValue);
                // }
                for (var i = 0; i < _smoothedValueFloatDrivers.Length; i++)
                {
                    _smoothedValueFloatDrivers[i].UpdateFloatRescale(floatValue);
                }

                UpdateValueIndicator(clampedPosRotEuler);

                return;
            }

            // value smoothing
            if (!_valueInitialized)
            {
                smoothingTargetNormalized = normalizedTargetValue;
                smoothedCurrentNormalized = normalizedTargetValue;
                _lastFrameTime = Time.time;
                _valueInitialized = true;
            }
            else
            {
                smoothingTargetNormalized = normalizedTargetValue;
            }

            if (!_isSmoothing)
            {
                _isSmoothing = true;
                this.SendCustomEventDelayedFrames(
                    nameof(_OnValueSmoothedUpdate),
                    0
                );
            }
        }

        private float _velocity;

        public void _OnValueSmoothedUpdate()
        {
            // Log($"UpdateLoop {smoothedCurrentNormalized} => {smoothingTargetNormalized}");

            var currentFrameTime = Time.time;
            var deltaTime = currentFrameTime - _lastFrameTime;
            _lastFrameTime = currentFrameTime;

            if (IsCyclic)
            {
                // TODO: implement delta for 0-1 range to adjust target
                // var delta = Mathf.Repeat(smoothingTargetNormalized - smoothedCurrentNormalized, 1f);
                // if (delta > 0.5f)
                // {
                //     delta -= 1f;
                // }

                // Log($"cyclic smoothing current {smoothedCurrentNormalized}");
                // Log($"cyclic smoothing target  {smoothingTargetNormalized} + {delta}");

                // smoothedCurrentNormalized = Mathf.Lerp(
                //     smoothedCurrentNormalized + delta,
                //     smoothedCurrentNormalized,
                //     Mathf.Exp(-smoothingRate * deltaTime)
                // );
                // if (smoothedCurrentNormalized < 0f)
                // {
                //     smoothedCurrentNormalized += 1f;
                // }
                //
                // if (smoothedCurrentNormalized > 1f)
                // {
                //     smoothedCurrentNormalized -= 1f;
                // }

                var delta = Mathf.Repeat(
                    smoothingTargetNormalized - smoothedCurrentNormalized, 
                    1f
                );
                if (delta > 0.5f)
                {
                    delta -= 1f;
                }

                var target = smoothedCurrentNormalized + delta;

                smoothedCurrentNormalized = SmoothDamp(
                    current: smoothedCurrentNormalized,
                    target: target,
                    currentVelocity: ref _velocity,
                    smoothTime: smoothingTime,
                    maxSpeed: smoothingMaxSpeed,
                    deltaTime: deltaTime
                );

                smoothedCurrentNormalized = Mathf.Repeat(smoothedCurrentNormalized, 1f);
            }
            else
            {
                // smoothedCurrentNormalized = Mathf.Lerp(
                //     smoothingTargetNormalized,
                //     smoothedCurrentNormalized,
                //     Mathf.Exp(-smoothingRate * deltaTime)
                // );

                smoothedCurrentNormalized = SmoothDamp(
                    current: smoothedCurrentNormalized,
                    target: smoothingTargetNormalized,
                    currentVelocity: ref _velocity,
                    smoothTime: smoothingTime,
                    maxSpeed: smoothingMaxSpeed,
                    deltaTime: deltaTime
                );
            }

            if (!SyncedIsBeingManipulated &&
                Mathf.Abs(smoothingTargetNormalized - smoothedCurrentNormalized) <= Epsilon)
            {
                smoothedCurrentNormalized = smoothingTargetNormalized;
                Log($"value reached target {smoothingTargetNormalized}");
                _isSmoothing = false;
            }
            else
            {
                this.SendCustomEventDelayedFrames(
                    nameof(_OnValueSmoothedUpdate),
                    smoothingUpdateInterval
                );
            }

            var floatValue = Mathf.Lerp(MinValue, MaxValue, smoothedCurrentNormalized);
            for (var i = 0; i < _smoothedValueFloatDrivers.Length; i++)
            {
                _smoothedValueFloatDrivers[i].UpdateFloatRescale(floatValue);
            }

            UpdateValueIndicator(
                Mathf.Lerp(MinPosOrRot, MaxPosOrRot, smoothedCurrentNormalized)
            );
        }


        public virtual void Reset()
        {
            if (!IsAuthorized) return;
            Log("re-setting synced to default");

            SetValue(defaultValueNormalized);
        }

        public virtual void SetValue(float normalizedValue)
        {
            if (!IsAuthorized) return;
            SyncedValueNormalized = normalizedValue;
            // should already be done in OnDeserialization?
            _UpdateTargetValue(normalizedValue);
            if (synced)
            {
                TakeOwnership();
                RequestSerialization();
            }

            OnDeserialization();
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

        // // Gradually changes an angle given in degrees towards a desired goal angle over time.
        // public static float SmoothDamp01(
        //     float current,
        //     float target,
        //     ref float currentVelocity,
        //     float smoothTime,
        //     [DefaultValue("Mathf.Infinity")] float maxSpeed,
        //     [DefaultValue("Time.deltaTime")] float deltaTime
        // )
        // {
        //     target = current + Delta01(current, target);
        //     return SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
        // }
        //
        // public static float Delta01(float current, float target)
        // {
        //     float delta = Mathf.Repeat(target - current, 1f);
        //     if (delta > 0.5f)
        //         delta -= 1f;
        //     return delta;
        // }
        //
        // // Gradually changes an angle given in degrees towards a desired goal angle over time.
        // public static float SmoothDampAngle(
        //     float current,
        //     float target,
        //     ref float currentVelocity,
        //     float smoothTime,
        //     [DefaultValue("Mathf.Infinity")] float maxSpeed,
        //     [DefaultValue("Time.deltaTime")] float deltaTime
        // )
        // {
        //     target = current + DeltaAngle(current, target);
        //     return SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
        // }
        //
        // // Calculates the shortest difference between two given angles.
        // public static float DeltaAngle(float current, float target)
        // {
        //     float delta = Mathf.Repeat((target - current), 360.0F);
        //     if (delta > 180.0F)
        //         delta -= 360.0F;
        //     return delta;
        // }

        // private float prevDefaultNormalized, prevDefault, prevMin, prevMax;
        // private int lastHashSmoothedBase = 0;
        // ReSharper restore InconsistentNaming
#if UNITY_EDITOR && !COMPILER_UDONSHARP

        // public override bool OnPreprocess()
        // {
        //     FindDrivers();
        //
        //     return base.OnPreprocess();
        // }
        //
        // protected override void OnValidate()
        // {
        //     if (Application.isPlaying) return;
        //     base.OnValidate();
        //
        //     if (
        //         ValidationCache.ShouldRunValidation(
        //             this,
        //             HashCode.Combine(
        //                 MinPosOrRot,
        //                 MinPosOrRot,
        //                 MinValue,
        //                 MaxValue,
        //                 defaultValueNormalized,
        //                 defaultValue
        //             )
        //         )
        //     )
        //     {
        //         ApplyValues();
        //     }
        // }
        //
        // [ContextMenu("Apply Values")]
        // public virtual void ApplyValues()
        // {
        //     _EnsureInit();
        //
        //     //TODO move to helper script ?
        //     if (prevDefaultNormalized != defaultValueNormalized)
        //     {
        //         defaultValue = Mathf.Lerp(MinValue, MaxValue, defaultValueNormalized);
        //         prevDefault = defaultValue;
        //     }
        //
        //     if (prevDefault != defaultValue)
        //     {
        //         defaultValueNormalized = Mathf.InverseLerp(MinValue, MaxValue, defaultValue);
        //         // _normalizedDefault = defaultValueNormalized;
        //         prevDefaultNormalized = defaultValueNormalized;
        //     }
        //
        //     if (prevMin != MinValue || prevMax != MaxValue)
        //     {
        //         defaultValueNormalized = Mathf.InverseLerp(MinValue, MaxValue, defaultValue);
        //         // _normalizedDefault = defaultValueNormalized;
        //         prevDefaultNormalized = defaultValueNormalized;
        //
        //         prevMin = MinValue;
        //         prevMax = MaxValue;
        //     }
        //
        //     prevDefaultNormalized = defaultValueNormalized;
        //     prevDefault = defaultValue;
        //
        //     UpdateValueIndicator(
        //         Mathf.Lerp(MinPosOrRot, MaxPosOrRot, defaultValueNormalized)
        //     );
        //     UpdateTargetIndicator(
        //         Mathf.Lerp(MinPosOrRot, MaxPosOrRot, defaultValueNormalized)
        //     );
        //
        //     var minValue = Mathf.Min(MinValue, MaxValue);
        //     var maxValue = Mathf.Max(MinValue, MaxValue);
        //
        //     foreach (var valueFloatDriver in _smoothedValueFloatDrivers)
        //     {
        //         valueFloatDriver.ApplyFloatValue(
        //             Math.Clamp(defaultValue, minValue, maxValue)
        //         );
        //     }
        //
        //     foreach (var targetFloatDriver in _targetValueFloatDrivers)
        //     {
        //         targetFloatDriver.ApplyFloatValue(
        //             Math.Clamp(defaultValue, minValue, maxValue)
        //         );
        //     }
        // }

        internal void UpdateIndicatorsInEditor()
        {
            UpdateValueIndicator(
                Mathf.Lerp(MinPosOrRot, MaxPosOrRot, defaultValueNormalized)
            );
            UpdateTargetIndicator(
                Mathf.Lerp(MinPosOrRot, MaxPosOrRot, defaultValueNormalized)
            );
        }
#endif
    }
}