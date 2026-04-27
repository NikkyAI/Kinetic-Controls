using System.ComponentModel;
using System.Runtime.CompilerServices;
using nikkyai.Kinetic_Controls;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
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
        private Vector3 _axisVector = Vector3.zero;

        [Header("Fader - VR")] // header
        [SerializeField]
        [Description("switches between finger contacts and pickup")]
        [FieldChangeCallback(nameof(HandleUseContactsInVR))]
        private bool useContactsInVR = true;

        public override bool HandleUseContactsInVR
        {
            get => handle.useContactsInVR;
            set => handle.UseContactsInVR = value;
        }

        protected override bool UseContactsInVR => useContactsInVR;

        [Header("Fader - Components")] //
        [FormerlySerializedAs("minLimit")]
        [SerializeField]
        [Description("move this on the configured axis to the minumum fader range")]
        private Transform minLimitIndicator;
        private float _minPos;
        protected override float MinPosOrRot => _minPos;

        [FormerlySerializedAs("maxLimit")]
        [SerializeField]
        [Description("move this on the configured axis to the maximum fader range")]
        private Transform maxLimitIndicator;
        private float _maxPos;
        protected override float MaxPosOrRot => _maxPos;

        [SerializeField]
        [Description("will be moved to follow the smoothed value")]
        private Transform valueIndicator;
        private bool _valueIndicatorValid = false;

        [SerializeField]
        [Description("will be moved to follow the handle (target value)")]
        private Transform targetIndicator;
        private bool _targetIndicatorValid = false;

        [Header("Fader - Debug")] // header
        [SerializeField]
        private Collider desktopRaycastCollider;

        protected override string LogPrefix => nameof(Fader);

        private void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            base._Init();
            SetupFaderValuesAndComponents();

            OnDeserialization();
            UpdateValueIndicator(
                Mathf.Lerp(_minPos, _maxPos, smoothedCurrentNormalized)
            );
            UpdateTargetIndicator(
                Mathf.Lerp(_minPos, _maxPos, smoothingTargetNormalized)
            );
            handle.ResetTransform();
            // UpdateHandlePosition();

            // pickup.transform.SetPositionAndRotation(pickupReset.position, pickupReset.rotation);
        }

        private void SetupFaderValuesAndComponents()
        {
            Log("SetupValuesAndComponents");

            _targetIndicatorValid = Utilities.IsValid(targetIndicator);
            _valueIndicatorValid = Utilities.IsValid(valueIndicator);
            _axisVector[(int)axis] = 1;

            var minLocalPos = transform.InverseTransformPoint(minLimitIndicator.position);
            var maxLocalPos = transform.InverseTransformPoint(maxLimitIndicator.position);
            _minPos = minLocalPos[(int)axis];
            _maxPos = maxLocalPos[(int)axis];
        }

        protected override void AccessChanged()
        {
            // for (var i = 0; i < _isAuthorizedBoolDrivers.Length; i++)
            // {
            //     _isAuthorizedBoolDrivers[i].UpdateBool(isAuthorized);
            // }
        }

        public override void SetValue(float normalizedValue)
        {
            if (!IsAuthorized) return;
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

        protected override float PosToNormalized(Vector3 absolutePos)
        {
            var relativePos = transform.InverseTransformPoint(absolutePos);
            var clampedPos = Mathf.Clamp(
                relativePos[(int)axis],
                _minPos,
                _maxPos
            );

            // UpdateIndicatorPosition(clampedPos);

            var normalizedValue = Mathf.InverseLerp(
                _minPos,
                _maxPos,
                clampedPos
            );
            Log($"calculating normalized value from {absolutePos} -> {clampedPos} -> {normalizedValue}");
            return normalizedValue;
        }

        public override void FollowPickup()
        {
            if (IsInVR)
            {
                Log("getting normalized value from pickup position");
                // SyncedValueNormalized = PosToNormalized(Pickup.transform.position);
                OnMoveHandle(handle.transform.position);
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
                    // var localHit = transform.InverseTransformPoint(hitPosition);

                    if (Utilities.IsValid(debugDesktopRaytrace))
                    {
                        debugDesktopRaytrace.gameObject.SetActive(true);
                        debugDesktopRaytrace.position = hitPosition;
                    }

                    OnMoveHandle(hitPosition);
                    // SyncedValueNormalized = PosToNormalized(hitPosition);
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

            handle.ResetTransformIfNotManipulated();

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
    }
}