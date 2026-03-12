using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace nikkyai.common
{
    public abstract class BaseSmoothedBehaviour: BaseSyncedBehaviour
    {
        protected FloatDriver[] TargetFloatDrivers = { };
        protected FloatDriver[] ValueFloatDrivers = { };

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

        protected bool isAngle = false;
        
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
            var clampedPosRotEuler = Mathf.Lerp(MinPosOrRot, MaxPosOrRot, normalizedTargetValue);
            UpdateTargetIndicator(clampedPosRotEuler);
            var floatValue = Mathf.Lerp(MinValue, MaxValue, normalizedTargetValue);
            for (var i = 0; i < TargetFloatDrivers.Length; i++)
            {
                TargetFloatDrivers[i].UpdateFloat(floatValue);
            }

            // immediate update
            if (!enableValueSmoothing)
            {
                // for (var i = 0; i < _floatDrivers.Length; i++)
                // {
                //     _floatDrivers[i].UpdateFloat(floatValue);
                // }
                for (var i = 0; i < ValueFloatDrivers.Length; i++)
                {
                    ValueFloatDrivers[i].UpdateFloat(floatValue);
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

            if (isAngle)
            {
                //TODO: implement delta for 0-1 range to adjust target
                var delta = Mathf.Repeat(smoothingTargetNormalized - smoothedCurrentNormalized, 1f); // (smoothingTargetNormalized - smoothedCurrentNormalized) % 1f;
                if (delta > 0.5f)
                {
                    delta -= 1f;
                }
                
                Log($"radial smoothing current {smoothedCurrentNormalized}");
                Log($"radial smoothing target  {smoothingTargetNormalized} + {delta}");
                
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
                // if (smoothedCurrentNormalized < 0f)
                // {
                //     smoothedCurrentNormalized += 1f;
                // }
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
            for (var i = 0; i < ValueFloatDrivers.Length; i++)
            {
                ValueFloatDrivers[i].UpdateFloat(floatValue);
            }

            UpdateValueIndicator(
                Mathf.Lerp(MinPosOrRot, MaxPosOrRot, smoothedCurrentNormalized)
            );
        }
        
        
        public virtual void Reset()
        {
            
        }

        public virtual void SetValue(float normalizedValue)
        {
            
        }
    }
}