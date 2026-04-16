using UdonSharp;
using UnityEngine;

namespace nikkyai.common
{
    public class BaseBehaviour : UdonSharpBehaviour
    {
        bool init = false;
        bool initDone = false;
        
        // [System.NonSerialized]
        // public System.Diagnostics.Stopwatch stopwatch;
        
        public void _EnsureInit()
        {
            if (init)
                return;

            init = true;

            // stopwatch = new System.Diagnostics.Stopwatch();
            // stopwatch.Start();
            
            _PreInit();
            _Init();
            
            // stopwatch.Stop();
            // LogWarning("Initialization time: " + stopwatch.ElapsedMilliseconds + "ms");
            
            initDone = true;
            
        }

        protected virtual void _PreInit() { }
        protected virtual void _Init() { }
        
        public bool Initialized
        {
            get { return initDone; }
        }

        protected virtual void LogError(string message)
        {
        }
        protected virtual void LogWarning(string message)
        {
        }
        protected virtual void Log(string message)
        {
        }
        protected virtual void LogAssert(string message)
        {
        }


        // private int lastValidationHash = 0;
#if UNITY_EDITOR && !COMPILER_UDONSHARP

        // protected virtual int ValidationHash => 0;
        //
        protected virtual void OnValidate()
        {
            if (Application.isPlaying) return;
        }
        //     
        //
        //     int hash = ValidationHash;
        //
        //     if (
        //         lastValidationHash != hash
        //     )
        //     {
        //         UnityEditor.EditorUtility.SetDirty(this);
        //         OnValidateApplyValues();
        //
        //         lastValidationHash = hash;
        //     }
        //
        //     // if (prevAccessControl != AccessControl
        //     //     || prevEnforceACL != EnforceACL
        //     //     || prevDebugLog != DebugLog
        //     //    )
        //     // {
        //     //     ApplyACLsAndLog();
        //     //     prevAccessControl = AccessControl;
        //     //     prevDebugLog = DebugLog;
        //     //     prevEnforceACL = EnforceACL;
        //     // }
        // }
        //
        //
        // public virtual void OnValidateApplyValues()
        // {
        //
        // }
#endif
    }
}