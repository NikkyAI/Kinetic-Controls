using System;
using System.Runtime.CompilerServices;
using nikkyai.ArrayExtensions;
using nikkyai.common;
using nikkyai.driver;
using Texel;
using UdonSharp;
using UnityEngine;
using VRC;
using VRC.Dynamics;
using VRC.SDKBase;
using VRC.Udon.Common;
using VRC.Udon.Serialization.OdinSerializer.Utilities;

namespace nikkyai.Kinetic_Controls
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ContactFaderDeprecated : BaseSmoothedBehaviour
    {
        [Header("Touch Fader")] // header
        [SerializeField]
        private Axis axis = Axis.Y;

        [SerializeField] private Vector2 range = new Vector2(0, 1);
        [SerializeField] private float defaultValue = 0.25f;
        private float _normalizedDefault;

        // [SerializeField] private TouchFaderHandle faderHandle;

        private Vector3 _axisVector = Vector3.zero;

        [InspectorName("minPosition"),
         SerializeField]
        private Transform minLimit;

        [InspectorName("maxPosition"),
         SerializeField]
        private Transform maxLimit;

        [Header("Drivers")] // header
        [SerializeField]
        private Transform valueIndicator;

        [SerializeField] private Transform targetIndicator;

        [SerializeField] private Transform isAuthorizedIndicator;

        private Rigidbody _rigidbody;
        
        protected override string LogPrefix => $"{nameof(ContactFaderDeprecated)} {name}";

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
                Log($"set state to {_syncedValueNormalized} => {prevValue}");
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

        private GameObject faderHandle = null;
        
        // private VRC_Pickup pickup;
        // private Rigidbody _rigidBody;
        private VRCPlayerApi _localPlayer;
        private float _lastValue;
        private bool _isHeldLocally;
        private bool _isDesktop = false;
        private bool _isInVR;

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
                _isInVR = _localPlayer.IsUserInVR();
            }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
            minLimit.transform.MarkDirty();
            maxLimit.transform.MarkDirty();
