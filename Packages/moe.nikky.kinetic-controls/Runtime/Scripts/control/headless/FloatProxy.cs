#define READONLY

using System;
using nikkyai.common;
using nikkyai.Editor;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace nikkyai.control.headless
{
#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [RequireComponent(typeof(PreProcessEditorHelper))]
#endif
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FloatProxy : LoggingSimple
    {
        [SerializeField]
#if READONLY
        [attribute.ReadOnly]
#endif
        [Obsolete]
        internal GameObject floatDriverSource;

        [SerializeField, attribute.ReadOnly, NonReorderable]
        public FloatDriver[] floatDrivers = Array.Empty<FloatDriver>();


        protected override string LogPrefix => nameof(FloatProxy);

        private void Start()
        {
            _EnsureInit();
        }

        //TODO: call from a proxy driver
        public void UpdateFloat(float value)
        {
            foreach (var floatDriver in floatDrivers)
            {
                floatDriver.UpdateFloatRescale(value);
            }
        }
        
        // protected override void OnUpdateFloat(float value)
        // {
        //     foreach (var floatDriver in floatDrivers)
        //     {
        //         floatDriver.UpdateFloatRescale(value);
        //     }
        // }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        internal void FindFloatDrivers()
        {
            if (Utilities.IsValid(floatDriverSource))
            {
                floatDrivers = floatDriverSource.GetComponentsInChildren<FloatDriver>();
            }
            else
            {
                LogError("missing object for float value drivers");
            }
        }

        public override bool OnPreprocess()
        {
            if (!base.OnPreprocess())
            {
                return false;
            }

            FindFloatDrivers();

            return true;
        }
        public void EditorUpdateFloatRescale(float value)
        {
            foreach (var floatDriver in floatDrivers)
            {
                floatDriver.EditorUpdateFloatRescale(value);
            }
        }
#endif
    }
}