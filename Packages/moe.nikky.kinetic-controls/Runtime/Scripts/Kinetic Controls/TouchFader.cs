using System;
using System.Runtime.CompilerServices;
using nikkyai.driver;
using nikkyai.toggle.common;
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
    public class TouchFader : ACLBase
    {
        [SerializeField] private Axis axis = Axis.Y;
        [SerializeField] private Vector2 range = new Vector2(0, 1);
        [SerializeField] private float defaultValue = 0.25f;
        private float _normalizedDefault;

        [SerializeField] private TouchFaderHandle faderHandle;
        private Collider _handleCollider;
        // private InteractCallback _interactCallback;
        // [SerializeField] private PickupTrigger pickupTrigger;

        // [SerializeField] private Transform pickupReset;

        private Vector3 _axisVector = Vector3.zero;

        [FormerlySerializedAs("fingerContactTracker")] [SerializeField] private FingerContactTracker fingerTracker;

        [InspectorName("minPosition"),
         SerializeField]
        private Transform minLimit;

        [InspectorName("maxPosition"),
         SerializeField]
        private Transform maxLimit;

        [FormerlySerializedAs("valuePosition"), InspectorName("valuePosition"), SerializeField]
        private Transform valueIndicator;

        [FormerlySerializedAs("targetPosition"), InspectorName("targetPosition"), SerializeField]
        private Transform targetIndicator;

        private FloatDriver[] _floatDrivers = { };
        private FloatDriver[] _targetFloatDrivers = { };
        private FloatDriver[] _valueFloatDrivers = { };
        //TODO: find at buildtime and update ?

        private Rigidbody _rigidbody;
        // private bool _pickupHasObjectSync = false;


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
                // UpdateSmoothing();
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
        
                protected override string LogPrefix => $"{nameof(TouchFader)} {name}";
        
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

        private float _minPos, _maxPos;

        private float _minValue, _maxValue;

        // private VRC_Pickup pickup;
        private Rigidbody _rigidBody;
        private VRCPlayerApi _localPlayer;
        private float _lastValue;
        private bool _isHeldLocally;
        private bool _isDesktop = false;

        private GameObject _leftHandCollider;
        private GameObject _rightHandCollider;
        // private bool _rightGrabbed;
        // private bool _leftGrabbed;
        private bool _inLeftTrigger;
        private bool _inRightTrigger;

        // private VRCParentConstraint parentConstraint;

        // value smoothing

        #region value smoothing

        private float smoothingTargetNormalized;
        private float smoothedCurrentNormalized;
        private const float epsilon = 0.005f;
        private bool valueInitialized = false;
        private bool isSmoothing = false;
        private float lastFrameTime = 0;

        #endregion

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
            enableValueSmoothing = enableValueSmoothing && smoothingUpdateInterval > 0;

            //TODO: move into running in editor
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
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetupFingerTracker()
        {
            if (fingerTracker)
            {
                _leftHandCollider = fingerTracker.leftHandCollider;
                _rightHandCollider = fingerTracker.rightHandCollider;
            }
        }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // private void FindInteractTrigger()
        // {
        //     if (_interactCallback == null)
        //     {
        //         _interactCallback = gameObject.GetComponent<InteractTrigger>();
        //     }
        // }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetupFaderHandle()
        {
            if (faderHandle)
            {
                faderHandle.touchFader = this;
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

//             if (_interactCallback)
//             {
//                 if (isUdon)
//                 {
//                     _interactCallback._Register(InteractCallback.EVENT_INTERACT, this, nameof(_HandleInteract));
//                     _interactCallback._Register(InteractCallback.EVENT_RELEASE, this, nameof(_HandleRelease));
//                 }
//
//                 //TODO: add label field ?
//                 _interactCallback.InteractionText = "click to move";
//                 _interactCallback.DisableInteractive = !_isDesktop;
//
// #if UNITY_EDITOR && !COMPILER_UDONSHARP
//                 _interactCallback.EditorACL = accessControl;
//                 _interactCallback.enforceACL = enforceACL;
//                 _interactCallback.EditorDebugLog = debugLog;
//
//                 _interactCallback.MarkDirty();
// #endif
//             }
//             else
//             {
//                 LogError("missing interact callback");
//             }
        }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // private void SetupPickup()
        // {
        //     if (pickupTrigger)
        //     {
        //         if (pickup == null)
        //         {
        //             pickup = pickupTrigger.GetComponent<VRC_Pickup>();
        //         }
        //     }
        //
        //     if (pickup == null)
        //     {
        //         pickup = gameObject.GetComponent<VRC_Pickup>();
        //     }
        //
        // }

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
            UpdateHandlePosition();

            // pickup.transform.SetPositionAndRotation(pickupReset.position, pickupReset.rotation);
        }

        private void TakeOwnership()
        {
            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
        }

        protected override void AccessChanged()
        {
            // _interactCallback.DisableInteractive = !_isDesktop && isAuthorized;
            // _interactCallback.DisableInteractive = !_isDesktop && isAuthorized;
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
            _syncedIsMoving = true;
            RequestSerialization();
        }


        private void DesktopDrop()
        {
            _isHeldLocally = false;
            _syncedIsMoving = false;
            RequestSerialization();
        }


        // public override void InputGrab(bool value, UdonInputEventArgs args)
        // {
        //     if (!isAuthorized) return;
        //
        //     Log($"InputGrab({value}, {args.handType})");
        //
        //     // if (_isDesktop)
        //     // {
        //     //     if (!value)
        //     //     {
        //     //         DesktopDrop();
        //     //     }
        //     // }
        //
        //     if (!_isDesktop)
        //     {
        //         if (value)
        //         {
        //             // if (!_leftGrabbed && !_rightGrabbed)
        //             // {
        //             //     Log("starting FollowCollider");
        //             //     this.SendCustomEventDelayedFrames(nameof(_OnFollowCollider), 1);
        //             // }
        //
        //             if (args.handType == HandType.LEFT)
        //             {
        //                 if (!_leftGrabbed)
        //                 {
        //                     Log($"LeftGrabbed()");
        //                 }
        //
        //                 _leftGrabbed = true;
        //             }
        //
        //             if (args.handType == HandType.RIGHT)
        //             {
        //                 if (!_rightGrabbed)
        //                 {
        //                     Log($"RightGrabbed()");
        //                 }
        //
        //                 _rightGrabbed = true;
        //             }
        //         }
        //         else
        //         {
        //             if (args.handType == HandType.LEFT)
        //             {
        //                 if (_leftGrabbed) Log($"LeftReleased()");
        //
        //                 _leftGrabbed = false;
        //             }
        //
        //             if (args.handType == HandType.RIGHT)
        //             {
        //                 if (_rightGrabbed) Log($"RightReleased()");
        //
        //                 _rightGrabbed = false;
        //             }
        //         }
        //     }
        // }

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

            if (!valueInitialized)
            {
                _syncedValueNormalized = _normalizedDefault;
                smoothedCurrentNormalized = _normalizedDefault;
                smoothingTargetNormalized = _normalizedDefault;
                valueInitialized = true;
            }

            // var offset = (maxValue - minValue) * value / _desktopDampening;
            var offset = value / DesktopDampening;
            // syncedValueNormalized = Mathf.Clamp(syncedValueNormalized + offset, minValue, maxValue);
            _syncedValueNormalized = Mathf.Clamp(_syncedValueNormalized + offset, 0f, 1f);

            if (_syncedValueNormalized != _lastSyncedValueNormalized)
            {
                RequestSerialization();
                OnDeserialization();
                // _lastSyncedValueNormalized = _syncedValueNormalized;
            }
        }

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
                if (_syncedIsMoving)
                {
                    _syncedIsMoving = false;
                    RequestSerialization();
                }
                return;
            }

            // if ((fingerContactTracker.rightGrabbed && _inRightTrigger) || (fingerContactTracker.leftGrabbed && _inLeftTrigger))
            if (_inRightTrigger || _inLeftTrigger)
            {
                _syncedIsMoving = true;
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


                if (_syncedValueNormalized != _lastSyncedValueNormalized)
                {
                    RequestSerialization();
                    OnDeserialization();
                    // _lastSyncedValueNormalized = _syncedValueNormalized;
                }
            }
            else
            {
                _syncedIsMoving = false;
                RequestSerialization();
            }
        }

        // [NonSerialized] private Collider other;
        public void _OnTriggerEnter(string other)
        {
            if (!isAuthorized) return;
            Log($"collision with {other}");
            // Log($"OnTriggerEnter() Other: {other.name} ({other.GetInstanceID()}), Script on: {gameObject.name} ({gameObject.GetInstanceID()})");
            if (other == _leftHandCollider.name)
            {
                if (!_inLeftTrigger)
                {
                    Log($"Left Trigger Enter");
                }
                if (!_inLeftTrigger && fingerTracker.leftGrabbed)
                {
                    Log("starting FollowCollider");
                    this.SendCustomEventDelayedFrames(nameof(_OnFollowCollider), 1);
                }
                TakeOwnership();

                _inLeftTrigger = true;
                _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 1f, 1f, 0.2f);
            }

            if (other == _rightHandCollider.name)
            {
                if (!_inRightTrigger)
                {
                    Log($"Right Trigger Enter");
                }
                if (!_inRightTrigger &&  fingerTracker.rightGrabbed)
                {
                    Log("starting FollowCollider");
                    this.SendCustomEventDelayedFrames(nameof(_OnFollowCollider), 1);
                } 
                TakeOwnership();
                
                _inRightTrigger = true;
                _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 1f, 1f, 0.2f);
            }
        }

        public void _OnTriggerExit(string other)
        {
            if (!isAuthorized)
            {
                return;
            }

            if (other == _leftHandCollider.name)
            {
                if (_inLeftTrigger)
                {
                    Log($"Left Trigger Exit");
                }

                _inLeftTrigger = false;
                _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 1f, 1f, 0.2f);
            }

            if (other == _rightHandCollider.name)
            {
                if (_inRightTrigger)
                {
                    Log($"Right Trigger Exit");
                }

                _inRightTrigger = false;
                _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 1f, 1f, 0.2f);
            }
        }

        private void _UpdateTargetValue(float normalizedTargetValue)
        {
            Log($"update target value {normalizedTargetValue}");
            var clampedPos = Mathf.Lerp(_minPos, _maxPos, normalizedTargetValue);
            UpdateTargetIndicator(clampedPos);
            UpdateHandlePosition();
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
                UpdateValueIndicator(
                    clampedPos
                );
                

                return;
            }

            // value smoothing
            if (!valueInitialized)
            {
                // Log("initializing values for smoothing");
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
                Mathf.Lerp(_minPos, _maxPos, smoothedCurrentNormalized)
            );
            // if (!_isHeldLocally && !(_leftGrabbed && _inLeftTrigger) && !(_rightGrabbed && _inRightTrigger))
            // {
            // UpdateHandlePosition();
            // }
        }

        private void UpdateTargetIndicator(float clampedPos)
        {
            // if (!enableValueSmoothing) return;
            if (targetIndicator == null) return;
            Vector3 newPos = targetIndicator.transform.localPosition;
            newPos[(int)axis] = clampedPos;
            targetIndicator.transform.localPosition = newPos;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
            targetIndicator.transform.MarkDirty();
#endif
        }

        private void UpdateValueIndicator(float clampedPos)
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
            if(_rigidBody) _rigidBody.MarkDirty();
            faderHandle.transform.MarkDirty();
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
            UpdateHandlePosition();

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