#endif

            smoothedCurrentNormalized = _normalizedDefault;
            smoothingTargetNormalized = _normalizedDefault;
            // enableValueSmoothing = enableValueSmoothing && smoothingUpdateInterval > 0;

            //TODO: move into running in editor
            _valueFloatDrivers = valueIndicator.GetComponentsInChildren<FloatDriver>();
            _targetFloatDrivers = targetIndicator.GetComponentsInChildren<FloatDriver>();

            if (isAuthorizedIndicator != null)
            {
                _isAuthorizedBoolDrivers = isAuthorizedIndicator.GetComponentsInChildren<BoolDriver>();
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
        private void SetupFaderHandle()
        {
            faderHandle = this.gameObject;
//             if (faderHandle)
//             {
//                 // faderHandle.touchFaderWithHandle = this;
//                 faderHandle.leftHandCollider = _leftHandCollider;
//                 faderHandle.rightHandCollider = _rightHandCollider;
//
//                 // _interactCallback = faderHandle.GetComponent<InteractCallback>();
// #if UNITY_EDITOR && !COMPILER_UDONSHARP
//                 faderHandle.EditorACL = accessControl;
//                 faderHandle.enforceACL = enforceACL;
//                 faderHandle.EditorDebugLog = debugLog;
//                 faderHandle.MarkDirty();
// #endif
//             }
//             else
//             {
//                 LogError("missing fader handle");
//             }
        }

        protected override void _Init()
        {
            SetupValuesAndComponents();
            SetupFaderHandle();

            OnDeserialization();
            UpdateValueIndicator(
                Mathf.Lerp(_minPos, _maxPos, smoothedCurrentNormalized)
            );
            UpdateTargetIndicator(
                Mathf.Lerp(_minPos, _maxPos, smoothingTargetNormalized)
            );

            DisableInteractive = _isInVR;
            // pickup.transform.SetPositionAndRotation(pickupReset.position, pickupReset.rotation);
        }

        protected override void AccessChanged()
        {
            for (var i = 0; i < _isAuthorizedBoolDrivers.Length; i++)
            {
                _isAuthorizedBoolDrivers[i].UpdateBool(isAuthorized);
            }
            DisableInteractive = !isAuthorized || _isInVR;
        }

        [NonSerialized] public bool rightGrabbed;
        [NonSerialized] public bool leftGrabbed;

        public override void InputGrab(bool value, UdonInputEventArgs args)
        {
            if (!_isInVR) return;
            //Log($"InputGrab({value}, {args.handType})");
            if (value)
            {
                // if (!_leftGrabbed && !_rightGrabbed)
                // {
                //     Log("starting FollowCollider");
                //     this.SendCustomEventDelayedFrames(nameof(_OnFollowCollider), 1);
                // }

                if (args.handType == HandType.LEFT)
                {
                    if (!leftGrabbed)
                    {
                       // Log($"LeftGrabbed()");
                    }

                    leftGrabbed = true;
                }

                if (args.handType == HandType.RIGHT)
                {
                    if (!rightGrabbed)
                    {
                        //Log($"RightGrabbed()");
                    }

                    rightGrabbed = true;
                }
            }
            else
            {
                if (args.handType == HandType.LEFT)
                {
                    if (leftGrabbed)
                    {
                        // Log($"LeftReleased()");
                    }

                    leftGrabbed = false;
                }

                if (args.handType == HandType.RIGHT)
                {
                    if (rightGrabbed)
                    {
                        //Log($"RightReleased()");
                    }

                    rightGrabbed = false;
                }
            }
        }
        
        public override void Reset()
        {
            if (!isAuthorized) return;
            _syncedValueNormalized = _normalizedDefault;
            if (synced)
            {
                RequestSerialization();
            }

            OnDeserialization();
        }

        public override void SetValue(float normalizedValue)
        {
            if (!isAuthorized) return;
            _syncedValueNormalized = normalizedValue;
            if (synced)
            {
                RequestSerialization();
            }

            OnDeserialization();
        }
        
        private bool _isInteracting = false;
        public override void Interact()
        {
            if (!isAuthorized) return;
            _isInteracting = true;
            _HandleInteract();
        }

        public override void InputUse(bool value, UdonInputEventArgs args)
        {
            if (!_isInteracting) return;
            if (!isAuthorized) return;
            if (!value)
            {
                _isInteracting = false;
                _HandleRelease();
            }
        }
        
        public void _HandleInteract()
        {
            if (!isAuthorized) return;
            if (_isInVR) return;

            TakeOwnership();

            Log("Interact");
            DesktopPickup();
        }

        public void _HandleRelease()
        {
            if (!isAuthorized) return;
            if (_isInVR) return;

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
            if(_isHeldLocally)
            {
                Log($"Desktop Drop with target at {_syncedValueNormalized}");
            }
            _isHeldLocally = false;
            _syncedIsBeingManipulated = false;

            if (synced)
            {
                RequestSerialization();
            }
        }

        private const float DesktopDampening = 20;

        public override void InputLookVertical(float value, UdonInputEventArgs args)
        {
            if (!isAuthorized) return;
            if (_isInVR) return;
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

        private bool _isTrackingLeft = false;
        private bool _isTrackingRight = false;
        private bool _isRunningLoop = false;
        
        public void _OnFollowCollider()
        {
            if (!isAuthorized) return;
            // if (_isDesktop) return; // should not be required

            var followLeft = leftGrabbed && _inLeftTrigger;
            var followRight = rightGrabbed && _inRightTrigger;

            if (_inLeftTrigger || _inRightTrigger)
            {
                this.SendCustomEventDelayedFrames(nameof(_OnFollowCollider), 1);
                _isRunningLoop = true;
            }
            else
            {
                Log("stopping follow collider loop");
                _isRunningLoop = false;
            }
            
            if (_isTrackingLeft && !followLeft)
            {
                Log($"VR Drop with target at {_syncedValueNormalized}");
                _isTrackingLeft = false;
                // if (synced)
                // {
                //     RequestSerialization();
                // }

                _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 1f, 1f, 0.2f);
                return;
            }
            if (_isTrackingRight && !followRight)
            {
                Log($"VR Drop with target at {_syncedValueNormalized}");
                _isTrackingRight = false;
                // if (synced)
                // {
                //     RequestSerialization();
                // }

                _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 1f, 1f, 0.2f);
                return;
            }

            if (followLeft)
            {

                if (!_isTrackingLeft)
                {
                    _isTrackingLeft = true;
                    _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 1f, 1f, 0.2f);
                }
            }
            else if (followRight)
            {

                if (!_isTrackingRight)
                {
                    _isTrackingRight = true;
                    _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 1f, 1f, 0.2f);
                }
            }

            // if ((fingerContactTracker.rightGrabbed && _inRightTrigger) || (fingerContactTracker.leftGrabbed && _inLeftTrigger))
            if (followLeft || followRight)
                //if(_isColliding)
            {
                _syncedIsBeingManipulated = true;
                // Transform handData = _inRightTrigger ? _rightHandCollider.transform : _leftHandCollider.transform;
                // var localFingerPos = transform.InverseTransformPoint(handData.position);
                Vector3 globalFingerPos = _inRightTrigger ? _getRightFingerPos() : _getLeftFingerPos();
                var localFingerPos = transform.parent.InverseTransformPoint(globalFingerPos);
                float positionComponent = localFingerPos[(int)axis];

                var clampedPos = Mathf.Clamp(
                    positionComponent,
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
            // else if(_syncedIsBeingManipulated)
            // {
            //     Log($"VR Drop with target at {_syncedValueNormalized}");
            //     _syncedIsBeingManipulated = false;
            //     
            //     if (synced)
            //     {
            //         RequestSerialization();
            //     }  
            //     if (_inLeftTrigger)
            //     {
            //         _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 1f, 1f, 0.2f);
            //     }
            //
            //     if (_inRightTrigger)
            //     {
            //         _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 1f, 1f, 0.2f);
            //     }
            // }
        }

        [NonSerialized] public ContactSenderProxy leftSender, rightSender;

        private Vector3 _getLeftFingerPos()
        {
            //var _leftSender = faderHandle._leftSender;
            if (leftSender != null)
            {
                return leftSender.position;
            }

            Vector3 _leftHandPos = _localPlayer.GetBonePosition(HumanBodyBones.LeftIndexDistal);
            if (_leftHandPos == Vector3.zero)
            {
                _leftHandPos = _localPlayer.GetBonePosition(HumanBodyBones.LeftIndexIntermediate);
            }

            return _leftHandPos;
        }

        private Vector3 _getRightFingerPos()
        {
            //var _rightSender = faderHandle._rightSender;
            if (rightSender != null)
            {
                return rightSender.position;
            }

            Vector3 _rightHandPos = _localPlayer.GetBonePosition(HumanBodyBones.RightIndexDistal);
            if (_rightHandPos == Vector3.zero)
            {
                _rightHandPos = _localPlayer.GetBonePosition(HumanBodyBones.RightIndexIntermediate);
            }

            return _rightHandPos;
        }

        public void OnLeftContactEnter()
        {
            if (!isAuthorized) return;
            Log($"received {leftSender.usage}");
            Log($"Left Contact Enter");

            if (!_inLeftTrigger)
            {
                Log($"VR Pickup with target at {_syncedValueNormalized}");
                if (!_isRunningLoop)
                {
                    Log("starting FollowCollider");
                    this.SendCustomEventDelayedFrames(nameof(_OnFollowCollider), 1);
                    _isRunningLoop = true;
                }
                else
                {
                    LogWarning("loop already running");
                }
            }

            TakeOwnership();

            _inLeftTrigger = true;
            //_localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 1f, 1f, 0.2f);
        }

        public void OnRightContactEnter()
        {
            if (!isAuthorized) return;
            Log($"received {rightSender.usage}");
            Log($"Right Contact Enter");

            if (!_inRightTrigger)
            {
                Log($"VR Pickup with target at {_syncedValueNormalized}");
                if (!_isRunningLoop)
                {
                    Log("starting FollowCollider");
                    this.SendCustomEventDelayedFrames(nameof(_OnFollowCollider), 1);
                    _isRunningLoop = true;
                }
                else
                {
                    LogWarning("loop already running");
                }
            }

            TakeOwnership();

            _inRightTrigger = true;
            //_localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 1f, 1f, 0.2f);
        }

        public void OnLeftContactExit()
        {
            if (!isAuthorized) return;
            Log($"Left Contact Exit");
            _inLeftTrigger = false;
            // _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 1f, 1f, 0.2f);
        }

        public void OnRightContactExit()
        {
            if (!isAuthorized) return;
            Log($"Right Contact Exit");
            _inRightTrigger = false;
            // _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 1f, 1f, 0.2f);
        }
        
        // [NonSerialized] public ContactSenderProxy _leftSender, _rightSender;
        public override void OnContactEnter(ContactEnterInfo contactInfo)
        {
            if (!_isInVR) return;
            if (!contactInfo.contactSender.player.isLocal) return;
            if (!isAuthorized) return;

            Log($"Contact Enter {contactInfo.contactPoint} {contactInfo.matchingTags.Length}");
            for (var i = 0; i < contactInfo.matchingTags.Length; i++)
            {
                var matchingTag = contactInfo.matchingTags[i];
                Log(matchingTag);
            }

            if (contactInfo.matchingTags.Contains("FingerIndexL"))
            {
                leftSender = contactInfo.contactSender;
                OnLeftContactEnter();
                return;
            }

            if (contactInfo.matchingTags.Contains("FingerIndexR"))
            {
                rightSender = contactInfo.contactSender;
                OnRightContactEnter();
                return;
            }
        }

        public override void OnContactExit(ContactExitInfo contactInfo)
        {
            if (!_isInVR) return;
            if (!contactInfo.contactSender.player.isLocal) return;
            if (!isAuthorized) return;
            Log($"Contact Exit");

            if (contactInfo.contactSender == leftSender)
            {
                Log($"Contact Exit Left");
                leftSender = null;
                OnLeftContactExit();
            }

            if (contactInfo.contactSender == rightSender)
            {
                Log($"Contact Exit Right");
                rightSender = null;
                OnRightContactExit();
            }
        }

        /*
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
        
                if (!_inLeftTrigger && faderHandle.leftGrabbed)
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
        
                if (!_inRightTrigger && faderHandle.rightGrabbed)
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
        */

        protected override void UpdateTargetIndicator(float clampedPos)
        {
            // if (!enableValueSmoothing) return;
            if (targetIndicator == null) return;
            Log($"update target indicator {clampedPos}");
            // target worldPos --> min local space
            Vector3 newPos = minLimit.transform.parent.InverseTransformPoint(
                faderHandle.transform.position
            );
            // update axis clamped pos
            newPos[(int)axis] = clampedPos;
            // min local space -> world pos
            // set position
            targetIndicator.transform.position = minLimit.transform.parent.TransformPoint(newPos);

            UpdateHandlePosition();

#if UNITY_EDITOR && !COMPILER_UDONSHARP
            targetIndicator.transform.MarkDirty();
#endif
        }

        protected override void UpdateValueIndicator(float clampedPos)
        {
            if (valueIndicator == null) return;
            Log($"update value indicator {clampedPos}");
            // Vector3 newPos = valueIndicator.transform.localPosition;
            // // Vector3 newPos = minLimit.transform.position;
            // newPos[(int)axis] = clampedPos;
            // valueIndicator.transform.position = newPos;
            
            // reference worldPos --> min localPos
            Vector3 newPos = minLimit.transform.parent.InverseTransformPoint(
                faderHandle.transform.position
            );
            // update axis clamped pos
            newPos[(int)axis] = clampedPos;
            // min local space -> world pos
            // set position
            valueIndicator.transform.position = minLimit.transform.parent.TransformPoint(newPos);

#if UNITY_EDITOR && !COMPILER_UDONSHARP
            valueIndicator.transform.MarkDirty();
#endif
        }

        private void UpdateHandlePosition()
        {
            // parentConstraint.GlobalWeight = 1;
            faderHandle.transform.SetPositionAndRotation(
                targetIndicator.position,
                targetIndicator.rotation
            );

#if UNITY_EDITOR && !COMPILER_UDONSHARP
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


        // [NonSerialized] private AccessControl prevAccessControl;
        // [NonSerialized] private bool prevEnforceACL;
        // [NonSerialized] private DebugLog prevDebugLog;
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

            // if (prevAccessControl != accessControl
            //     || prevEnforceACL != enforceACL
            //     || prevDebugLog != debugLog
            //    )
            // {
            //     ApplyACLsAndLog();
            //     prevAccessControl = accessControl;
            //     prevDebugLog = debugLog;
            // }
        }

        [ContextMenu("Apply Values")]
        public void ApplyValues()
        {
            SetupValuesAndComponents();
            // SetupFingerTracker();
            SetupFaderHandle();
            OnDeserialization();
            UpdateValueIndicator(
                Mathf.Lerp(_minPos, _maxPos, smoothedCurrentNormalized)
            );
            UpdateTargetIndicator(
                Mathf.Lerp(_minPos, _maxPos, smoothingTargetNormalized)
            );

            Debug.Log("Applying target and value floats");
            foreach (var valueFloatDriver in _valueFloatDrivers)
            {
                valueFloatDriver.ApplyFloatValue(
                    Math.Clamp(defaultValue,_minValue,_maxValue)
                );
            }

            foreach (var targetFloatDriver in _targetFloatDrivers)
            {
                targetFloatDriver.ApplyFloatValue(
                    Math.Clamp(defaultValue,_minValue,_maxValue)
                );
            }
        }

        // [ContextMenu("Apply ACLs and Log")]
        // private void ApplyACLsAndLog()
        // {
        //     // _interactCallback.EditorACL = accessControl;
        //     // _interactCallback.EditorDebugLog = debugLog;
        //     // _interactCallback.EditorEnforceACL = enforceACL;
        //     // _interactCallback.MarkDirty();
        //
        //     faderHandle.EditorACL = accessControl;
        //     faderHandle.EditorDebugLog = debugLog;
        //     faderHandle.EditorEnforceACL = enforceACL;
        //     faderHandle.MarkDirty();
        // }
#endif
    }
}
