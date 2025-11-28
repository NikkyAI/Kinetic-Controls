using System;
using System.Runtime.CompilerServices;
using nikkyai.driver;
using nikkyai.ArrayExtensions;
using Texel;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC;
using VRC.SDKBase;
using VRC.Udon.Common;

// ReSharper disable ForCanBeConvertedToForeach

namespace nikkyai.Kinetic_Controls
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class TouchFader : BaseSmoothedBehaviour
    {
        [Header("Touch Fader")] // header
        [SerializeField]
        private Axis axis = Axis.Y;

        [SerializeField] private Vector2 range = new Vector2(0, 1);
        [SerializeField] private float defaultValue = 0.25f;
        private float _normalizedDefault;

        [SerializeField] private TouchFaderHandle faderHandle;

        private Collider _handleCollider;
        // private InteractCallback _interactCallback;
        // [SerializeField] private PickupTrigger pickupTrigger;

        // [SerializeField] private Transform pickupReset;

        private Vector3 _axisVector = Vector3.zero;

        [SerializeField] private FingerContactTracker fingerTracker;

        [InspectorName("minPosition"),
         SerializeField]
        private Transform minLimit;

        [InspectorName("maxPosition"),
         SerializeField]
        private Transform maxLimit;

        [Header("Drivers")] // header
        [SerializeField]
        private Transform valueIndicator;

        [SerializeField]
        private Transform targetIndicator;

        [SerializeField] private Transform isAuthorizedIndicator;

        private Rigidbody _rigidbody;

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

        protected override string LogPrefix => $"{nameof(TouchFader)} {name}";

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
                
                TakeOwnership();
                synced = value;
                
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

        // private VRC_Pickup pickup;
        private Rigidbody _rigidBody;
        private VRCPlayerApi _localPlayer;
        private float _lastValue;
        private bool _isHeldLocally;
        private bool _isDesktop = false;

        private Collider _leftHandCollider;
        private Collider _rightHandCollider;
        private int _leftHandColliderId;
        private int _rightHandColliderId;
        private bool _inLeftTrigger;
        private bool _inRightTrigger;

        private bool isUdon = true;

        private void Start()
        {
            _EnsureInit();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetupValuesAndComponents()
        {
#if COMPILER_UDONSHARP
                isUdon = false;
#endif
            _axisVector[(int)axis] = 1;
            _minValue = range.x;
            _maxValue = range.y;
            _minPos = minLimit.localPosition[(int)axis];
            _maxPos = maxLimit.localPosition[(int)axis];
            _normalizedDefault = Mathf.InverseLerp(_minValue, _maxValue, defaultValue);
            _syncedValueNormalized = _normalizedDefault;

            _localPlayer = Networking.LocalPlayer;
            if (_localPlayer != null)
            {
                _isDesktop = !_localPlayer.IsUserInVR();
            }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
            minLimit.transform.MarkDirty();
            maxLimit.transform.MarkDirty();
#endif

            smoothedCurrentNormalized = _normalizedDefault;
            smoothingTargetNormalized = _normalizedDefault;
            // enableValueSmoothing = enableValueSmoothing && smoothingUpdateInterval > 0;

            //TODO: move into running in editor
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
        private void SetupFingerTracker()
        {
            if (fingerTracker)
            {
                _leftHandCollider = fingerTracker.leftHandCollider;
                _rightHandCollider = fingerTracker.rightHandCollider;
                if (_leftHandCollider)
                {
                    _leftHandColliderId = _leftHandCollider.GetInstanceID();
                }

                if (_rightHandCollider)
                {
                    _rightHandColliderId = _rightHandCollider.GetInstanceID();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetupFaderHandle()
        {
            if (faderHandle)
            {
                faderHandle.touchFader = this;
                faderHandle.leftHandCollider = _leftHandCollider;
                faderHandle.rightHandCollider = _rightHandCollider;

                // _interactCallback = faderHandle.GetComponent<InteractCallback>();
#if UNITY_EDITOR && !COMPILER_UDONSHARP
                faderHandle.EditorACL = accessControl;
                faderHandle.enforceACL = enforceACL;
                faderHandle.EditorDebugLog = debugLog;
                faderHandle.MarkDirty();
#endif
            }
            else
            {
                LogError("missing fader handle");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetupRigidBody()
        {
            if (faderHandle)
            {
                //TODO: get rigidbody from handle contact
                _rigidBody = faderHandle.GetComponent<Rigidbody>();
                if (_rigidBody)
                {
                    _rigidBody.useGravity = false;
                    _rigidBody.isKinematic = true;
                    // if (axis == Axis.X)
                    // {
                    //     _rigidBody.constraints = RigidbodyConstraints.FreezeAll
                    //                              & ~RigidbodyConstraints.FreezePositionX;
                    // }
                    // else if (axis == Axis.Y)
                    // {
                    //     _rigidBody.constraints = RigidbodyConstraints.FreezeAll
                    //                              & ~RigidbodyConstraints.FreezePositionY;
                    // }
                    // else
                    // {
                    //     _rigidBody.constraints = RigidbodyConstraints.FreezeAll
                    //                              & ~RigidbodyConstraints.FreezePositionZ;
                    // }
                }
            }
            else
            {
                LogError("missing handle collider");
            }
        }

        protected override void _Init()
        {
            SetupValuesAndComponents();
            SetupFingerTracker();
            SetupFaderHandle();
            SetupRigidBody();

            OnDeserialization();
            UpdateValueIndicator(
                Mathf.Lerp(_minPos, _maxPos, smoothedCurrentNormalized)
            );
            UpdateTargetIndicator(
                Mathf.Lerp(_minPos, _maxPos, smoothingTargetNormalized)
            );

            // pickup.transform.SetPositionAndRotation(pickupReset.position, pickupReset.rotation);
        }

        protected override void AccessChanged()
        {
            for (var i = 0; i < _isAuthorizedBoolDrivers.Length; i++)
            {
                _isAuthorizedBoolDrivers[i].UpdateBool(isAuthorized);
            }
        }


        public void _HandleInteract()
        {
            if (!isAuthorized) return;
            if (!_isDesktop) return;

            TakeOwnership();

            Log("Interact");
            DesktopPickup();
        }

        public void _HandleRelease()
        {
            if (!isAuthorized) return;
            if (!_isDesktop) return;

            DesktopDrop();
        }

        private void DesktopPickup()
        {
            _isHeldLocally = true;
            _syncedIsBeingManipulated = true;
            Log($"Desktop Pickup with target at {_syncedValueNormalized}");
            
            if (synced)
            {
                RequestSerialization();
            }
        }


        private void DesktopDrop()
        {
            _isHeldLocally = false;
            _syncedIsBeingManipulated = false;
            Log($"Desktop Drop with target at {_syncedValueNormalized}");
            
            if (synced)
            {
                RequestSerialization();
            }
        }

        private const float DesktopDampening = 20;

        public override void InputLookVertical(float value, UdonInputEventArgs args)
        {
            if (!isAuthorized) return;
            if (!_isDesktop) return;
            if (!_isHeldLocally) return;
            // if (axis != Axis.Y) return; 
            // Log($"InputLookVertical {value} {args.handType}");

            if (!_localPlayer.IsOwner(gameObject))
            {
                Networking.SetOwner(_localPlayer, gameObject);
            }

            //TODO: figure out if this is necessary
            // if (!valueInitialized)
            // {
            //     _syncedValueNormalized = _normalizedDefault;
            //     smoothedCurrentNormalized = _normalizedDefault;
            //     smoothingTargetNormalized = _normalizedDefault;
            //     valueInitialized = true;
            // }

            var offset = value / DesktopDampening;
            // syncedValueNormalized = Mathf.Clamp(syncedValueNormalized + offset, minValue, maxValue);
            _syncedValueNormalized = Mathf.Clamp(_syncedValueNormalized + offset, 0f, 1f);

            // if (_syncedValueNormalized != _lastSyncedValueNormalized)
            // {
           
            if (synced)
            {
                RequestSerialization();
            }
            OnDeserialization();
            // }
        }

        private bool _isColliding = false;

        public void _OnFollowCollider()
        {
            if (!isAuthorized) return;
            // if (_isDesktop) return; // should not be required

            // if (_leftGrabbed || _rightGrabbed)
            if (fingerTracker.leftGrabbed || fingerTracker.rightGrabbed)
            {
                this.SendCustomEventDelayedFrames(nameof(_OnFollowCollider), 1);
            }
            else
            {
                if (_syncedIsBeingManipulated)
                {
                    _syncedIsBeingManipulated = false;
                    
                    if (synced)
                    {
                        RequestSerialization();
                    }
                }

                return;
            }

            // if ((fingerContactTracker.rightGrabbed && _inRightTrigger) || (fingerContactTracker.leftGrabbed && _inLeftTrigger))
            if (_inRightTrigger || _inLeftTrigger)
            //if(_isColliding)
            {
                _syncedIsBeingManipulated = true;
                Transform handData = _inRightTrigger ? _rightHandCollider.transform : _leftHandCollider.transform;

                var localFingerPos = transform.InverseTransformPoint(handData.position);
                float fingerPos = localFingerPos[(int)axis];
                var clampedPos = Mathf.Clamp(
                    fingerPos,
                    _minPos,
                    _maxPos
                );

                // UpdateIndicatorPosition(clampedPos);

                _syncedValueNormalized = Mathf.InverseLerp(
                    _minPos,
                    _maxPos,
                    clampedPos
                );


                // if (_syncedValueNormalized != _lastSyncedValueNormalized)
                // {
                
                if (synced)
                {
                    RequestSerialization();
                }
                OnDeserialization();
                // }
            }
            else if(_syncedIsBeingManipulated)
            {
                Log($"VR Drop with target at {_syncedValueNormalized}");
                _syncedIsBeingManipulated = false;
                
                if (synced)
                {
                    RequestSerialization();
                }
            }
        }

        public void _OnCollisionStart()
        {
            TakeOwnership();
            _isColliding = true;
            if (!_isColliding)
            {
                Log($"VR Pickup with target at {_syncedValueNormalized}");
                Log("starting FollowCollider");
                this.SendCustomEventDelayedFrames(nameof(_OnFollowCollider), 1);
            }
        }

        public void _OnCollisionEnd()
        {
            _isColliding = false;
        }
        
        public void _OnTriggerEnter(int other)
        {
            if (!isAuthorized) return;
            Log($"collision with {other}");
            // Log($"OnTriggerEnter() Other: {other.name} ({other.GetInstanceID()}), Script on: {gameObject.name} ({gameObject.GetInstanceID()})");
            if (other == _leftHandColliderId)
            {
                if (!_inLeftTrigger)
                {
                    Log($"Left Trigger Enter");
                }
        
                if (!_inLeftTrigger && fingerTracker.leftGrabbed)
                {
                    Log($"VR Pickup with target at {_syncedValueNormalized}");
                    Log("starting FollowCollider");
                    this.SendCustomEventDelayedFrames(nameof(_OnFollowCollider), 1);
                }
        
                TakeOwnership();
        
                _inLeftTrigger = true;
                _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 1f, 1f, 0.2f);
            }
        
            if (other == _rightHandColliderId)
            {
                if (!_inRightTrigger)
                {
                    Log($"Right Trigger Enter");
                }
        
                if (!_inRightTrigger && fingerTracker.rightGrabbed)
                {
                    Log($"VR Pickup with target at {_syncedValueNormalized}");
                    Log("starting FollowCollider");
                    this.SendCustomEventDelayedFrames(nameof(_OnFollowCollider), 1);
                }
        
                TakeOwnership();
        
                _inRightTrigger = true;
                _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 1f, 1f, 0.2f);
            }
        }

        public void _OnTriggerExit(int other)
        {
            if (!isAuthorized)
            {
                return;
            }
        
            if (other == _leftHandColliderId)
            {
                if (_inLeftTrigger)
                {
                    Log($"Left Trigger Exit");
                }
        
                _inLeftTrigger = false;
                _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 1f, 1f, 0.2f);
            }
        
            if (other == _rightHandColliderId)
            {
                if (_inRightTrigger)
                {
                    Log($"Right Trigger Exit");
                }
        
                _inRightTrigger = false;
                _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 1f, 1f, 0.2f);
            }
        }

        protected override void UpdateTargetIndicator(float clampedPos)
        {
            // if (!enableValueSmoothing) return;
            if (targetIndicator == null) return;
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
            if (valueIndicator == null) return;
            Vector3 newPos = valueIndicator.transform.localPosition;
            newPos[(int)axis] = clampedPos;
            valueIndicator.transform.localPosition = newPos;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
            valueIndicator.transform.MarkDirty();
#endif
        }

        private void UpdateHandlePosition()
        {
            if (_rigidBody)
            {
                _rigidBody.angularVelocity = Vector3.zero;
                _rigidBody.velocity = Vector3.zero;
            }

            // parentConstraint.GlobalWeight = 1;
            faderHandle.transform.SetPositionAndRotation(
                targetIndicator.position,
                targetIndicator.rotation
            );

#if UNITY_EDITOR && !COMPILER_UDONSHARP
            if (_rigidBody) _rigidBody.MarkDirty();
            faderHandle.transform.MarkDirty();
#endif
        }

        public override void OnDeserialization()
        {
            if (_syncedValueNormalized != _lastSyncedValueNormalized)
            {
                _UpdateTargetValue(_syncedValueNormalized);

                _lastSyncedValueNormalized = _syncedValueNormalized;
            }
        }

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

            if (prevAccessControl != accessControl
                || prevEnforceACL != enforceACL
                || prevDebugLog != debugLog
               )
            {
                ApplyACLsAndLog();
                prevAccessControl = accessControl;
                prevDebugLog = debugLog;
            }
        }

        [ContextMenu("Apply Values")]
        public void ApplyValues()
        {
            SetupValuesAndComponents();
            SetupFingerTracker();
            SetupFaderHandle();
            SetupRigidBody();
            OnDeserialization();
            UpdateValueIndicator(
                Mathf.Lerp(_minPos, _maxPos, smoothedCurrentNormalized)
            );
            UpdateTargetIndicator(
                Mathf.Lerp(_minPos, _maxPos, smoothingTargetNormalized)
            );

            foreach (var valueFloatDriver in _valueFloatDrivers)
            {
                valueFloatDriver.ApplyFloatValue(defaultValue);
            }

            foreach (var targetFloatDriver in _targetFloatDrivers)
            {
                targetFloatDriver.ApplyFloatValue(defaultValue);
            }
        }

        [ContextMenu("Apply ACLs and Log")]
        private void ApplyACLsAndLog()
        {
            // _interactCallback.EditorACL = accessControl;
            // _interactCallback.EditorDebugLog = debugLog;
            // _interactCallback.EditorEnforceACL = enforceACL;
            // _interactCallback.MarkDirty();

            faderHandle.EditorACL = accessControl;
            faderHandle.EditorDebugLog = debugLog;
            faderHandle.EditorEnforceACL = enforceACL;
            faderHandle.MarkDirty();
        }
#endif
    }
}