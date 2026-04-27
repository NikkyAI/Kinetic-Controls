#define HIDE_INSPECTOR

using System.ComponentModel;
using System.Runtime.CompilerServices;
using nikkyai.Editor;
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
#if HIDE_INSPECTOR
        [HideInInspector]
#endif
        private Axis axis = Axis.Z;
        private Vector3 _axisVector = Vector3.zero;

        private Vector3 _forwardVector = Vector3.zero;

        // [SerializeField, InspectorName("output range")]
        // private Vector2 range = new Vector2(0, 1);
        //
        // [SerializeField, Range(0,1)] private float defaultValueNormalized = 0.25f;
        // [SerializeField] private float defaultValue = 0;

        [Range(-180, 180)]
        [SerializeField]
#if HIDE_INSPECTOR
        [HideInInspector]
#endif
        internal float minRot = -45;
        protected override float MinPosOrRot => minRot;

        [Range(-180, 180)]
        [SerializeField]
#if HIDE_INSPECTOR
        [HideInInspector]
#endif
        internal float maxRot = 45;
        protected override float MaxPosOrRot => maxRot;

        [Header("Lever - Components")] //
        [FormerlySerializedAs("minLimit")]
        [SerializeField]
        [Tooltip("will be rotated to indicate the minimum possible lever range")]
#if HIDE_INSPECTOR
        [HideInInspector]
#endif
        internal Transform minLimitIndicator;

        [FormerlySerializedAs("maxLimit")] 
        [SerializeField]
        [Tooltip("will be rotated to indicate the maximum possible lever range")]
#if HIDE_INSPECTOR
        [HideInInspector]
#endif
        internal Transform maxLimitIndicator;

        [SerializeField]
        [Tooltip("will be rotated to follow the smoothed value")]
#if HIDE_INSPECTOR
        [HideInInspector]
#endif
        internal Transform valueIndicator;
        private bool _valueIndicatorValid = false;

        [SerializeField]
        [Tooltip("will be rotated to follow the handle (target value)")]
#if HIDE_INSPECTOR
        [HideInInspector]
#endif
        internal Transform targetIndicator;
        private bool _targetIndicatorValid = false;

        [Header("Lever - Debug")] // header
        [SerializeField]
#if HIDE_INSPECTOR
        [HideInInspector]
#endif
        internal Collider desktopRaycastCollider;

        protected override string LogPrefix => nameof(Lever);

        private void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            base._Init();
            SetupLeverValuesAndComponents();

            OnDeserialization();
            UpdateValueIndicator(
                Mathf.Lerp(minRot, maxRot, smoothedCurrentNormalized)
            );
            UpdateTargetIndicator(
                Mathf.Lerp(minRot, maxRot, smoothingTargetNormalized)
            );
            handle.ResetTransform();
            // UpdateHandlePosition();
        }

        private void SetupLeverValuesAndComponents()
        {
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

            if (minLimitIndicator)
            {
                minLimitIndicator.transform.localRotation = Quaternion.AngleAxis(minRot, _axisVector);
                
#if UNITY_EDITOR && !COMPILER_UDONSHARP
                minLimitIndicator.transform.MarkDirty();
#endif
            }
            else
            {
                LogError("minLimit is not set");
            }

            if (maxLimitIndicator)
            {
                maxLimitIndicator.transform.localRotation = Quaternion.AngleAxis(maxRot, _axisVector);
                
#if UNITY_EDITOR && !COMPILER_UDONSHARP
                maxLimitIndicator.transform.MarkDirty();
#endif
            }
            else
            {
                LogError("maxLimit is not set");
            }

            // if (Utilities.IsValid(LocalPlayer))
            // {
            //     IsInVR = LocalPlayer.IsUserInVR();
            // }

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

            IsCyclic = Mathf.Approximately(minRot, -180f) && Mathf.Approximately(maxRot, 180f);
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
            if (synced)
            {
                RequestSerialization();
            }

            OnDeserialization();
        }

        protected override float PosToNormalized(Vector3 absolutePos)
        {
            var relativePos = transform.InverseTransformPoint(absolutePos);
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

        public override void FollowPickup()
        {
            if (IsInVR)
            {
                Log("getting normalized value from pickup position");
                // var relativePos = transform.InverseTransformPoint(Pickup.transform.position);
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
                    // var localHit = targetIndicator.parent.InverseTransformPoint(hitPosition);

                    if (Utilities.IsValid(debugDesktopRaytrace))
                    {
                        debugDesktopRaytrace.gameObject.SetActive(true);
                        debugDesktopRaytrace.position = hitPosition;
                    }

                    // SyncedValueNormalized = PosToNormalized(hitPosition);
                    OnMoveHandle(hitPosition);
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

        protected override void UpdateTargetIndicator(float clampedRotEuler)
        {
            // base.UpdateTargetIndicator(clampedRotEuler);
            if (!_targetIndicatorValid) return;

            targetIndicator.localRotation = Quaternion.AngleAxis(clampedRotEuler, _axisVector);

            handle.ResetTransformIfNotManipulated();
            // if (!handle.PickupHasObjectSync && !handle.IsHeldLocally)
            // {
            //     handle.ResetTransform();
            // }
#if UNITY_EDITOR && !COMPILER_UDONSHARP
            targetIndicator.transform.MarkDirty();
#endif
        }

        protected override void UpdateValueIndicator(float clampedRotEuler)
        {
            if (!_valueIndicatorValid) return;

            valueIndicator.localRotation = Quaternion.AngleAxis(clampedRotEuler, _axisVector);

#if UNITY_EDITOR && !COMPILER_UDONSHARP
            valueIndicator.transform.MarkDirty();
#endif
        }
#if UNITY_EDITOR && !COMPILER_UDONSHARP
            [ContextMenu("Setup Editor Helper Script")]
            private void SetupEditorHelper()
            {
                var editorHelper = GetComponent<LeverEditorHelper>();
                if (editorHelper == null)
                {
                    editorHelper = gameObject.AddComponent<LeverEditorHelper>();
                    editorHelper.lever = this;
                    editorHelper.CopyFromLever();
                }
                else
                {
                    editorHelper.lever = this;
                    editorHelper.CopyFromLever();
                }
            }
#endif
    }
}