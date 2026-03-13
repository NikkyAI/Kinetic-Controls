using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using nikkyai.driver;
using nikkyai.ArrayExtensions;
using nikkyai.common;
using Texel;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon.Serialization.OdinSerializer;

// ReSharper disable ForCanBeConvertedToForeach

namespace nikkyai.Kinetic_Controls
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Lever : BaseKineticControl
    {
        [Header("Lever")] // header
        [SerializeField]
        private Axis axis = Axis.Z;

        private Vector3 _forwardVector = Vector3.forward;

        [SerializeField, InspectorName("output range")]
        private Vector2 range = new Vector2(0, 1);

        [SerializeField] private float defaultValue = 0.25f;

        [Range(-180, 180), SerializeField, PreviouslySerializedAs("_minRot")]
        private float minRot = -45;

        [Range(-180, 180), SerializeField, PreviouslySerializedAs("_maxRot")]
        private float maxRot = 45;

        protected override float MinPosOrRot => minRot;
        protected override float MaxPosOrRot => maxRot;

        private float _normalizedDefault;
        // [SerializeField] private PickupTrigger pickupTrigger;

        [Header("VR")] // header
        [SerializeField, Description("switches between finger contacts and pickup")]
        private bool useContactsInVR = true;
        protected override bool UseContactsInVR => useContactsInVR;

        [Header("Components")] //
        [SerializeField] private Handle leverHandle;
        protected override Handle Handle => leverHandle;
        
        [Tooltip("should be the same as targetIndicator or a child")] //
        [SerializeField]
        private Transform pickupReset;

        private Vector3 _axisVector = Vector3.zero;

        [FormerlySerializedAs("minRotation"), // force newline
         InspectorName("minRotation"),
         SerializeField]
        private Transform minLimit;

        [FormerlySerializedAs("maxRotation"), // force newline
         InspectorName("maxRotation"),
         SerializeField]
        private Transform maxLimit;

        [FormerlySerializedAs("valueRotation"), // force newline
         InspectorName("valueRotation"),
         SerializeField]
        private Transform valueIndicator;

        [FormerlySerializedAs("targetRotation"), // force newline
         InspectorName("targetRotation"),
         SerializeField]
        private Transform targetIndicator;

        [Header("Drivers")] // header
        
        [SerializeField] private Transform floatValueDrivers;

        [SerializeField] private Transform floatTargetDrivers;
        
        [SerializeField] private Transform boolAuthorizedDrivers;
        
        private Rigidbody _rigidbody;

        protected override string LogPrefix => $"{nameof(Lever)} {name}";

        private BoolDriver[] _isAuthorizedBoolDrivers = { };
        
        protected override Transform HandleReset => pickupReset;
        
        // internal values

        private float _minValue, _maxValue;
        protected override float MinValue => _minValue;
        protected override float MaxValue => _maxValue;

        // private VRC_Pickup _pickup;
        // private Rigidbody _pickupRigidBody;
        // private float _lastValue;
        // private bool _isHeldLocally;

        private void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            base._Init();
            Log("Init");
            SetupValuesAndComponents();

            if (pickupReset == null)
            {
                LogError("missing pickup reset transform");
            }

            OnDeserialization();
            UpdateValueIndicator(
                Mathf.Lerp(minRot, maxRot, smoothedCurrentNormalized)
            );
            UpdateTargetIndicator(
                Mathf.Lerp(minRot, maxRot, smoothingTargetNormalized)
            );
            UpdateHandlePosition();

            Log("Init Done");
            // pickup.transform.SetPositionAndRotation(pickupReset.position, pickupReset.rotation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetupValuesAndComponents()
        {
            Log("SetupValuesAndComponents");
            _axisVector = Vector3.zero;
            _axisVector[(int)axis] = 1;
            if (axis == Axis.X)
            {
                _forwardVector = Vector3.up;
            }
            else if (axis == Axis.Y)
            {
                _forwardVector = Vector3.right;
            }
            else if (axis == Axis.Z)
            {
                _forwardVector = Vector3.right;
            }
            else
            {
                LogError("Invalid axis");
            }

            // _updateFloatSynced = UpdateFloatSynced;
            _minValue = range.x;
            _maxValue = range.y;
            _normalizedDefault = Mathf.InverseLerp(_minValue, _maxValue, defaultValue);

            SyncedValueNormalized = _normalizedDefault;

            if (minLimit)
            {
                minLimit.localRotation = Quaternion.AngleAxis(minRot, _axisVector);
            }
            else
            {
                LogError("minLimit is not set");
            }

            if (maxLimit)
            {
                maxLimit.localRotation = Quaternion.AngleAxis(maxRot, _axisVector);
            }
            else
            {
                LogError("maxLimit is not set");
            }

            LocalPlayer = Networking.LocalPlayer;
            if (Utilities.IsValid(LocalPlayer))
            {
                IsInVR = LocalPlayer.IsUserInVR();
            }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
            minLimit.transform.MarkDirty();
            maxLimit.transform.MarkDirty();
#endif

            smoothedCurrentNormalized = _normalizedDefault;
            smoothingTargetNormalized = _normalizedDefault;
            // enableValueSmoothing = enableValueSmoothing && smoothingUpdateInterval > 0;

            isCyclic = Mathf.Approximately(minRot, -180f) && Mathf.Approximately(maxRot, 180f);
            
            //TODO: move into running in editor ?
            Log($"Searching for float value drivers in {floatValueDrivers}");
            if (floatValueDrivers != null && Utilities.IsValid(floatValueDrivers))
            {
                Log("GetComponentsInChildren float value drivers");
                ValueFloatDrivers = floatValueDrivers.GetComponentsInChildren<FloatDriver>();
                Log($"found {ValueFloatDrivers.Length} drivers for value");
            }
            else
            {
                LogError("missing transform for float value drivers");
            }
            Log($"Searching for float target drivers in {floatTargetDrivers}");
            if (floatTargetDrivers != null && Utilities.IsValid(floatTargetDrivers))
            {
                Log("GetComponentsInChildren float target drivers");
                TargetFloatDrivers = floatTargetDrivers.GetComponentsInChildren<FloatDriver>();
                Log($"found {TargetFloatDrivers.Length} drivers for target");
            }
            else
            {
                LogError("missing transform for float target drivers");
            }

            if (Utilities.IsValid(boolAuthorizedDrivers))
            {
                Log($"Searching for bool authorized drivers in {boolAuthorizedDrivers}");
                _isAuthorizedBoolDrivers = boolAuthorizedDrivers.GetComponentsInChildren<BoolDriver>();
                Log($"found {_isAuthorizedBoolDrivers.Length} drivers for authorized");
            }
            // if (Utilities.IsValid(ValueFloatDrivers))
            // {
            //     Log($"found {ValueFloatDrivers.Length} drivers for value");
            // }
            //
            // if (Utilities.IsValid(TargetFloatDrivers))
            // {
            //     Log($"found {TargetFloatDrivers.Length} drivers for target");
            // }
        }

        protected override void AccessChanged()
        {
            Pickup.pickupable = isAuthorized;
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
            relativePos[(int)axis] = 0;

            var angle = Vector3.SignedAngle(_forwardVector, relativePos, _axisVector);
            Log($"forwardVector: {_forwardVector}");
            Log($"axisVector: {_axisVector}");
            Log($"relativePos: {relativePos}");
            Log($"angle: {angle}");

            // UpdateIndicatorPosition(clampedPos);

            var normalized = Mathf.InverseLerp(
                a: minRot,
                b: maxRot,
                value: angle
            );
            Log($"InverseLerp: {minRot} .. {maxRot}");
            Log($"normalized: {normalized}");
            return normalized;
        }


        protected override void FollowPickup()
        {
            if (IsInVR)
            {
                var relativePos = transform.InverseTransformPoint(Pickup.transform.position);
                SyncedValueNormalized = RelativePosToNormalized(relativePos);
            }
            else
            {
                var trackingData = LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                var inPoint = transform.position;
                var planeDirection = Vector3.zero;
                //TODO: get correct side facing towards player
                planeDirection[(int)axis] = 1;

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
                    // var angle = Vector3.Angle(lookDirectionAxisLocked, -planeDirection);
                    // // Log($"angle: {angle}");
                    // if (angle < 45)
                    // {
                        var hitPosition = ray.GetPoint(distance);
                        Log($"raycast hit, distance: {distance}, point: {hitPosition}");
                        var localHit = transform.InverseTransformPoint(hitPosition);

                        if (Utilities.IsValid(debugRaytrace))
                        {
                            debugRaytrace.gameObject.SetActive(true);
                            debugRaytrace.position = hitPosition;
                        }
                        SyncedValueNormalized = RelativePosToNormalized(localHit);
                    // }
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


        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void UpdateTargetIndicator(float clampedRotEuler)
        {
            // if (!enableValueSmoothing) return;
            if (!Utilities.IsValid(targetIndicator)) return;

            // Vector3 newRot = targetIndicator.localEulerAngles;
            // newRot[(int)axis] = clampedRotEuler;
            // targetIndicator.localRotation = Quaternion.Euler(newRot);
            targetIndicator.localRotation = Quaternion.AngleAxis(clampedRotEuler, _axisVector);

            if (!PickupHasObjectSync && !IsHeldLocally)
            {
                UpdateHandlePosition();
            }
#if UNITY_EDITOR && !COMPILER_UDONSHARP
            targetIndicator.transform.MarkDirty();
#endif
        }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void UpdateValueIndicator(float clampedRotEuler)
        {
            if (!Utilities.IsValid(valueIndicator)) return;

            // Vector3 newRot = valueIndicator.localEulerAngles;
            // newRot[(int)axis] = clampedRotEuler;
            // valueIndicator.localRotation = Quaternion.Euler(newRot);
            valueIndicator.localRotation = Quaternion.AngleAxis(clampedRotEuler, _axisVector);

            // if (!_pickupHasObjectSync && !_isHeldLocally)
            // {
            //     UpdatePickupPosition();
            // }
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
            prevMinRot,
            prevMaxRot,
            prevMinValue,
            prevMaxValue,
            prevDefault;

        [NonSerialized] private Vector3 prevResetPos;
        [NonSerialized] private Quaternion prevResetRot;

        // ReSharper restore InconsistentNaming
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        private void OnValidate()
        {
            if (Application.isPlaying) return;
            UnityEditor.EditorUtility.SetDirty(this);

            if (
                prevMinRot != minRot ||
                prevMaxRot != maxRot ||
                prevMinValue != range.x ||
                prevMaxValue != range.y ||
                prevDefault != defaultValue
            )
            {
                ApplyValues();

                prevMinRot = minRot;
                prevMaxRot = maxRot;
                prevMinValue = range.x;
                prevMaxValue = range.y;
                prevDefault = defaultValue;
            }
        }

        [ContextMenu("Apply Values")]
        public override void ApplyValues()
        {
            base.ApplyValues();
            SetupValuesAndComponents();
            
            OnDeserialization();
            UpdateValueIndicator(
                Mathf.Lerp(minRot, maxRot, smoothedCurrentNormalized)
            );
            UpdateTargetIndicator(
                Mathf.Lerp(minRot, maxRot, smoothingTargetNormalized)
            );
            UpdateHandlePosition();

            foreach (var valueFloatDriver in ValueFloatDrivers)
            {
                valueFloatDriver.ApplyFloatValue(
                    Math.Clamp(defaultValue,_minValue,_maxValue)
                );
            }

            foreach (var targetFloatDriver in TargetFloatDrivers)
            {
                targetFloatDriver.ApplyFloatValue(
                    Math.Clamp(defaultValue,_minValue,_maxValue)
                );
            }
        }
#endif
    }
}