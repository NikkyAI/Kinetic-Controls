using nikkyai.attribute;
using nikkyai.common;
using nikkyai.Editor;
using nikkyai.extensions;
using UnityEngine;
using UnityEngine.Serialization;
using VRC;
using VRC.SDKBase;

namespace nikkyai.driver.transform
{
#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [RequireComponent(typeof(PreProcessEditorHelper))]
#endif
    public class BoolScaleByPostfixDriver : BoolDriver
    {
        // [SerializeField] private Transform[] targetsOn = { };
        // [SerializeField] private Transform[] targetsOff = { };

        [SerializeField] private Transform findInChildren;
        [SerializeField] private string offTargetsPostfix = "S1";
        [SerializeField] private string onTargetsPostfix = "S2";
        // [SerializeField] private bool disableOtherChildren = true;

        [SerializeField] [ReadOnly] private Transform[] targetsOn = { };
        [SerializeField] [ReadOnly] private Transform[] targetsOff = { };

        protected override string LogPrefix => nameof(BoolScaleByPostfixDriver);

        private void Start()
        {
            _EnsureInit();
        }

        // protected override void _Init()
        // {
        //     base._Init();
        // }

        public override void OnUpdateBool(bool value)
        {
            if (!enabled) return;
            foreach (var obj in targetsOn)
            {
                if (Utilities.IsValid(obj))
                {
                    Log($"update scale of {obj} to {value}");
                    obj.localScale = value ? Vector3.one : Vector3.zero;
                }
            }

            foreach (var obj in targetsOff)
            {
                if (Utilities.IsValid(obj))
                {
                    Log($"update scale of {obj} to {!value}");
                    obj.localScale = !value ? Vector3.one : Vector3.zero;
                }
            }
        }


#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public override void ApplyBoolValue(bool value)
        {
            OnUpdateBool(value);
            this.MarkDirty();
        }

        public override bool OnPreprocess()
        {
            if (!Utilities.IsValid(findInChildren))
            {
                findInChildren = transform;
            }

            // _targetsOff = new Transform[0] ;
            // _targetsOn = new Transform[0] ;
            for (int i = 0; i < findInChildren.childCount; i++)
            {
                var c = findInChildren.GetChild(i);
                string objectName = c.gameObject.name;
                // Log($"found '{objectName}', comparing against {offTargetsPostfix} and {onTargetsPostfix}");
                if (objectName.EndsWith(offTargetsPostfix))
                {
                    // Log($"found off transform: {objectName}");
                    targetsOff = targetsOff.Add(c);
                }
                else if (objectName.EndsWith(onTargetsPostfix))
                {
                    // Log($"found on transform: {objectName}");
                    targetsOn = targetsOn.Add(c);
                }
            }

            return true;
        }
#endif
    }
}