using nikkyai.common;
using UnityEngine;
using VRC;
using VRC.SDKBase;

namespace nikkyai.driver.gameobject
{
    public class BoolObjectActiveDriver : BoolDriver
    {
        [SerializeField] public GameObject[] targetsOff = { };
        [SerializeField] public GameObject[] targetsOn = { };
        protected override string LogPrefix => $"{nameof(BoolObjectActiveDriver)} {name}";

        private void Start()
        {
            _EnsureInit();
        }

        public override void OnUpdateBool(bool value)
        {
            if (!enabled) return;
            Log($"switching state to {value}");
            if (Utilities.IsValid(targetsOn))
            {
                for (var i = 0; i < targetsOn.Length; i++)
                {
                    var obj = targetsOn[i];
                    if (Utilities.IsValid(obj))
                    {
                        obj.SetActive(value);
                    }
                }
            }

            if (Utilities.IsValid(targetsOff))
            {
                for (var j = 0; j < targetsOff.Length; j++)
                {
                    var obj = targetsOff[j];
                    if (Utilities.IsValid(obj))
                    {
                        obj.SetActive(!value);
                    }
                }
            }
        }
        
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public override void ApplyBoolValue(bool value)
        {
            base.ApplyBoolValue(value);
            if (!enabled) return;
            
            OnUpdateBool(value);
            
            this.MarkDirty();
            foreach (var obj in targetsOn)
            {
                if (Utilities.IsValid(obj))
                {
                    obj.MarkDirty();
                }
            }

            foreach (var obj in targetsOff)
            {
                if (Utilities.IsValid(obj))
                {
                    obj.MarkDirty();
                }
            }
        }
#endif
    }
}