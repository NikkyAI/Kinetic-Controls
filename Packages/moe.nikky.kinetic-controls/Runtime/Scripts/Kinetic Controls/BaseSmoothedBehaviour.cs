using System.Runtime.CompilerServices;
using nikkyai.common;
using nikkyai.driver;
using UnityEngine;

namespace nikkyai.Kinetic_Controls
{
    public abstract class BaseSmoothedBehaviour: BaseSyncedBehaviour
    {
        protected FloatDriver[] _targetFloatDrivers = { };
        protected FloatDriver[] _valueFloatDrivers = { };

        protected abstract float MinPosOrRot { 
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }

        protected abstract float MaxPosOrRot {  
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }

        protected abstract float MinValue { 
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }

        protected abstract float MaxValue {  
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }

        protected abstract bool TargetIsBeingManipulated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set;
        }
        
        
        #region value smoothing

        [Header("Smoothing")] // header
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
         SerializeField, Min(1)]
        private int smoothingUpdateInterval = 3;

        public int SmoothingFrames
        {
            get => smoothingUpdateInterval;
            set { smoothingUpdateInterval = value; }
        }
        
        [Tooltip("fraction of the distance covered within roughly 1s"),
         SerializeField]
        private float smoothingRate = 0.5f;

        public float SmoothingRate
        {
            get => smoothingRate;
            set { smoothingRate = value; }
        }
        protected float smoothingTargetNormalized;
        protected float smoothedCurrentNormalized;
        
        private const float epsilon = 0.005f;
        private bool valueInitialized = false;
        private bool isSmoothing = false;
        private float lastFrameTime = 0;

        #endregion

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
        
        protected void _UpdateTargetValue(float normalizedTargetValue)
        {
            // Log($"update target value {normalizedTargetValue}");
            var clampedRotEuler = Mathf.Lerp(MinPosOrRot, MaxPosOrRot, normalizedTargetValue);
            UpdateTargetIndicator(clampedRotEuler);
            for (var i = 0; i < _targetFloatDrivers.Length; i++)
            {
                _targetFloatDrivers[i].UpdateFloat(normalizedTargetValue);
            }

            // immediate update
            if (!enableValueSmoothing)
            {
                var floatValue = Mathf.Lerp(MinValue, MaxValue, normalizedTargetValue);
                // for (var i = 0; i < _floatDrivers.Length; i++)
                // {
                //     _floatDrivers[i].UpdateFloat(floatValue);
                // }
                for (var i = 0; i < _valueFloatDrivers.Length; i++)
                {
                    _valueFloatDrivers[i].UpdateFloat(floatValue);
                }

                UpdateValueIndicator(clampedRotEuler);

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

        public void _OnValueSmoothedUpdate()
        {
            // Log($"UpdateLoop {smoothedCurrentNormalized} => {smoothingTargetNormalized}");

            var currentFrameTime = Time.time;
            var deltaTime = currentFrameTime - lastFrameTime;
            lastFrameTime = currentFrameTime;

            smoothedCurrentNormalized = Mathf.Lerp(
                smoothingTargetNormalized,
                smoothedCurrentNormalized,
                Mathf.Exp(-smoothingRate * deltaTime)
            );

            if (!TargetIsBeingManipulated && Mathf.Abs(smoothingTargetNormalized - smoothedCurrentNormalized) <= epsilon)
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
            // for (var i = 0; i < _floatDrivers.Length; i++)
            // {
            //     _floatDrivers[i].UpdateFloat(floatValue);
            // }
            for (var i = 0; i < _valueFloatDrivers.Length; i++)
            {
                _valueFloatDrivers[i].UpdateFloat(floatValue);
            }

            UpdateValueIndicator(
                Mathf.Lerp(MinPosOrRot, MaxPosOrRot, smoothedCurrentNormalized)
            );
        }
    }
}