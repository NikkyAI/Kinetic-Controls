using System;
using System.ComponentModel;
using nikkyai.common;
using Texel;
using UdonSharp;
using UnityEngine;
using VRC;
using VRC.Dynamics;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon.Common;

// ReSharper disable ForCanBeConvertedToForeach

namespace nikkyai.Kinetic_Controls
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Fader : BaseKineticControl
    {
        [Header("Fader")] // header
        [SerializeField]
        private Axis axis = Axis.Y;

        [SerializeField, InspectorName("output range")]
        private Vector2 range = new Vector2(0, 1);
        [SerializeField] private float defaultValue = 0.25f;
        private float _normalizedDefault;

        [Header("VR")] // header
        [SerializeField, Description("switches between finger contacts and pickup")]
        private bool useContactsInVR = true;
        protected override bool UseContactsInVR => useContactsInVR;

        [Header("Desktop")] // header
        [SerializeField, Range(5, 90)]
        private float minLookAngle = 30f;
        
        [Header("Components")] //
        [SerializeField] private Handle faderHandle;
        protected override Handle Handle => faderHandle;
        
        private Vector3 _axisVector = Vector3.zero;

        [InspectorName("minPosition"),
         SerializeField]
        private Transform minLimit;

        [InspectorName("maxPosition"),
         SerializeField]
        private Transform maxLimit;

        [SerializeField] private Transform valueIndicator;

        [SerializeField] private Transform targetIndicator;
        
        // [Header("VR")] // header
        // [SerializeField, Description("switches between finger contacts and pickup")]
        // private bool useContactsInVR = true;


        [Header("Drivers")] // header

        [SerializeField] private Transform floatValueDrivers;

        [SerializeField] private Transform floatTargetDrivers;

        [SerializeField] private Transform boolAuthorizedDrivers;


        protected override string LogPrefix => $"{nameof(Fader)} {name}";

        private BoolDriver[] _isAuthorizedBoolDrivers = { };

        protected override Transform HandleReset => targetIndicator;
        
        // internal values

        private float _minPos, _maxPos;
        protected override float MinPosOrRot => _minPos;
        protected override float MaxPosOrRot => _maxPos;

        private float _minValue, _maxValue;
        protected override float MinValue => _minValue;
        protected override float MaxValue => _maxValue;

        private void Start()
        {
            _EnsureInit();
        }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetupValuesAndComponents()
        {
// #if UNITY_EDITOR && !COMPILER_UDONSHARP
//                 isUdon = false;
// #endif
            _axisVector[(int)axis] = 1;
            _minValue = range.x;
            _maxValue = range.y;
            _minPos = minLimit.localPosition[(int)axis];
            _maxPos = maxLimit.localPosition[(int)axis];
            _normalizedDefault = Mathf.InverseLerp(_minValue, _maxValue, defaultValue);

            SyncedValueNormalized = _normalizedDefault;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
            minLimit.transform.MarkDirty();
            maxLimit.transform.MarkDirty();
#endif

            smoothedCurrentNormalized = _normalizedDefault;
            smoothingTargetNormalized = _normalizedDefault;
            // enableValueSmoothing = enableValueSmoothing && smoothingUpdateInterval > 0;

            //TODO: move into running in editor ?
            ValueFloatDrivers = floatValueDrivers.GetComponentsInChildren<FloatDriver>();
            TargetFloatDrivers = floatTargetDrivers.GetComponentsInChildren<FloatDriver>();

            if (boolAuthorizedDrivers != null)
            {
                _isAuthorizedBoolDrivers = boolAuthorizedDrivers.GetComponentsInChildren<BoolDriver>();
            }

            if (ValueFloatDrivers != null)
            {
                Log($"found {ValueFloatDrivers.Length} drivers for value");
            }

            if (TargetFloatDrivers != null)
            {
                Log($"found {TargetFloatDrivers.Length} drivers for target");
            }
        }

        protected override void _Init()
        {
            base._Init();
            SetupValuesAndComponents();

            OnDeserialization();
            UpdateValueIndicator(
                Mathf.Lerp(_minPos, _maxPos, smoothedCurrentNormalized)
            );
            UpdateTargetIndicator(
                Mathf.Lerp(_minPos, _maxPos, smoothingTargetNormalized)
            );
            UpdateHandlePosition();

            // pickup.transform.SetPositionAndRotation(pickupReset.position, pickupReset.rotation);
        }

        protected override void AccessChanged()
        {
            for (var i = 0; i < _isAuthorizedBoolDrivers.Length; i++)
            {
                _isAuthorizedBoolDrivers[i].UpdateBool(isAuthorized);
            }
        }

        public override void Reset()
        {
            if (!isAuthorized) return;
            SyncedValueNormalized = _normalizedDefault;
            if (synced)
            {
                RequestSerialization();
            }

            OnDeserialization();
        }

        public override void SetValue(float normalizedValue)
        {
            if (!isAuthorized) return;
            SyncedValueNormalized = normalizedValue;
            if (synced)
            {
                RequestSerialization();
            }

            OnDeserialization();
        }

        protected override float RelativePosToNormalized(Vector3 relativePos)
        {
            var clampedPos = Mathf.Clamp(
                relativePos[(int)axis],
                _minPos,
                _maxPos
            );

            // UpdateIndicatorPosition(clampedPos);

            return Mathf.InverseLerp(
                _minPos,
                _maxPos,
                clampedPos
            );
        }

        protected override void FollowPickup()
        {
            if (IsInVR)
            {
                SyncedValueNormalized = RelativePosToNormalized(Pickup.transform.localPosition);
            }
            else
            {
                var trackingData = LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                var inPoint = transform.position;
                var planeDirection = trackingData.position - inPoint;
                planeDirection[(int)axis] = 0;

                var plane = new Plane(
                    Vector3.Normalize(planeDirection),
                    inPoint
                );
                // var lookDirection = trackingData.rotation * Vector3.forward;
                var lookDirection = Vector3.Normalize(Pickup.transform.position - trackingData.position);
                var ray = new Ray(
                    trackingData.position,
                    lookDirection
                );

                if (plane.Raycast(ray, out var distance))
                {
                    var lookDirectionAxisLocked = lookDirection;
                    lookDirectionAxisLocked[(int)axis] = 0;
                    var angle = Vector3.Angle(lookDirectionAxisLocked, -planeDirection);
                    // Log($"angle: {angle}");
                    if (angle < minLookAngle)
                    {
                        var hitPosition = ray.GetPoint(distance);
                        // Log($"raycast hit, distance: {distance}, point: {hitPosition}");
                        var localHit = transform.InverseTransformPoint(hitPosition);

                        if (Utilities.IsValid(debugRaytrace))
                        {
                            debugRaytrace.gameObject.SetActive(true);
                            debugRaytrace.position = hitPosition;
                        }
                        SyncedValueNormalized = RelativePosToNormalized(localHit);
                    }
                    else
                    {
                        if (Utilities.IsValid(debugRaytrace))
                        {
                            debugRaytrace.gameObject.SetActive(false);
                        }
                    }
                }
                else
                {
                    if (Utilities.IsValid(debugRaytrace))
                    {
                        debugRaytrace.gameObject.SetActive(false);
                    }
                }
            }
        }
        
        protected override void UpdateTargetIndicator(float clampedPos)
        {
            // if (!enableValueSmoothing) return;
            if (!Utilities.IsValid(targetIndicator)) return;
            Vector3 newPos = targetIndicator.transform.localPosition;
            newPos[(int)axis] = clampedPos;
            targetIndicator.transform.localPosition = newPos;

            UpdateHandlePosition();

#if UNITY_EDITOR && !COMPILER_UDONSHARP
            targetIndicator.transform.MarkDirty();
#endif
        }

        protected override void UpdateValueIndicator(float clampedPos)
        {
            if (!Utilities.IsValid(valueIndicator)) return;
            Vector3 newPos = valueIndicator.transform.localPosition;
            newPos[(int)axis] = clampedPos;
            valueIndicator.transform.localPosition = newPos;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
            valueIndicator.transform.MarkDirty();
#endif
        }

        // public override void OnDeserialization()
        // {
        //     if (SyncedValueNormalized != LastSyncedValueNormalized)
        //     {
        //         _UpdateTargetValue(SyncedValueNormalized);
        //
        //         LastSyncedValueNormalized = SyncedValueNormalized;
        //     }
        // }

        // ReSharper disable InconsistentNaming
        [NonSerialized] private float
            prevMinPos = float.NaN,
            prevMaxPos = float.NaN,
            prevMinValue = float.NaN,
            prevMaxValue = float.NaN,
            prevDefault = float.NaN;


        [NonSerialized] private AccessControl prevAccessControl;
        [NonSerialized] private bool prevEnforceACL;
        [NonSerialized] private DebugLog prevDebugLog;
        [NonSerialized] private bool childrenInitialized = false;
        // ReSharper restore InconsistentNaming
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        private void OnValidate()
        {
            if (Application.isPlaying) return;
            UnityEditor.EditorUtility.SetDirty(this);

            if (
                prevMinPos != _minPos ||
                prevMaxPos != _maxPos ||
                prevMinValue != range.x ||
                prevMaxValue != range.y ||
                prevDefault != defaultValue
            )
            {
                ApplyValues();

                prevMinPos = _minPos;
                prevMaxPos = _maxPos;
                prevMinValue = range.x;
                prevMaxValue = range.y;
                prevDefault = defaultValue;
            }

            if (prevAccessControl != AccessControl
                || prevEnforceACL != EnforceACL
                || prevDebugLog != DebugLog
               )
            {
                ApplyACLsAndLog();
                prevAccessControl = AccessControl;
                prevDebugLog = DebugLog;
            }
        }

        [ContextMenu("Apply Values")]
        public override void ApplyValues()
        {
            base.ApplyValues();
            SetupValuesAndComponents();
            // SetupFaderHandle();
            // SetupPickup();
            // SetupPickupRigidBody();

            OnDeserialization();

            UpdateValueIndicator(
                Mathf.Lerp(_minPos, _maxPos, smoothedCurrentNormalized)
            );
            UpdateTargetIndicator(
                Mathf.Lerp(_minPos, _maxPos, smoothingTargetNormalized)
            );
            UpdateHandlePosition();

            foreach (var valueFloatDriver in ValueFloatDrivers)
            {
                valueFloatDriver.ApplyFloatValue(
                    Math.Clamp(defaultValue, _minValue, _maxValue)
                );
            }

            foreach (var targetFloatDriver in TargetFloatDrivers)
            {
                targetFloatDriver.ApplyFloatValue(
                    Math.Clamp(defaultValue, _minValue, _maxValue)
                );
            }
        }

        [ContextMenu("Apply ACLs and Log")]
        private void ApplyACLsAndLog()
        {
            faderHandle.EditorACL = AccessControl;
            faderHandle.EditorDebugLog = DebugLog;
            faderHandle.EditorEnforceACL = EnforceACL;
            faderHandle.MarkDirty();
        }
#endif
    }
}