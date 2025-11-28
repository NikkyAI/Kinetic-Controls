using System;
using nikkyai.common;
using Texel;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace nikkyai.Kinetic_Controls
{
    // [DefaultExecutionOrder(-5)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FingerContactTracker : LoggerBase
    {
        [SerializeField] public Collider leftHandCollider;
        [SerializeField] public Collider rightHandCollider;

        private Vector3 _leftResetPos = Vector3.zero;
        private Vector3 _rightResetPos = Vector3.zero;
        private Transform _leftReset = null;
        private Transform _rightReset = null;
        
        #region Debug

        [Header("Debug")] // header
        [SerializeField]
        private DebugLog debugLog;

        protected override string LogPrefix => $"{nameof(FingerContactTracker)} {name}";

        protected override DebugLog DebugLog
        {
            get => debugLog;
            set => debugLog = value;
        }

        #endregion

        private VRCPlayerApi _localPlayer;
        private Vector3 _leftHandPos = Vector3.zero;
        private Vector3 _rightHandPos = Vector3.zero;
        // private Quaternion _leftHandRot = Quaternion.identity;
        // private Quaternion _rightHandRot = Quaternion.identity;
        // private Transform _leftHand = null;
        // private Transform _rightHand = null;
        [NonSerialized] public bool rightGrabbed;
        [NonSerialized] public bool leftGrabbed;

        private bool _isInVR;

        private void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            base._Init();
            _leftResetPos = leftHandCollider.transform.position;
            _rightResetPos = rightHandCollider.transform.position;
            _localPlayer = Networking.LocalPlayer;
            _isInVR = _localPlayer.IsUserInVR();

            // if (!_isInVR)
            // {
            //     Debug.Log("Not in VR, stopping finger contact tracker");
            //     gameObject.SetActive(false);
            // }
        }

        public override void InputGrab(bool value, UdonInputEventArgs args)
        {
            if (!_isInVR) return;
            Log($"InputGrab({value}, {args.handType})");
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
                        Log($"LeftGrabbed()");
                    }

                    leftGrabbed = true;
                }

                if (args.handType == HandType.RIGHT)
                {
                    if (!rightGrabbed)
                    {
                        Log($"RightGrabbed()");
                    }

                    rightGrabbed = true;
                }
            }
            else
            {
                if (args.handType == HandType.LEFT)
                {
                    if (leftGrabbed) Log($"LeftReleased()");

                    leftGrabbed = false;
                }

                if (args.handType == HandType.RIGHT)
                {
                    if (rightGrabbed) Log($"RightReleased()");

                    rightGrabbed = false;
                }
            }
        }

        public void Update()
        {
            if (_localPlayer.isSuspended) return;
            if (!_isInVR) return;
            
             //TODO: scale with avatar ?
             // use Networking.LocalPlayer.GetBoneTransform

             // _rightHand = _rightReset;
             // if (rightGrabbed)
             // {
             //     _rightHand = _localPlayer.GetBoneTransform(HumanBodyBones.RightIndexDistal);
             //     if (!_rightHand)
             //     {
             //         _rightHand = _localPlayer.GetBoneTransform(HumanBodyBones.RightIndexIntermediate);
             //     }
             // }
             //
             // _leftHand = _leftReset;
             // if (leftGrabbed)
             // {
             //     _leftHand = _localPlayer.GetBoneTransform(HumanBodyBones.LeftIndexDistal);
             //     if (!_leftHand)
             //     {
             //         _leftHand = _localPlayer.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate);
             //     }
             // }

             _rightHandPos = _rightResetPos;
            // _rightHandRot = Quaternion.identity;
            if (rightGrabbed)
            {
                _rightHandPos = _localPlayer.GetBonePosition(HumanBodyBones.RightIndexDistal);
                if (_rightHandPos == Vector3.zero)
                {
                    _rightHandPos = _localPlayer.GetBonePosition(HumanBodyBones.RightIndexIntermediate);
                }
            
                // _rightHandRot = _localPlayer.GetBoneRotation(HumanBodyBones.RightIndexDistal);
                // if (_rightHandRot == Quaternion.identity)
                // {
                //     _leftHandRot = _localPlayer.GetBoneRotation(HumanBodyBones.RightIndexIntermediate);
                // }
            }
            
            _leftHandPos = _leftResetPos;
            // _leftHandRot = Quaternion.identity;
            if (leftGrabbed)
            {
                _leftHandPos = _localPlayer.GetBonePosition(HumanBodyBones.LeftIndexDistal);
                if (_leftHandPos == Vector3.zero)
                {
                    _leftHandPos = _localPlayer.GetBonePosition(HumanBodyBones.LeftIndexIntermediate);
                }
            
                // _leftHandRot = _localPlayer.GetBoneRotation(HumanBodyBones.LeftIndexDistal);
                // if (_rightHandRot == Quaternion.identity)
                // {
                //     _leftHandRot = _localPlayer.GetBoneRotation(HumanBodyBones.LeftIndexIntermediate);
                // }
            }

            // rightHandCollider.transform.SetPositionAndRotation(_rightHandPos, _rightHandRot);
            // leftHandCollider.transform.SetPositionAndRotation(_leftHandPos, _leftHandRot);

            rightHandCollider.transform.position = _rightHandPos;
            leftHandCollider.transform.position = _leftHandPos;

            // rightHandCollider.transform.SetPositionAndRotation(_rightHand.position, _rightHand.rotation);
            // leftHandCollider.transform.SetPositionAndRotation(_leftHand.position, _leftHand.rotation);
        }
    }
}