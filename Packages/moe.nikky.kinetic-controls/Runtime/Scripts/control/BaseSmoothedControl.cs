using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using nikkyai.ArrayExtensions;
using nikkyai.common;
using nikkyai.Utils;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace nikkyai.control
{
    public abstract class BaseSmoothedControl : BaseSyncedControl
    {
        // eh just add a trigger on the button that triggered the reset ?
        // protected TriggerDriver[] ResetTriggerDriver = { };

        protected abstract float MinPosOrRot
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }

        protected abstract float MaxPosOrRot
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }

        // protected abstract float MinValue { 
        //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //     get;
        // }
        //
        // protected abstract float MaxValue {  
        //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //     get;
        // }

        // protected abstract bool TargetIsBeingManipulated
        // {
        //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //     get;
        //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //     set;
        // }

        #region default value

        [Header("Base Smoothed Control")]
        [FormerlySerializedAs("range")]
        [SerializeField]
        [Description("The range of values that this behaviour will send to any attached float drivers")]
        private Vector2 outputRange = new Vector2(0, 1);

        [SerializeField, Range(0, 1)] internal float defaultValueNormalized = 0.25f;
        [SerializeField] internal float defaultValue = 0;

        protected float MinValue => outputRange.x;
        protected float MaxValue => outputRange.y;

        #endregion

        #region drivers

        [Header("Base Smoothed Control - Drivers")] // header
        [FormerlySerializedAs("floatTargetValueDriversTransform")]
        [FormerlySerializedAs("floatTargetDriversTransform")]
        [FormerlySerializedAs("floatTargetDrivers")]
        [SerializeField] //
        [InspectorName("target value drivers")]
        private Transform floatTargetValueDrivers;

        [FormerlySerializedAs("floatSmoothedValueDriversTransform")]
        [FormerlySerializedAs("floatValueDriversTransform")]
        [FormerlySerializedAs("floatValueDrivers")]
        [SerializeField] //
        [InspectorName("smoothed value drivers")]
        private Transform floatSmoothedValueDrivers;

        private FloatDriver[] _targetValueFloatDrivers = { };
        private FloatDriver[] _smoothedValueFloatDrivers = { };

        #endregion

        #region value smoothing

        [Header("Base Smoothed Control - Smoothing")] // header
        [Tooltip("smoothes out value updates over time, may impact CPU frametimes"),
         SerializeField]
        private bool enableValueSmoothing = true;

        public bool ValueSmoothing
        {
            get => enableValueSmoothing;
            set
            {
                enableValueSmoothing = value;
                // UpdateSmoothing();
            }
        }

        [Tooltip("amount of frames to skip when approaching target value," +
                 "higher number == less load, but more choppy smoothing"),
         SerializeField, Range(1, 10)]
        private int smoothingUpdateInterval = 3;

        public int SmoothingFrames
        {
            get => smoothingUpdateInterval;
            set => smoothingUpdateInterval = value;
        }

        [Tooltip("fraction of the distance covered within roughly 1s"),
         SerializeField, Min(0.05f),]
        private float smoothingRate = 0.5f;

        public float SmoothingRate
        {
            get => smoothingRate;
            set => smoothingRate = value;
        }

        protected float smoothingTargetNormalized;
        protected float smoothedCurrentNormalized;

        protected bool isCyclic = false;

        private const float epsilon = 0.005f;
        private bool valueInitialized = false;
        private bool isSmoothing = false;
        private float lastFrameTime = 0;

        #endregion


        [UdonSynced]
        // IMPORTANT, DO NOT DELETE
        protected float SyncedValueNormalized;

        [UdonSynced] // IMPORTANT, DO NOT DELETE
        protected bool SyncedIsBeingManipulated;

        protected bool TargetIsBeingManipulated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => SyncedIsBeingManipulated;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SyncedIsBeingManipulated = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract void UpdateTargetIndicator(float clampedPosOrRotEuler);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract void UpdateValueIndicator(float clampedPosOrRotEuler);

        // protected void InitValueSmoothing()
        // {
        //     // NOTE: maybe we can get away without it ?
        //     // smoothedCurrentNormalized = _normalizedDefault;
        //     // smoothingTargetNormalized = _normalizedDefault;
        //     enableValueSmoothing = enableValueSmoothing && smoothingUpdateInterval > 0;
        // }
        protected virtual void SetupValuesAndComponents()
        {
            //TODO: move into running in editor ?
            Log($"Searching for float value drivers in {floatSmoothedValueDrivers}");
            if (Utilities.IsValid(floatSmoothedValueDrivers))
            {
                _smoothedValueFloatDrivers = gameObject.GetComponents<FloatDriver>()
                    .AddRange(
                        floatSmoothedValueDrivers.GetComponentsInChildren<FloatDriver>()
                    );
                Log($"found {_smoothedValueFloatDrivers.Length} drivers for value");
            }
            else
            {
                LogError("missing transform for float value drivers");
            }

            Log($"Searching for float target drivers in {floatTargetValueDrivers}");
            if (Utilities.IsValid(floatTargetValueDrivers))
            {
                _targetValueFloatDrivers = floatTargetValueDrivers.GetComponentsInChildren<FloatDriver>();
                Log($"found {_targetValueFloatDrivers.Length} drivers for target");
            }
            else
            {
                LogError("missing transform for float target drivers");
            }

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
            SetupValuesAndComponents();
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
            if (!valueInitialized)
            {
                smoothingTargetNormalized = normalizedTargetValue;
                smoothedCurrentNormalized = normalizedTargetValue;
                lastFrameTime = Time.time;
                valueInitialized = true;
            }
            else
            {
                smoothingTargetNormalized = normalizedTargetValue;
            }

            if (!isSmoothing)
            {
                isSmoothing = true;
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
            var deltaTime = currentFrameTime - lastFrameTime;
            lastFrameTime = currentFrameTime;

            if (isCyclic)
            {
                //TODO: implement delta for 0-1 range to adjust target
                var delta = Mathf.Repeat(smoothingTargetNormalized - smoothedCurrentNormalized,
                    1f); // (smoothingTargetNormalized - smoothedCurrentNormalized) % 1f;
                if (delta > 0.5f)
                {
                    delta -= 1f;
                }

                // Log($"cyclic smoothing current {smoothedCurrentNormalized}");
                // Log($"cyclic smoothing target  {smoothingTargetNormalized} + {delta}");

                smoothedCurrentNormalized = Mathf.Lerp(
                    smoothedCurrentNormalized + delta,
                    smoothedCurrentNormalized,
                    Mathf.Exp(-smoothingRate * deltaTime)
                );
                if (smoothedCurrentNormalized < 0f)
                {
                    smoothedCurrentNormalized += 1f;
                }

                if (smoothedCurrentNormalized > 1f)
                {
                    smoothedCurrentNormalized -= 1f;
                }
                //TODO: use modulo after to get value in expected range

                // smoothedCurrentNormalized %= 1f;

                // smoothedCurrentNormalized = Mathf.SmoothDampAngle(
                //     current: smoothedCurrentNormalized * 360f,
                //     target: smoothingTargetNormalized * 360f,
                //     currentVelocity: ref _velocity,
                //     smoothTime: 1f / smoothingRate, 
                //     maxSpeed: 10f,
                //     deltaTime: deltaTime
                // ) / 360f;
            }
            else
            {
                smoothedCurrentNormalized = Mathf.Lerp(
                    smoothingTargetNormalized,
                    smoothedCurrentNormalized,
                    Mathf.Exp(-smoothingRate * deltaTime)
                );
            }

            if (!TargetIsBeingManipulated &&
                Mathf.Abs(smoothingTargetNormalized - smoothedCurrentNormalized) <= epsilon)
            {
                smoothedCurrentNormalized = smoothingTargetNormalized;
                Log($"value reached target {smoothingTargetNormalized}");
                isSmoothing = false;
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
            if (!isAuthorized) return;
            Log("re-setting synced to default");

            SetValue(defaultValueNormalized);
        }

        // public abstract void SetValue(float normalizedValue);

        public virtual void SetValue(float normalizedValue)
        {
            if (!isAuthorized) return;
            SyncedValueNormalized = normalizedValue;
            // should already be done in OnDeserialization?
            _UpdateTargetValue(normalizedValue);
            if (Synced)
            {
                TakeOwnership();
                RequestSerialization();
            }

            OnDeserialization();
        }

        private float prevDefaultNormalized, prevDefault;
        private int lastHashSmoothedBase = 0;
        // ReSharper restore InconsistentNaming
#if UNITY_EDITOR && !COMPILER_UDONSHARP

        protected override void OnValidate()
        {
            if (Application.isPlaying) return;
            base.OnValidate();

            if (
                ValidationCache.ShouldRunValidation(
                    this,
                    HashCode.Combine(
                        MinPosOrRot,
                        MinPosOrRot,
                        MinValue,
                        MaxValue,
                        defaultValueNormalized,
                        defaultValue
                    )
                )
            )
            {
                ApplyValues();
            }
        }

        [ContextMenu("Apply Values")]
        public virtual void ApplyValues()
        {
            SetupValuesAndComponents();

            //TODO move to helper script ?
            if (prevDefaultNormalized != defaultValueNormalized)
            {
                defaultValue = Mathf.Lerp(MinValue, MaxValue, defaultValueNormalized);
                prevDefault = defaultValue;
            }

            if (prevDefault != defaultValue)
            {
                defaultValueNormalized = Mathf.InverseLerp(MinValue, MaxValue, defaultValue);
                // _normalizedDefault = defaultValueNormalized;
                prevDefaultNormalized = defaultValueNormalized;
            }

            prevDefaultNormalized = defaultValueNormalized;
            prevDefault = defaultValue;

            UpdateValueIndicator(
                Mathf.Lerp(MinPosOrRot, MaxPosOrRot, defaultValueNormalized)
            );
            UpdateTargetIndicator(
                Mathf.Lerp(MinPosOrRot, MaxPosOrRot, defaultValueNormalized)
            );

            var minValue = Mathf.Min(MinValue, MaxValue);
            var maxValue = Mathf.Max(MinValue, MaxValue);
            
            foreach (var valueFloatDriver in _smoothedValueFloatDrivers)
            {
                valueFloatDriver.ApplyFloatValue(
                    Math.Clamp(defaultValue, minValue, maxValue)
                );
            }

            foreach (var targetFloatDriver in _targetValueFloatDrivers)
            {
                targetFloatDriver.ApplyFloatValue(
                    Math.Clamp(defaultValue, minValue, maxValue)
                );
            }
        }
#endif
    }
}