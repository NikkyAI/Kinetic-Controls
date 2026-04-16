using nikkyai.common;
using nikkyai.Kinetic_Controls;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace nikkyai.driver
{
    public class FloatSetRotation : FloatDriver
    {
        [SerializeField]
        private Axis axis = Axis.X;

        [SerializeField] private Transform target;

        private Vector3 vectorAxis = Vector3.zero;
        
        void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            base._Init();
            
            vectorAxis[(int)axis] = 1;
        }

        protected override string LogPrefix => nameof(FloatSetRotation);

        protected override void OnUpdateFloat(float value)
        {
            if (!enabled) return;

            if (Utilities.IsValid(target))
            {
                var rotationVector = target.localRotation.eulerAngles;
                rotationVector[(int)axis] = value;
                target.localRotation = Quaternion.Euler(rotationVector);
            }
            else
            {
                LogWarning("reference target is invalid");
            }
        }
    }
}
