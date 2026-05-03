#define READONLY

using nikkyai.attribute;
using nikkyai.Editor;
using nikkyai.Kinetic_Controls;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using VRC;
using VRC.SDKBase;


// ReSharper disable ForCanBeConvertedToForeach

namespace nikkyai.control.kinetic
{
#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [RequireComponent(typeof(FaderEditorHelper))]
#endif
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Fader : BaseKineticControl
    {
        [Header("Fader")] // header
        [SerializeField]
#if READONLY
        [ReadOnly]
#endif
        internal Axis axis = Axis.Y;

        private Vector3 _axisVector = Vector3.zero;

        [Header("Fader - Desktop")] // header
        [SerializeField]
#if READONLY
        [ReadOnly]
#endif
        internal Collider desktopRaycastCollider;

        [Header("Fader - Components")] //
        [SerializeField]
        [Tooltip("move this on the configured axis to the minumum fader range")]
#if READONLY
        [ReadOnly]
#endif
        internal Transform minLimitIndicator;

        [SerializeField]
#if READONLY
        [ReadOnly]
#endif
        internal float minPos;
        protected override float MinPosOrRot => minPos;

        [SerializeField]
        [Tooltip("move this on the configured axis to the maximum fader range")]
#if READONLY
        [ReadOnly]
#endif
        public Transform maxLimitIndicator;

        [SerializeField]
#if READONLY
        [ReadOnly]
#endif
        internal float maxPos;
        protected override float MaxPosOrRot => maxPos;

        [SerializeField]
        [Tooltip("will be moved to follow the smoothed value")]
#if READONLY
        [ReadOnly]
#endif
        internal Transform valueIndicator;

        private bool _valueIndicatorValid = false;

        [SerializeField]
        [Tooltip("will be moved to follow the handle (target value)")]
#if READONLY
        [ReadOnly]
#endif
        internal Transform targetIndicator;

        private bool _targetIndicatorValid = false;


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
                Mathf.Lerp(minPos, maxPos, smoothedCurrentNormalized)
            );
            UpdateTargetIndicator(
                Mathf.Lerp(minPos, maxPos, smoothingTargetNormalized)
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

            // var minLocalPos = transform.InverseTransformPoint(minLimitIndicator.position);
            // var maxLocalPos = transform.InverseTransformPoint(maxLimitIndicator.position);
            // _minPos = minLocalPos[(int)axis];
            // _maxPos = maxLocalPos[(int)axis];
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
                minPos,
                maxPos
            );

            // UpdateIndicatorPosition(clampedPos);

            var normalizedValue = Mathf.InverseLerp(
                minPos,
                maxPos,
                clampedPos
            );
            Log($"calculating normalized value from {absolutePos} -> {clampedPos} -> {normalizedValue}");
            return normalizedValue;
        }

        public override void FollowPickup()
        {
                Log("getting normalized value from pickup position");
                // SyncedValueNormalized = PosToNormalized(Pickup.transform.position);
                OnMoveHandle(handle.transform.position);
        }

        public override void FollowDesktop()
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

        protected override void UpdateTargetIndicator(float clampedPos)
        {
            if (!_targetIndicatorValid) return;
            // Vector3 newPos = targetIndicator.transform.localPosition;
            Vector3 newPos = Vector3.zero;
            newPos[(int)axis] = clampedPos;
            targetIndicator.transform.localPosition = newPos;

            if (Utilities.IsValid(handle))
            {
                handle.ResetTransformIfNotManipulated();
            }
            else
            {
// #if UNITY_EDITOR && !COMPILER_UDONSHARP
//                 LogError($"handle is not set (hideInHierarchy: {hideFlags.HasFlag(HideFlags.HideInHierarchy)} {PrefabUtility.GetPrefabAssetType(this)} {PrefabUtility.GetPrefabInstanceStatus(this)})");
// #else
                LogError("handle is not set (you can ignore this warning for prefabs that aren't in the scene)");
// #endif
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


// #if UNITY_EDITOR && !COMPILER_UDONSHARP
//         [ContextMenu("Setup Editor Helper Script")]
//         private void SetupEditorHelper()
//         {
//             var editorHelper = GetComponent<FaderEditorHelper>();
//             if (editorHelper == null)
//             {
//                 editorHelper = gameObject.AddComponent<FaderEditorHelper>();
//                 editorHelper.fader = this;
//                 editorHelper.CopyFromFader();
//             }
//             else
//             {
//                 editorHelper.fader = this;
//                 editorHelper.CopyFromFader();
//             }
//         }
// #endif
    }
}