using System;
using System.Runtime.CompilerServices;
using nikkyai.driver;
using nikkyai.ArrayExtensions;
using Texel;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC;
using VRC.SDK3.Components;
using VRC.SDKBase;

// ReSharper disable ForCanBeConvertedToForeach

namespace nikkyai.Kinetic_Controls
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PickupFader : BaseSmoothedBehaviour
    {
        [Header("Pickup Fader")] // header
        [SerializeField] private Axis axis = Axis.Y;
        [SerializeField] private Vector2 range = new Vector2(0, 1);
        [SerializeField] private float defaultValue = 0.25f;
        private float _normalizedDefault;
        [SerializeField] private PickupTrigger pickupTrigger;

        [Tooltip("should be the same as targetIndicator or a child")] //
        [SerializeField] private Transform pickupReset;

        private Vector3 _axisVector = Vector3.zero;

        [InspectorName("minPosition"),
         SerializeField]
        private Transform minLimit;

        [InspectorName("maxPosition"),
         SerializeField]
        private Transform maxLimit;

        [Header("Drivers")] // header
        [SerializeField] private Transform valueIndicator;

        [SerializeField] private Transform targetIndicator;

        [SerializeField] private Transform isAuthorizedIndicator;

        private Rigidbody _rigidbody;
        private bool _pickupHasObjectSync = false;

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

        protected override string LogPrefix => $"{nameof(PickupFader)} {name}";

        protected override DebugLog DebugLog
        {
            get => debugLog;
            set => debugLog = value;
        }

        #endregion
        
        private BoolDriver[] _isAuthorizedBoolDrivers = { };

        [Header("State")] // header
        
        [SerializeField, UdonSynced]
        private bool synced = true;
        
        public override bool Synced
        {
            get => synced;
            set
            {
                if (!isAuthorized) return;

                var prevValue = _syncedValueNormalized;
                TakeOwnership();
                Log($"set synced to {value}");
                synced = value;
                Log($"set normalized to {_syncedValueNormalized} => {prevValue}");
                _syncedValueNormalized = prevValue;
                
                RequestSerialization();
            }
        }

        [UdonSynced]
        // IMPORTANT, DO NOT DELETE
        private float _syncedValueNormalized;

        [UdonSynced] // IMPORTANT, DO NOT DELETE
        private bool _syncedIsBeingManipulated = false;
        protected override bool TargetIsBeingManipulated
        {
            get => _syncedIsBeingManipulated;
            set => _syncedIsBeingManipulated = value;
        }

        private float _lastSyncedValueNormalized = 0;

        // internal values

        private float _minPos, _maxPos;
        protected override float MinPosOrRot => _minPos;
        protected override float MaxPosOrRot => _maxPos;
        
        private float _minValue, _maxValue;
        protected override float MinValue => _minValue;
        protected override float MaxValue => _maxValue;

        private VRC_Pickup _pickup;
        private Rigidbody _pickupRigidBody;
        private VRCPlayerApi _localPlayer;
        private float _lastValue;
        private bool _isHeldLocally;

        private void Start()
        {
            _EnsureInit();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetupValuesAndComponents()
        {
            _axisVector[(int)axis] = 1;
            _minValue = range.x;
            _maxValue = range.y;
            _minPos = minLimit.localPosition[(int)axis];
            _maxPos = maxLimit.localPosition[(int)axis];
            _normalizedDefault = Mathf.InverseLerp(_minValue, _maxValue, defaultValue);
            _syncedValueNormalized = _normalizedDefault;
            
#if UNITY_EDITOR && !COMPILER_UDONSHARP
            minLimit.transform.MarkDirty();
            maxLimit.transform.MarkDirty();
#endif

            smoothedCurrentNormalized = _normalizedDefault;
            smoothingTargetNormalized = _normalizedDefault;
            // enableValueSmoothing = enableValueSmoothing && smoothingUpdateInterval > 0;

            //TODO: move into running in editor
            // _floatDrivers = gameObject.GetComponents<FloatDriver>();
            _valueFloatDrivers = valueIndicator.GetComponents<FloatDriver>()
                .AddRange(
                    valueIndicator.GetComponentsInChildren<FloatDriver>()
                );
            _targetFloatDrivers = targetIndicator.GetComponents<FloatDriver>()
                .AddRange(
                    targetIndicator.GetComponentsInChildren<FloatDriver>()
                );
            
            if (isAuthorizedIndicator)
            {
                _isAuthorizedBoolDrivers = isAuthorizedIndicator.GetComponents<BoolDriver>()
                    .AddRange(
                        isAuthorizedIndicator.GetComponentsInChildren<BoolDriver>()
                    );
            }
            // if (_floatDrivers != null)
            // {
            //     Log($"found {_floatDrivers.Length} drivers in fader");
            // }
            if (_valueFloatDrivers != null)
            {
                Log($"found {_valueFloatDrivers.Length} drivers for value");
            }
            if (_targetFloatDrivers != null)
            {
                Log($"found {_targetFloatDrivers.Length} drivers for target");
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FindPickupTrigger()
        {
            if (pickupTrigger == null)
            {
                pickupTrigger = gameObject.GetComponent<PickupTrigger>();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetupPickupTrigger()
        {
            if (pickupTrigger)
            {
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
                if (_pickup == null)
                {
                    _pickup = pickupTrigger.GetComponent<VRC_Pickup>();
                }
            }

            if (_pickup == null)
            {
                _pickup = gameObject.GetComponent<VRC_Pickup>();
                _pickup.pickupable = false;
            }

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetupPickupRigidBody()
        {
            if (_pickup)
            {
                _pickupRigidBody = _pickup.GetComponent<Rigidbody>();
                _pickupRigidBody.useGravity = false;
                _pickupRigidBody.isKinematic = false;
                _pickupHasObjectSync = _pickup.GetComponent<VRCObjectSync>() != null ||
                                       _pickup.GetComponent("MMMaellon.SmartObjectSync") != null;
            }
            else
            {
                LogError("no pickup found");
            }
        }
        
        protected override void _Init()
        {
            SetupValuesAndComponents();
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
                Mathf.Lerp(_minPos, _maxPos, smoothedCurrentNormalized)
            );
            UpdateTargetIndicator(
                Mathf.Lerp(_minPos, _maxPos, smoothingTargetNormalized)
            );
            UpdatePickupPosition();

            // pickup.transform.SetPositionAndRotation(pickupReset.position, pickupReset.rotation);
        }

        protected override void AccessChanged()
        {
            _pickup.pickupable = isAuthorized;
            
            for (var i = 0; i < _isAuthorizedBoolDrivers.Length; i++)
            {
                _isAuthorizedBoolDrivers[i].UpdateBool(isAuthorized);
            }
        }

        public void _OnPickup()
        {
            if (_isHeldLocally)
            {
                Log("already being adjusted");
                return;
            }

            TakeOwnership();

            _isHeldLocally = true;
            _syncedIsBeingManipulated = true;
            // this.SendCustomEventDelayedFrames(nameof(FollowPickup), 1);
            _OnFollowPickup();
        }

        public void _OnDrop()
        {
            TakeOwnership();

            _isHeldLocally = false;
            _syncedIsBeingManipulated = false;

            
            if (synced)
            {
                RequestSerialization();
            }
            OnDeserialization();

            UpdatePickupPosition();
            // Log("handle released, resetting position");
            // SendCustomNetworkEvent(NetworkEventTarget.All, nameof(UpdatePickupPosition));
        }

        public void _OnFollowPickup()
        {
            if (!_isHeldLocally) return;

            var clampedPos = Mathf.Clamp(
                _pickup.transform.localPosition[(int)axis],
                _minPos,
                _maxPos
            );

            // UpdateIndicatorPosition(clampedPos);

            _syncedValueNormalized = Mathf.InverseLerp(
                _minPos,
                _maxPos,
                clampedPos
            );

            if (_isHeldLocally)
            {
                this.SendCustomEventDelayedFrames(nameof(_OnFollowPickup), 0);
            }

            
            if (synced)
            {
                RequestSerialization();
            }
            OnDeserialization();
        }

        protected override void UpdateTargetIndicator(float clampedPos)
        {
            // if (!enableValueSmoothing) return;
            if (targetIndicator == null) return;
            Vector3 newPos = targetIndicator.transform.localPosition;
            newPos[(int)axis] = clampedPos;
            targetIndicator.transform.localPosition = newPos;
            
            if (!_pickupHasObjectSync && !_isHeldLocally)
            {
                UpdatePickupPosition();
            }
#if UNITY_EDITOR && !COMPILER_UDONSHARP
            targetIndicator.transform.MarkDirty();
#endif
        }

        protected override void UpdateValueIndicator(float clampedPos)
        {
            if (valueIndicator == null) return;
            Vector3 newPos = valueIndicator.transform.localPosition;
            newPos[(int)axis] = clampedPos;
            valueIndicator.transform.localPosition = newPos;

            // if (!_pickupHasObjectSync && !_isHeldLocally)
            // {
            //     UpdatePickupPosition();
            // }
#if UNITY_EDITOR && !COMPILER_UDONSHARP
            valueIndicator.transform.MarkDirty();
#endif
        }

        private void UpdatePickupPosition()
        {
            _pickupRigidBody.angularVelocity = Vector3.zero;
            _pickupRigidBody.velocity = Vector3.zero;
            // parentConstraint.GlobalWeight = 1;
            _pickup.transform.SetPositionAndRotation(
                pickupReset.position,
                pickupReset.rotation
            );

#if UNITY_EDITOR && !COMPILER_UDONSHARP
            _pickupRigidBody.MarkDirty();
            _pickup.transform.MarkDirty();
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

                _lastSyncedValueNormalized = _syncedValueNormalized;
            }
        }
        
        // ReSharper disable InconsistentNaming
        [NonSerialized] private float 
            prevMinPos, prevMaxPos,
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
                prevMinPos != _minPos ||
                prevMaxPos != _maxPos ||
                prevMinValue != range.x ||
                prevMaxValue != range.y ||
                !prevResetPos.Compare(pickupReset.position, 1) ||
                !prevResetRot.Compare(pickupReset.rotation, 1) || 
                prevDefault != defaultValue
            )
            {
                ApplyValues();

                prevMinPos = _minPos;
                prevMaxPos = _maxPos;
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
            FindPickupTrigger();
            // SetupPickupTrigger();
            SetupPickup();
            SetupPickupRigidBody();
            OnDeserialization();
            UpdateValueIndicator(
                Mathf.Lerp(_minPos, _maxPos, smoothedCurrentNormalized)
            );
            UpdateTargetIndicator(
                Mathf.Lerp(_minPos, _maxPos, smoothingTargetNormalized)
            );
            UpdatePickupPosition();

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