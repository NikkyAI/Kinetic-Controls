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

        public virtual bool OnPreprocess()
        {
            return true;
        }

        // protected virtual void LogError(string message)
        // {
        // }
        // protected virtual void LogWarning(string message)
        // {
        // }
        // protected virtual void Log(string message)
        // {
        // }
        // protected virtual void LogAssert(string message)
        // {
        // }
        
        #region Network Sync
        
        public virtual bool Synced
        {
            get => false;
            set { }
        }
        
        #endregion

        #region local player and ownership
        
        private VRCPlayerApi _owner;
        private VRCPlayerApi _localPLayer;
        protected VRCPlayerApi LocalPlayer => _localPLayer;
        private bool _isInVR;
        protected bool IsInVR => _isInVR;
        private string _localName = "???";
        protected string LocalPlayerName => _localName;
        
        public virtual void TakeOwnership()
        {
            if (_owner != _localPLayer) // !Networking.IsOwner(gameObject)
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            base.OnPlayerJoined(player);
            if (player == Networking.LocalPlayer)
            {
                _localPLayer = player;
                _localName = player.displayName;
                _isInVR = player.IsUserInVR();
                // _isInVR = true; // fakes being in VR during testing
                
            }
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            base.OnOwnershipTransferred(player);
            _owner = player;
        }

        #endregion

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