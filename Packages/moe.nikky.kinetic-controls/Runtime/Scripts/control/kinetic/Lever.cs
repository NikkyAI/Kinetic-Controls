using System.ComponentModel;
using System.Runtime.CompilerServices;
using nikkyai.Kinetic_Controls;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC;
using VRC.SDKBase;
using VRC.Udon.Serialization.OdinSerializer;

// ReSharper disable ForCanBeConvertedToForeach

namespace nikkyai.control.kinetic
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Lever : BaseKineticControl
    {
        [Header("Lever")] // header
        [SerializeField]
        private Axis axis = Axis.Z;

        private Vector3 _forwardVector = Vector3.zero;
        
        // [SerializeField, InspectorName("output range")]
        // private Vector2 range = new Vector2(0, 1);
        //
        // [SerializeField, Range(0,1)] private float defaultValueNormalized = 0.25f;
        // [SerializeField] private float defaultValue = 0;

        [Range(-180, 180), SerializeField, PreviouslySerializedAs("_minRot")]
        private float minRot = -45;

        [Range(-180, 180), SerializeField, PreviouslySerializedAs("_maxRot")]
        private float maxRot = 45;

        protected override float MinPosOrRot => minRot;
        protected override float MaxPosOrRot => maxRot;

        [Header("Lever - VR")] // header
        [SerializeField, Description("switches between finger contacts and pickup")]
        private bool useContactsInVR = true;

        protected override bool UseContactsInVR => useContactsInVR;

        [Header("Lever - Desktop")] // header
        [SerializeField]
        private Collider desktopRaycastCollider;


        private Vector3 _axisVector = Vector3.zero;

        [Header("Lever - Components")] //
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

        // [Header("Drivers")] // header
        // [SerializeField]
        // private Transform floatValueDrivers;

        // [SerializeField] private Transform floatTargetDrivers;

        // [SerializeField] private Transform boolAuthorizedDrivers;

        // private Rigidbody _rigidbody;
        

        protected override string LogPrefix => $"{nameof(Lever)} @ {name}";

        // private BoolDriver[] _isAuthorizedBoolDrivers = { };

        // internal values

        private bool _valueIndicatorValid = false;
        private bool _targetIndicatorValid = false;
        // private float _minValue, _maxValue;
        // protected override float MinValue => _minValue;
        // protected override float MaxValue => _maxValue;

        // private VRC_Pickup _pickup;
        // private Rigidbody _pickupRigidBody;
        // private float _lastValue;
        // private bool _isHeldLocally;

        private void Start()
        {
            _EnsureInit();
        }

        protected override void _PreInit()
        {
            base._PreInit();
        }

        protected override void _Init()
        {
            base._Init();

            OnDeserialization();
            UpdateValueIndicator(
                Mathf.Lerp(minRot, maxRot, smoothedCurrentNormalized)
            );
            UpdateTargetIndicator(
                Mathf.Lerp(minRot, maxRot, smoothingTargetNormalized)
            );
            UpdateHandlePosition();

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void SetupValuesAndComponents()
        {
            base.SetupValuesAndComponents();
            Log("SetupValuesAndComponents");
            _targetIndicatorValid = Utilities.IsValid(targetIndicator);
            _valueIndicatorValid = Utilities.IsValid(valueIndicator);
            _axisVector = Vector3.zero;
            _axisVector[(int)axis] = 1;
            if (_forwardVector == Vector3.zero)
            {
                if (axis == Axis.X)
                {
                    _forwardVector = Vector3.up;
                }
                else if (axis == Axis.Y)
                {
                    _forwardVector = Vector3.forward;
                }
                else if (axis == Axis.Z)
                {
                    _forwardVector = Vector3.up;
                }
                else
                {
                    LogError("Invalid axis");
                }
            }

            // _minValue = range.x;
            // _maxValue = range.y;
            // _normalizedDefault = Mathf.InverseLerp(_minValue, _maxValue, defaultValue);

            // SyncedValueNormalized = _normalizedDefault;

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

            if (desktopRaycastCollider)
            {
                if (IsInVR)
                {
                    desktopRaycastCollider.enabled = false;
                }
                else
                {
                    desktopRaycastCollider.enabled = true;
                    desktopRaycastCollider.isTrigger = true;
                }
            }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
            minLimit.transform.MarkDirty();
            maxLimit.transform.MarkDirty();
#endif

            // smoothedCurrentNormalized = _normalizedDefault;
            // smoothingTargetNormalized = _normalizedDefault;
            // enableValueSmoothing = enableValueSmoothing && smoothingUpdateInterval > 0;

            isCyclic = Mathf.Approximately(minRot, -180f) && Mathf.Approximately(maxRot, 180f);

            // if (Utilities.IsValid(boolAuthorizedDrivers))
            // {
            //     Log($"Searching for bool authorized drivers in {boolAuthorizedDrivers}");
            //     _isAuthorizedBoolDrivers = boolAuthorizedDrivers.GetComponentsInChildren<BoolDriver>();
            //     Log($"found {_isAuthorizedBoolDrivers.Length} drivers for authorized");
            // }
            
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
            // for (var i = 0; i < _isAuthorizedBoolDrivers.Length; i++)
            // {
            //     _isAuthorizedBoolDrivers[i].UpdateBool(isAuthorized);
            // }
        }


        // implemented in BaseSyncedBehaviour
        // public override void Reset()
        // {
        //     if (!isAuthorized) return;
        //     Log("re-setting synced to default");
        //     SetValue(_normalizedDefault);
        // }

        public override void SetValue(float normalizedValue)
        {
            if (!isAuthorized) return;
            SyncedValueNormalized = normalizedValue;
            // should already be done in OnDeserialization?
            _UpdateTargetValue(normalizedValue);
            if (valueSynced)
            {
                RequestSerialization();
            }

            OnDeserialization();
        }

        protected override float RelativePosToNormalized(Vector3 relativePos)
        {
            relativePos[(int)axis] = 0;

            var angle = Vector3.SignedAngle(_forwardVector, relativePos, _axisVector);

            // Log($"forwardVector: {_forwardVector}");
            // Log($"axisVector: {_axisVector}");
            // Log($"relativePos: {relativePos}");
            // Log($"angle: {angle}");

            // UpdateIndicatorPosition(clampedPos);

            var normalized = Mathf.InverseLerp(
                a: minRot,
                b: maxRot,
                value: angle
            );
            // Log($"InverseLerp: {minRot} .. {maxRot}");
            // Log($"normalized: {normalized}");
            return normalized;
        }

        protected override void FollowPickup()
        {
            if (IsInVR)
            {
                var relativePos = transform.InverseTransformPoint(pickup.transform.position);
                SyncedValueNormalized = RelativePosToNormalized(relativePos);
            }
            else
            {
                if (!Utilities.IsValid(desktopRaycastCollider))
                {
                    LogError("desktop raycast collider is not valid");
                    return;
                }
                
                var trackingData = LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                var ray = new Ray(
                    trackingData.position,
                    trackingData.rotation * Vector3.forward
                );

                if (desktopRaycastCollider.Raycast(ray, out var hit, 5))
                {
                    var hitPosition = ray.GetPoint(hit.distance);
                    Log($"raycast hit, distance: {hit.distance}, point: {hitPosition}");
                    var localHit = targetIndicator.parent.InverseTransformPoint(hitPosition);

                    if (Utilities.IsValid(debugDesktopRaytrace))
                    {
                        debugDesktopRaytrace.gameObject.SetActive(true);
                        debugDesktopRaytrace.position = hitPosition;
                    }

                    SyncedValueNormalized = RelativePosToNormalized(localHit);
                }
                else
                {
                    if (Utilities.IsValid(debugDesktopRaytrace))
                    {
                        debugDesktopRaytrace.gameObject.SetActive(false);
                    }
                }
            }
        }


        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void UpdateTargetIndicator(float clampedRotEuler)
        {
            // base.UpdateTargetIndicator(clampedRotEuler);
            if (!_targetIndicatorValid) return;

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
            if (!_valueIndicatorValid) return;

            valueIndicator.localRotation = Quaternion.AngleAxis(clampedRotEuler, _axisVector);

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

        // private float prevDefaultNormalized, prevDefault;
        // private int lastHashLever = 0;
        
        // ReSharper restore InconsistentNaming
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        // protected override void OnValidate()
        // {
        //     if (Application.isPlaying) return;
        //
        //     var hash = HashCode.Combine(
        //         MinPosOrRot,
        //         MinPosOrRot,
        //         MinValue,
        //         MaxValue,
        //         defaultValueNormalized,
        //         defaultValue
        //     );
        //     if (
        //         lastHashLever != hash
        //     )
        //     {
        //         UnityEditor.EditorUtility.SetDirty(this);
        //         ApplyValues();
        //
        //         lastHashLever = hash;
        //         // prevDefaultNormalized =  defaultValueNormalized;
        //         // prevDefault =  defaultValue;
        //     }
        // }
#endif
    }
}