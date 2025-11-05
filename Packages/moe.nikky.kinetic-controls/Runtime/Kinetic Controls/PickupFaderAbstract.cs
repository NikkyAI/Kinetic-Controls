using nikkyai.Base;
using nikkyai.Kinetic_Controls.Drivers;
using Texel;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

// ReSharper disable ForCanBeConvertedToForeach

namespace nikkyai.Kinetic_Controls
{
    internal enum Axis
    {
        X = 0,
        Y = 1,
        Z = 2,
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public abstract class PickupFaderAbstract : ACLBase
    {
        [SerializeField] private Axis axis = Axis.Y;
        [SerializeField] private Vector2 range = new Vector2(0, 1);
        [SerializeField] private float defaultValue = 0.25f;
        private float _normalizedDefault;
        [SerializeField] private PickupTrigger pickupTrigger;

        [SerializeField] private Transform pickupReset, minLimit, maxLimit;
        
        [SerializeField] private FloatDriver[] floatDrivers = { };
        //TODO: find at buildtime and update

        // [SerializeField] private bool syncPickup = true;
        private Rigidbody _rigidbody;
        private bool _pickupHasObjectSync = false;

        [Header("Access Control")] // header
        [SerializeField]
        private bool useACL = true;

        protected override bool UseACL => useACL;

        [Tooltip("ACL used to check who can use the toggle")] [SerializeField]
        private AccessControl accessControl;

        protected override AccessControl AccessControl
        {
            get => accessControl;
            set => accessControl = value;
        }

        [Header("Smoothing")] // header
        
        [Tooltip("smoothes out value updates over time, may impact CPU frametimes"),
        SerializeField] 
        private bool enableValueSmoothing = true;
        
        [Tooltip("amount of frames to skip when approaching target value," +
                 "higher number == less load, but more choppy smoothing"),
         SerializeField]
        private int smoothingUpdateInterval = 5;
        

        [Tooltip("fraction of the distance covered within roughly 1s"),
         SerializeField]
        private float smoothingRate = 0.5f;

        [Header("Debug")] // header
        [SerializeField]
        private DebugLog debugLog;

        protected override DebugLog DebugLog
        {
            get => debugLog;
            set => debugLog = value;
        }

        [Header("State")] // header
        [UdonSynced]
        // IMPORTANT, DO NOT DELETE
        private float _syncedValueNormalized;

        private float _lastSyncedValueNormalized = 0;

        // internal values

        private float _minPos, _maxPos;
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
        private const float epsilon = 0.01f;
        private bool valueInitialized = false;
        private bool isSmoothing = false;

        #endregion

        protected override void _Init()
        {
            // _updateFloatSynced = UpdateFloatSynced;
            _minValue = range.x;
            _maxValue = range.y;
            _minPos = minLimit.localPosition[(int)axis];
            _maxPos = maxLimit.localPosition[(int)axis];
            _normalizedDefault = Mathf.InverseLerp(_minValue, _maxValue, defaultValue);

            smoothedCurrentNormalized = _normalizedDefault;
            enableValueSmoothing = enableValueSmoothing && smoothingUpdateInterval > 0;

            if (pickupTrigger == null)
            {
                pickupTrigger = gameObject.GetComponent<PickupTrigger>();
            }

            if (pickupTrigger)
            {
                if (pickup == null)
                {
                    pickup = pickupTrigger.GetComponent<VRC_Pickup>();
                }

                pickupTrigger.accessControl = accessControl;
                pickupTrigger.enforceACL = UseACL;
                pickupTrigger._Register(PickupTrigger.EVENT_PICKUP, this, nameof(_OnPickup));
                pickupTrigger._Register(PickupTrigger.EVENT_DROP, this, nameof(_OnDrop));
            }
            else
            {
                LogError("missing pickup trigger");
            }

            if (pickup == null)
            {
                pickup = gameObject.GetComponent<VRC_Pickup>();
            }

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

            if (pickupReset == null)
            {
                pickupReset = transform;
            }

            OnDeserialization();
            UpdateIndicatorPosition(
                Mathf.Lerp(_minPos, _maxPos, smoothedCurrentNormalized)
            );
            UpdatePickupPosition();

            pickup.transform.SetPositionAndRotation(pickupReset.position, pickupReset.rotation);
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
            // this.SendCustomEventDelayedFrames(nameof(FollowPickup), 1);
            _OnFollowPickup();
        }

        public void _OnDrop()
        {
            isHeldLocally = false;

            TakeOwnership();
            RequestSerialization();
            OnDeserialization();

            Log("handle released, resetting position");
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(UpdatePickupPosition));
        }

