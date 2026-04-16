using System.ComponentModel;
using nikkyai.Kinetic_Controls;
using UdonSharp;
using UnityEngine;
using VRC;
using VRC.SDKBase;

// ReSharper disable ForCanBeConvertedToForeach

namespace nikkyai.control.kinetic
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Fader : BaseKineticControl
    {
        [Header("Fader")] // header
        [SerializeField]
        private Axis axis = Axis.Y;

        //[SerializeField, InspectorName("output range")]
        // private Vector2 range = new Vector2(0, 1);
        //
        // [SerializeField, Range(0,1)] private float defaultValueNormalized = 0.25f;
        // [SerializeField] private float defaultValue = 0;

        [Header("Fader - VR")] // header
        [SerializeField, Description("switches between finger contacts and pickup")]
        private bool useContactsInVR = true;

        protected override bool UseContactsInVR => useContactsInVR;

        [Header("Fader - Desktop")] // header
        [SerializeField]
        private Collider desktopRaycastCollider;
        // [SerializeField, Range(5, 90)]
        // private float minLookAngle = 30f;

        // [SerializeField]
        // private Handle handle;
        //
        // protected override Handle Handle => handle;

        private Vector3 _axisVector = Vector3.zero;

        [Header("Fader - Components")] //
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


        // [Header("Drivers")] // header
        // [SerializeField]
        // private Transform floatValueDrivers;
        //
        // [SerializeField] private Transform floatTargetDrivers;

        // [SerializeField] private Transform boolAuthorizedDrivers;
        
        // [SerializeField] private Transform triggerOnResetDrivers;


        protected override string LogPrefix => $"{nameof(Fader)} @ {name}";

        // private BoolDriver[] _isAuthorizedBoolDrivers = { };
        
        // private TriggerDriver[] _triggerOnResetDrivers = { };

        // internal values

        private bool _valueIndicatorValid = false;
        private bool _targetIndicatorValid = false;
        private float _minPos, _maxPos;
        protected override float MinPosOrRot => _minPos;
        protected override float MaxPosOrRot => _maxPos;

        private float _minValue, _maxValue;
        // protected override float MinValue => _minValue;
        // protected override float MaxValue => _maxValue;

        private void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            base._Init();
            
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

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void SetupValuesAndComponents()
        {
            base.SetupValuesAndComponents();
            Log("SetupValuesAndComponents");
            
            _targetIndicatorValid = Utilities.IsValid(targetIndicator);
            _valueIndicatorValid = Utilities.IsValid(valueIndicator);
// #if UNITY_EDITOR && !COMPILER_UDONSHARP
//                 isUdon = false;
// #endif
            _axisVector[(int)axis] = 1;
            // _minValue = range.x;
            // _maxValue = range.y;

            var minLocalPos = transform.InverseTransformPoint(minLimit.position);
            var maxLocalPos = transform.InverseTransformPoint(maxLimit.position);
            _minPos = minLocalPos[(int)axis];
            _maxPos = maxLocalPos[(int)axis];
            // _normalizedDefault = Mathf.InverseLerp(_minValue, _maxValue, defaultValue);

            // SyncedValueNormalized = defaultValueNormalized;

// #if UNITY_EDITOR && !COMPILER_UDONSHARP
//             minLimit.transform.MarkDirty();
//             maxLimit.transform.MarkDirty();
// #endif

            // smoothedCurrentNormalized = defaultValueNormalized;
            // smoothingTargetNormalized = defaultValueNormalized;
            // enableValueSmoothing = enableValueSmoothing && smoothingUpdateInterval > 0;

            // if (Utilities.IsValid(boolAuthorizedDrivers))
            // {
            //     _isAuthorizedBoolDrivers = boolAuthorizedDrivers.GetComponentsInChildren<BoolDriver>();
            // }

            // if (Utilities.IsValid(triggerOnResetDrivers))
            // {
            //     _triggerOnResetDrivers = triggerOnResetDrivers.GetComponentsInChildren<TriggerDriver>();
            // }

            // if (ValueFloatDrivers != null)
            // {
            //     Log($"found {ValueFloatDrivers.Length} drivers for value");
            // }
            //
            // if (TargetFloatDrivers != null)
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
        //
        //     SetValue(defaultValueNormalized);
        //     
        //     // for (var i = 0; i < _triggerOnResetDrivers.Length; i++)
        //     // {
        //     //     var triggerDriver = _triggerOnResetDrivers[i];
        //     //     if (Utilities.IsValid(triggerDriver) && triggerDriver.enabled)
        //     //     {
        //     //         triggerDriver.Trigger();
        //     //     }
        //     // }
        // }

        public override void SetValue(float normalizedValue)
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
                SyncedValueNormalized = RelativePosToNormalized(pickup.transform.localPosition);
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

                if(desktopRaycastCollider.Raycast(ray, out var hit, 5))
                {
                    var hitPosition = ray.GetPoint(hit.distance);
                    Log($"raycast hit, distance: {hit.distance}, point: {hitPosition}");
                    var localHit = transform.InverseTransformPoint(hitPosition);

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

        protected override void UpdateTargetIndicator(float clampedPos)
        {
            if (!_targetIndicatorValid) return;
            // Vector3 newPos = targetIndicator.transform.localPosition;
            Vector3 newPos = Vector3.zero;
            newPos[(int)axis] = clampedPos;
            targetIndicator.transform.localPosition = newPos;

            if (!PickupHasObjectSync && !IsHeldLocally)
            {
                UpdateHandlePosition();
            }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
            targetIndicator.transform.MarkDirty();
#endif
        }

        protected override void UpdateValueIndicator(float clampedPos)
        {
            if (!_valueIndicatorValid) return;
            // Vector3 newPos = targetIndicator.transform.localPosition;
            Vector3 newPos = Vector3.zero;
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
        // private float prevDefaultNormalized, prevDefault;

        // private int lastHashFader = 0;
        // ReSharper restore InconsistentNaming
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        // protected override void OnValidate()
        // {
        //     base.OnValidate();
        //     if (Application.isPlaying) return;
        //
        //     int hash = HashCode.Combine(
        //         MinPosOrRot,
        //         MinPosOrRot,
        //         MinValue, 
        //         MaxValue,
        //         defaultValueNormalized,
        //         defaultValue
        //     );
        //         
        //     if (
        //         lastHashFader != hash
        //     )
        //     {
        //         UnityEditor.EditorUtility.SetDirty(this);
        //         ApplyValues();
        //
        //         lastHashFader = hash;
        //         // prevDefaultNormalized =  defaultValueNormalized;
        //         // prevDefault =  defaultValue;
        //     }
        //
        //     // if (prevAccessControl != AccessControl
        //     //     || prevEnforceACL != EnforceACL
        //     //     || prevDebugLog != DebugLog
        //     //    )
        //     // {
        //     //     ApplyACLsAndLog();
        //     //     prevAccessControl = AccessControl;
        //     //     prevDebugLog = DebugLog;
        //     //     prevEnforceACL = EnforceACL;
        //     // }
        // }
#endif
    }
}