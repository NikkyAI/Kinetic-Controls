using nikkyai.ArrayExtensions;
using nikkyai.common;
using UnityEngine;
using VRC;

namespace nikkyai.driver
{
    public class BoolScaleByPostfixDriver : BoolDriver
    {
        // [SerializeField] private Transform[] targetsOn = { };
        // [SerializeField] private Transform[] targetsOff = { };

        [SerializeField] private Transform findInChildren = null;
        [SerializeField] private string offTargetsPostfix = "S1";
        [SerializeField] private string onTargetsPostfix = "S2";
    
        private Transform[] _targetsOn = { };
        private Transform[] _targetsOff = { };
    
        protected override string LogPrefix => $"{nameof(BoolScaleByPostfixDriver)} {name}";
        private void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            base._Init();
        
            //TODO: find all 
            if (findInChildren == null)
            {
                findInChildren = transform;
            }

            var candidates = findInChildren.GetComponentsInChildren<Transform>();

            foreach (var candidate in candidates)
            {
                if (candidate.name.EndsWith(offTargetsPostfix))
                {
                    _targetsOff.Add(candidate);
                } else if (candidate.name.EndsWith(onTargetsPostfix))
                {
                    _targetsOn.Add(candidate);
                }
            }
        }
    
        public override void UpdateBool(bool value)
        {
            if (!enabled) return;
            foreach (var obj in _targetsOn)
            {
                if (obj)
                {
                    obj.localScale = value ?  Vector3.one : Vector3.zero;
                }
            }

            foreach (var obj in _targetsOff)
            {
                if (obj)
                {
                    obj.localScale = !value ?  Vector3.one : Vector3.zero;
                }
            }
        }

    
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public override void ApplyBoolValue(bool value)
        {
            UpdateBool(value);
            this.MarkDirty();
        }
    
#endif
    }
}
