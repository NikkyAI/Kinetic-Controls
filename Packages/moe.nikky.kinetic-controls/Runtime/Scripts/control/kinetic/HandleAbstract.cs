using nikkyai.common;
using nikkyai.extensions;
using UnityEngine;
using UnityEngine.Serialization;
using VRC;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace nikkyai.control.kinetic
{
    public abstract class HandleAbstract : ACLBaseReadonly
    {
        protected bool IsHeldLocally = false;

        [SerializeField]
        [ReadOnly]
        protected BaseKineticControl[] controlBehaviours = { };

        [Header("Handle - Internals")]
        [Tooltip(
            "should be the same as targetIndicator or a child, " +
            "handle will be reset to the given transform position / rotation on release")]
        [SerializeField]
#if HIDE_INSPECTOR
        [HideInInspector]
#endif
        [ReadOnly]
        public Transform resetTransform;

        [SerializeField]
#if HIDE_INSPECTOR
        [HideInInspector]
#endif
        [ReadOnly]
        private Rigidbody rigidBody;

        protected override void AccessChanged()
        {
            if (IsInVR || IsHeldLocally)
            {
                 DisableInteractive = true;
            }
            else
            {
                DisableInteractive = !IsAuthorized;
            }
        }

        public override void Interact()
        {
            if (IsInVR || !IsAuthorized)
            {
                return;
            }

            IsHeldLocally = true;
            DisableInteractive = true;
            foreach (var baseKineticControl in controlBehaviours)
            {
                baseKineticControl.OnMoveHandle(transform.position);
            }

            _OnFollowInteract();
        }

        public override void InputUse(bool value, UdonInputEventArgs args)
        {
            base.InputUse(value, args);
            if (IsInVR || !IsAuthorized)
            {
                return;
            }

            if (!value && IsHeldLocally)
            {
                IsHeldLocally = false;
                foreach (var baseKineticControl in controlBehaviours)
                {
                    baseKineticControl.OnDropHandle();
                    // baseKineticControl.UpdateHandlePosition();
                    if (!IsInVR)
                    {
                        baseKineticControl.DebugDesktopRaytrace(false);
                    }
                }

                ResetTransform();
                AccessChanged();
            }
        }

        // public void OnRelease()
        // {
        //     Log("OnRelease");
        //     if (!IsAuthorized)
        //         return;
        //
        //     if (!IsAuthorized)
        //     {
        //         return;
        //     }
        //
        //     // foreach (var baseKineticControl in controlBehaviours)
        //     // {
        //     //     baseKineticControl.TakeOwnership();
        //     // }
        //
        //     IsHeldLocally = false;
        //     foreach (var baseKineticControl in ControlBehaviours)
        //     {
        //         baseKineticControl.OnDropHandle();
        //         // baseKineticControl.UpdateHandlePosition();
        //         if (!IsInVR)
        //         {
        //             baseKineticControl.DebugDesktopRaytrace(false);
        //         }
        //     }
        //
        //     ResetTransform();
        //
        //     // Log("handle released, resetting position");
        //     // SendCustomNetworkEvent(NetworkEventTarget.All, nameof(UpdatePickupPosition));
        // }

        public void _OnFollowInteract()
        {
            if (!IsHeldLocally) return;
            foreach (var baseKineticControl in controlBehaviours)
            {
                baseKineticControl.FollowDesktop();
            }

            // FollowPickup();
            if (IsHeldLocally)
            {
                this.SendCustomEventDelayedFrames(nameof(_OnFollowInteract), 0);
            }
        }

        public abstract void ResetTransformIfNotManipulated();
        // {
        //     // if (!pickupHasObjectSync && !IsHeldLocally)
        //     // {
        //     //     ResetTransform();
        //     // }
        //     // ResetTransform();
        // }

        public void ResetTransform()
        {
            FreezeRigidBody();

            if (Utilities.IsValid(resetTransform))
            {
                //LogWarning($"handle reset");
                transform.SetPositionAndRotation(
                    resetTransform.position,
                    resetTransform.rotation
                );
            }
            else
            {
                LogWarning("reset transform is not valid");
            }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
            transform.MarkDirty();
#endif
        }

        private void FreezeRigidBody()
        {
            if (Utilities.IsValid(rigidBody))
            {
                rigidBody.velocity = Vector3.zero;
                rigidBody.maxAngularVelocity = 0;
                rigidBody.angularVelocity = Vector3.zero;
            }
            else
            {
                LogError("Rigid body is not valid");
            }
        }

        public void RegisterRuntime(BaseKineticControl baseKineticControl)
        {
            Log($"registering {baseKineticControl}");
            controlBehaviours = controlBehaviours.AddUnique(baseKineticControl);
        }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        internal virtual void Setup()
        {
            InteractionText = "Click and drag to adjust";
            FindBoolAuthDrivers();
            SetupRigidbody();
        }

        private void SetupRigidbody()
        {
            Log("SetupPickupRigidbody");
            rigidBody = GetComponent<Rigidbody>();
            rigidBody.useGravity = false;
            rigidBody.isKinematic = true;
            rigidBody.drag = 10f;
            rigidBody.angularDrag = 5f;
            rigidBody.MarkDirty();
        }
#endif
    }
}