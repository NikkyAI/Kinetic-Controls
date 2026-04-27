using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

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

        protected virtual void OnValidate()
        {
            // if (Application.isPlaying) return;
            // _EnsureInit();
        }
#endif
    }
}