using System;
using nikkyai.toggle.common;
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
        [SerializeField] public GameObject leftHandCollider;
        [SerializeField] public GameObject rightHandCollider;

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

        private VRCPlayerApi _currentPlayer;
        private Vector3 _leftHandPos = Vector3.zero;
        private Vector3 _rightHandPos = Vector3.zero;
        private Quaternion _leftHandRot = Quaternion.identity;
        private Quaternion _rightHandRot = Quaternion.identity;
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
            _currentPlayer = Networking.LocalPlayer;
            _isInVR = _currentPlayer.IsUserInVR();

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
            if (_currentPlayer.isSuspended) return;
            if (!_isInVR) return;

            _rightHandPos = Vector3.zero;
            _rightHandRot = Quaternion.identity;
            if (rightGrabbed)
            {
                _rightHandPos = _currentPlayer.GetBonePosition(HumanBodyBones.RightIndexDistal);
                if (_rightHandPos == Vector3.zero)
                {
                    _rightHandPos = _currentPlayer.GetBonePosition(HumanBodyBones.RightIndexIntermediate);
                }

                _rightHandRot = _currentPlayer.GetBoneRotation(HumanBodyBones.RightIndexDistal);
                if (_rightHandRot == Quaternion.identity)
                {
                    _leftHandRot = _currentPlayer.GetBoneRotation(HumanBodyBones.RightIndexIntermediate);
                }
            }

            _leftHandPos = Vector3.zero;
            _leftHandRot = Quaternion.identity;
            if (leftGrabbed)
            {
                _leftHandPos = _currentPlayer.GetBonePosition(HumanBodyBones.LeftIndexDistal);
                if (_leftHandPos == Vector3.zero)
                {
                    _leftHandPos = _currentPlayer.GetBonePosition(HumanBodyBones.LeftIndexIntermediate);
                }

                _leftHandRot = _currentPlayer.GetBoneRotation(HumanBodyBones.LeftIndexDistal);
                if (_rightHandRot == Quaternion.identity)
                {
                    _leftHandRot = _currentPlayer.GetBoneRotation(HumanBodyBones.LeftIndexIntermediate);
                }
            }

            rightHandCollider.transform.SetPositionAndRotation(_rightHandPos, _rightHandRot);
            leftHandCollider.transform.SetPositionAndRotation(_leftHandPos, _leftHandRot);
        }
    }
}