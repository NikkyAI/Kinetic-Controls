using System;
using System.Runtime.CompilerServices;
using nikkyai.driver;
using nikkyai.toggle.common;
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
    public class PickupLever : ACLBase
    {
        [SerializeField] private Axis axis = Axis.Z;
        private Vector3 _forwardVector = Vector3.forward;
        [SerializeField] private Transform leverBase;

        [SerializeField, InspectorName("output range")]
        private Vector2 range = new Vector2(0, 1);

        [SerializeField] private float defaultValue = 0.25f;

        [Range(-180, 180), SerializeField, PreviouslySerializedAs("_minRot")]
        private float minRot = -45;

        [Range(-180, 180), SerializeField, PreviouslySerializedAs("_maxRot")]
        private float maxRot = 45;

        private float _normalizedDefault;
        [SerializeField] private PickupTrigger pickupTrigger;

        [SerializeField] private Transform pickupReset;

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

        private FloatDriver[] _floatDrivers = { };
        private FloatDriver[] _targetFloatDrivers = { };
        private FloatDriver[] _valueFloatDrivers = { };
        //TODO: find at buildtime and update

        // [SerializeField] private bool syncPickup = true;
        private Rigidbody _rigidbody;
        private bool _pickupHasObjectSync = false;


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
                UpdateSmoothing();
            }
        }

        [Tooltip("amount of frames to skip when approaching target value," +
                 "higher number == less load, but more choppy smoothing"),
         SerializeField]
        private int smoothingUpdateInterval = 3;


        [Tooltip("fraction of the distance covered within roughly 1s"),
         SerializeField]
        private float smoothingRate = 0.5f;

        public float SmoothingRate
        {
            get => smoothingRate;
            set { smoothingRate = value; }
        }

        #region ACL
        
        [Header("Access Control")] // header
        [SerializeField]
        private bool enforceACL = true;

        protected override bool EnforceACL
        {
            get => enforceACL;
            set => enforceACL = value;
        }

        [Tooltip("ACL used to check who can use the toggle")] [SerializeField]
        private AccessControl accessControl;

        protected override AccessControl AccessControl
        {
            get => accessControl;
            set => accessControl = value;
        }

        #endregion

        #region Debug
        
        [Header("Debug")] // header
        [SerializeField]
        private DebugLog debugLog;

        protected override string LogPrefix => $"{nameof(PickupLever)} {name}";

        protected override DebugLog DebugLog
        {
            get => debugLog;
            set => debugLog = value;
        }

        #endregion

        [Header("State")] // header
        [UdonSynced]
        // IMPORTANT, DO NOT DELETE
        private float _syncedValueNormalized;

        [UdonSynced] private bool _syncedIsMoving = false;
        
        private float _lastSyncedValueNormalized = 0;

        // internal values

        private float _minValue, _maxValue;
        private VRC_Pickup pickup;
        private Rigidbody pickupRigidBody;
        private VRCPlayerApi _localPlayer;
        private float _lastValue;
        private bool isHeldLocally;


        // value smoothing

        #region value smoothing

        private float smoothingTargetNormalized;
        private float smoothedCurrentNormalized;
        private const float epsilon = 0.005f;
        private bool valueInitialized = false;
        private bool isSmoothing = false;
        private float lastFrameTime = 0;

        #endregion

        private void Start()
        {
            _EnsureInit();
        }
        
        protected override void _Init()
        {
            Log("Init");
            SetupValuesAndComponents();
            UpdateSmoothing();
            FindPickupTrigger();
            SetupPickupTrigger();
            SetupPickup();
            SetupPickupRigidBody();

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
            UpdatePickupPosition();

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
            _syncedValueNormalized = _normalizedDefault;

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

#if UNITY_EDITOR && !COMPILER_UDONSHARP
            minLimit.transform.MarkDirty();
            maxLimit.transform.MarkDirty();
#endif
            
            smoothedCurrentNormalized = _normalizedDefault;
            smoothingTargetNormalized = _normalizedDefault;
            enableValueSmoothing = enableValueSmoothing && smoothingUpdateInterval > 0;
            
            //TODO: move into running in editor ?
            Log("Searching for float drivers");
            _floatDrivers = gameObject.GetComponents<FloatDriver>();
            _valueFloatDrivers = valueIndicator.GetComponents<FloatDriver>();
            _targetFloatDrivers = targetIndicator.GetComponents<FloatDriver>();
            if (_floatDrivers != null)
            {
                Log($"found {_floatDrivers.Length} drivers in fader");
            }
            if (_valueFloatDrivers != null)
            {
                Log($"found {_valueFloatDrivers.Length} drivers for value");
            }
            if (_targetFloatDrivers != null)
            {
                Log($"found {_targetFloatDrivers.Length} drivers for target");
            }
            Log("Searching for leverBase");
            if (leverBase == null)
            {
                leverBase = this.transform;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FindPickupTrigger()
        {
            if (pickupTrigger == null)
            {
                LogWarning("PickupTrigger not found");
                pickupTrigger = gameObject.GetComponent<PickupTrigger>();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetupPickupTrigger()
        {
            if (pickupTrigger)
            {
                Log("SetupPickupTrigger");
                pickupTrigger.accessControl = accessControl;
                pickupTrigger.enforceACL = EnforceACL;
                pickupTrigger._Register(PickupTrigger.EVENT_PICKUP, this, nameof(_OnPickup));
                pickupTrigger._Register(PickupTrigger.EVENT_DROP, this, nameof(_OnDrop));
            }
            else
            {
                LogError("missing pickup trigger");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetupPickup()
        {
            if (pickupTrigger)
            {
                Log("SetupPickup");
                if (pickup == null)
                {
                    pickup = pickupTrigger.GetComponent<VRC_Pickup>();
                }
            }

            if (pickup == null)
            {
                pickup = gameObject.GetComponent<VRC_Pickup>();
            }

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetupPickupRigidBody()
        {
            if (pickup)
            {
                pickupRigidBody = pickup.GetComponent<Rigidbody>();
                pickupRigidBody.useGravity = false;
                pickupRigidBody.isKinematic = false;
                _pickupHasObjectSync = pickup.GetComponent<VRCObjectSync>() != null;
            }
            else
            {
                LogError("no pickup found");
            }
        }

        private void UpdateSmoothing()
        {
            // TODO: enable or disable components used for smoothing
        }

        public void TakeOwnership()
        {
            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
        }

        protected override void AccessChanged()
        {
            pickup.pickupable = isAuthorized;
        }

        public void _OnPickup()
        {
            if (isHeldLocally)
            {
                Log("already being adjusted");
                return;
            }

            TakeOwnership();

            isHeldLocally = true;
            _syncedIsMoving = true;
            // this.SendCustomEventDelayedFrames(nameof(FollowPickup), 1);
            _OnFollowPickup();
        }

        public void _OnDrop()
        {
            TakeOwnership();

            isHeldLocally = false;
            _syncedIsMoving = false;

            RequestSerialization();
            OnDeserialization();

            UpdatePickupPosition();
            // Log("handle released, resetting position");
            // SendCustomNetworkEvent(NetworkEventTarget.All, nameof(UpdatePickupPosition));
        }

        public void _OnFollowPickup()
        {
            if (!isHeldLocally) return;

            // var clampedPos = Mathf.Clamp(
            //     pickup.transform.localRotation[(int)axis],
            //     _minPos,
            //     _maxPos
            // );
            var relativePos = leverBase.transform.InverseTransformPoint(pickup.transform.position);
            // var relativePos = pickup.transform.localPosition;
            relativePos[(int)axis] = 0;

            var angle = Vector3.SignedAngle(_forwardVector, relativePos, _axisVector);
            Log($"forwardVector: {_forwardVector}");
            Log($"axisVector: {_axisVector}");
            Log($"relativePos: {relativePos}");
            Log($"angle: {angle}");

            // UpdateIndicatorPosition(clampedPos);

            _syncedValueNormalized = Mathf.InverseLerp(
                a: minRot,
                b: maxRot,
                value: angle
            );
            Log($"InverseLerp: {minRot} .. {maxRot}");
            Log($"normalized: {_syncedValueNormalized}");

            if (isHeldLocally)
            {
                this.SendCustomEventDelayedFrames(nameof(_OnFollowPickup), 0);
            }

            if (_syncedValueNormalized != _lastSyncedValueNormalized)
            {
                RequestSerialization();
                OnDeserialization();
                //_lastSyncedValueNormalized = _syncedValueNormalized;
            }
        }
        
        private void _UpdateTargetValue(float normalizedTargetValue)
        {
            Log($"update target value {normalizedTargetValue}");
            var clampedRotEuler = Mathf.Lerp(minRot, maxRot, normalizedTargetValue);
            UpdateTargetIndicator(clampedRotEuler);
            for (var i = 0; i < _targetFloatDrivers.Length; i++)
            {
                _targetFloatDrivers[i].UpdateFloat(normalizedTargetValue);
            }

            // immediate update
            if (!enableValueSmoothing)
            {
                var floatValue = Mathf.Lerp(_minValue, _maxValue, normalizedTargetValue);
                for (var i = 0; i < _floatDrivers.Length; i++)
                {
                    _floatDrivers[i].UpdateFloat(floatValue);
                }
                for (var i = 0; i < _valueFloatDrivers.Length; i++)
                {
                    _valueFloatDrivers[i].UpdateFloat(normalizedTargetValue);
                }

                // _UpdateFloat(
                //     Mathf.Lerp(_minValue, _maxValue, normalizedTargetValue)
                // );
                UpdateValueIndicator(clampedRotEuler);
                if (!_pickupHasObjectSync && !isHeldLocally)
                {
                    UpdatePickupPosition();
                }

                return;
            }

            // value smoothing
            if (!valueInitialized)
            {
                // smoothingTargetNormalized = _normalizedDefault;
                // smoothedCurrentNormalized = _normalizedDefault;
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
                _OnValueSmoothedUpdate();
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

            if (!_syncedIsMoving && Mathf.Abs(smoothingTargetNormalized - smoothedCurrentNormalized) <= epsilon)
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

            var floatValue = Mathf.Lerp(_minValue, _maxValue, smoothedCurrentNormalized);
            for (var i = 0; i < _floatDrivers.Length; i++)
            {
                _floatDrivers[i].UpdateFloat(floatValue);
            }
            for (var i = 0; i < _valueFloatDrivers.Length; i++)
            {
                _valueFloatDrivers[i].UpdateFloat(floatValue);
            }

            // _UpdateFloat(
            //     floatValue
            //     // Mathf.Lerp(_minValue, _maxValue, smoothedCurrentNormalized)
            // );
            UpdateValueIndicator(
                Mathf.Lerp(minRot, maxRot, smoothedCurrentNormalized)
            );
            if (!_pickupHasObjectSync && !isHeldLocally)
            {
                UpdatePickupPosition();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateTargetIndicator(float clampedRotEuler)
        {
            // if (!enableValueSmoothing) return;
            if (targetIndicator == null) return;

            // Vector3 newRot = targetIndicator.localEulerAngles;
            // newRot[(int)axis] = clampedRotEuler;
            // targetIndicator.localRotation = Quaternion.Euler(newRot);
            targetIndicator.localRotation = Quaternion.AngleAxis(clampedRotEuler, _axisVector);

#if UNITY_EDITOR && !COMPILER_UDONSHARP
            targetIndicator.transform.MarkDirty();
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateValueIndicator(float clampedRotEuler)
        {
            if (valueIndicator == null) return;

            // Vector3 newRot = valueIndicator.localEulerAngles;
            // newRot[(int)axis] = clampedRotEuler;
            // valueIndicator.localRotation = Quaternion.Euler(newRot);
            valueIndicator.localRotation = Quaternion.AngleAxis(clampedRotEuler, _axisVector);
            
#if UNITY_EDITOR && !COMPILER_UDONSHARP
            valueIndicator.transform.MarkDirty();
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdatePickupPosition()
        {
            pickupRigidBody.angularVelocity = Vector3.zero;
            pickupRigidBody.velocity = Vector3.zero;
            pickup.transform.SetPositionAndRotation(
                pickupReset.position,
                pickupReset.rotation
            );
            
#if UNITY_EDITOR && !COMPILER_UDONSHARP
            pickupRigidBody.MarkDirty();
            pickup.transform.MarkDirty();
#endif
        }


        public override void OnDeserialization()
        {
            if (_syncedValueNormalized != _lastSyncedValueNormalized)
            {
                _UpdateTargetValue(_syncedValueNormalized);
                // _UpdateFloat(
                //     Mathf.Lerp(_minValue, _maxValue, syncedValueNormalized)
                // );
                // UpdateIndicatorPosition(
                //     Mathf.Lerp(_minPos, _maxPos, smoothedCurrentNormalized)
                // );
                // if (!_pickupHasObjectSync && !isHeldLocally)
                // {
                //     UpdatePickupPosition();
                // }

                _lastSyncedValueNormalized = _syncedValueNormalized;
            }
        }

        // ReSharper disable InconsistentNaming
        [NonSerialized] private float
            prevMinRot, prevMaxRot,
            prevMinValue, prevMaxValue, 
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
                !prevResetPos.Compare(pickupReset.position, 1) ||
                !prevResetRot.Compare(pickupReset.rotation, 1) || 
                prevDefault != defaultValue
            )
            {
                ApplyValues();

                prevMinRot = minRot;
                prevMaxRot = maxRot;
                prevMinValue = range.x;
                prevMaxValue = range.y;
                prevResetPos = pickupReset.position;
                prevResetRot = pickupReset.rotation;
                prevDefault = defaultValue;
            }
        }

        [ContextMenu("Apply Values")]
        public void ApplyValues()
        {
            
            SetupValuesAndComponents();
            UpdateSmoothing();
            FindPickupTrigger();
            // SetupPickupTrigger();
            SetupPickup();
            SetupPickupRigidBody();
            OnDeserialization();
            UpdateValueIndicator(
                Mathf.Lerp(minRot, maxRot, smoothedCurrentNormalized)
            );
            UpdateTargetIndicator(
                Mathf.Lerp(minRot, maxRot, smoothingTargetNormalized)
            );
            UpdatePickupPosition();
            
            foreach (var floatDriver in _floatDrivers)
            {
                floatDriver.ApplyFloatValue(defaultValue);
            }
            foreach (var valueFloatDriver in _valueFloatDrivers)
            {
                valueFloatDriver.ApplyFloatValue(defaultValue);
            }
            foreach (var targetFloatDriver in _targetFloatDrivers)
            {
                targetFloatDriver.ApplyFloatValue(defaultValue);
            }
        }
#endif
    }
}