        public void _OnFollowPickup()
        {
            if (!isHeldLocally) return;

            var clampedPos = Mathf.Clamp(
                pickup.transform.localPosition[(int)axis],
                _minPos,
                _maxPos
            );

            // UpdateIndicatorPosition(clampedPos);

            _syncedValueNormalized = Mathf.InverseLerp(
                _minPos,
                _maxPos,
                clampedPos
            );

            if (isHeldLocally)
            {
                this.SendCustomEventDelayedFrames(nameof(_OnFollowPickup), 1);
            }

            if (_syncedValueNormalized != _lastSyncedValueNormalized)
            {
                RequestSerialization();
                OnDeserialization();
                _lastSyncedValueNormalized = _syncedValueNormalized;
            }
        }

        private void _UpdateTargetValue(float normalizedTargetValue)
        {
            // immediate update
            if (!enableValueSmoothing)
            {
                var floatValue = Mathf.Lerp(_minValue, _maxValue, normalizedTargetValue);
                for (var i = 0; i < floatDrivers.Length; i++)
                {
                    floatDrivers[i].UpdateFloat(floatValue);
                }
                // _UpdateFloat(
                //     Mathf.Lerp(_minValue, _maxValue, normalizedTargetValue)
                // );
                UpdateIndicatorPosition(
                    Mathf.Lerp(_minPos, _maxPos, normalizedTargetValue)
                );
                return;
            }

            // value smoothing
            if (!valueInitialized)
            {
                // smoothingTargetNormalized = _normalizedDefault;
                smoothedCurrentNormalized = _normalizedDefault;
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
            Log($"UpdateLoop {smoothedCurrentNormalized} => {smoothingTargetNormalized}");

            smoothedCurrentNormalized = Mathf.Lerp(
                smoothingTargetNormalized,
                smoothedCurrentNormalized,
                Mathf.Exp(-smoothingRate * Time.deltaTime * smoothingUpdateInterval)
            );

            if (Mathf.Abs(smoothingTargetNormalized - smoothedCurrentNormalized) <= epsilon)
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
            for (var i = 0; i < floatDrivers.Length; i++)
            {
                floatDrivers[i].UpdateFloat(floatValue);
            }
            _UpdateFloat(
                floatValue
                // Mathf.Lerp(_minValue, _maxValue, smoothedCurrentNormalized)
            );
            UpdateIndicatorPosition(
                Mathf.Lerp(_minPos, _maxPos, smoothedCurrentNormalized)
            );
        }

        private void UpdateIndicatorPosition(float clampedPos)
        {
            Vector3 newPos = pickupReset.transform.localPosition;
            newPos[(int)axis] = clampedPos;
            pickupReset.transform.localPosition = newPos;
        }

        public void UpdatePickupPosition()
        {
            pickupRigidBody.angularVelocity = Vector3.zero;
            pickupRigidBody.velocity = Vector3.zero;
            if (enableValueSmoothing)
            {
                var clampedPos = Mathf.Lerp(_minPos, _maxPos, smoothingTargetNormalized);
                var newPos = pickupReset.transform.localPosition;
                newPos[(int)axis] = clampedPos;

                pickup.transform.SetLocalPositionAndRotation(
                    newPos,
                    pickupReset.localRotation
                );
            }
            else
            {
                pickup.transform.SetLocalPositionAndRotation(
                    pickupReset.localPosition, 
                    pickupReset.localRotation
                );
            }
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
                if (!_pickupHasObjectSync && !isHeldLocally)
                {
                    UpdatePickupPosition();
                }

                _lastSyncedValueNormalized = _syncedValueNormalized;
            }
        }

        protected abstract void _UpdateFloat(float val);
    }
}