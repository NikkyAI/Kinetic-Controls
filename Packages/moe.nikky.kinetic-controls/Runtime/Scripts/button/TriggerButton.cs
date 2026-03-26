using nikkyai.ArrayExtensions;
using nikkyai.common;
using nikkyai.driver;
using Texel;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC;
using VRC.SDKBase;

namespace nikkyai
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TriggerButton : ACLBase
    {
        [FormerlySerializedAs("triggerHolder")]
        [Header("Drivers")] // header
        [SerializeField] private Transform triggerDrivers;
        [FormerlySerializedAs("isAuthorizedIndicator")] //
        [SerializeField] private Transform boolAuthorizedDrivers;

        protected override string LogPrefix => $"{nameof(TriggerButton)} {name}";
    
        private TriggerDriver[] _triggerDrivers = { };
        private BoolDriver[] _boolAuthorizedDrivers = { };

        void Start()
        {
            _EnsureInit();   
        }

        protected override void _Init()
        {
            if (triggerDrivers == null)
            {
                triggerDrivers = this.transform;
            }

            _triggerDrivers = gameObject.GetComponents<TriggerDriver>();
            if (Utilities.IsValid(triggerDrivers))
            {
                _triggerDrivers = _triggerDrivers.AddRange(
                        triggerDrivers.GetComponentsInChildren<TriggerDriver>()
                );
            }
            _triggerDrivers = triggerDrivers.GetComponentsInChildren<TriggerDriver>();
            Log($"Found {_triggerDrivers.Length} trigger drivers");
            if (boolAuthorizedDrivers)
            {
                _boolAuthorizedDrivers = boolAuthorizedDrivers.GetComponentsInChildren<BoolDriver>();
                Log($"Found {_boolAuthorizedDrivers.Length} isAuthorized bool drivers");
            }
        }

        protected override void AccessChanged()
        {
            DisableInteractive = !isAuthorized;
        
            for (var i = 0; i < _boolAuthorizedDrivers.Length; i++)
            {
                _boolAuthorizedDrivers[i].UpdateBool(isAuthorized);
            }
        }

        public override void Interact()
        {
            if (!isAuthorized) return;
            Log("Trigger executing");
            for (var i = 0; i < _triggerDrivers.Length; i++)
            {
                _triggerDrivers[i].Trigger();
            }
        }
        
        
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        [ContextMenu("Assign Defaults")]
        public void AssignDefaults()
        {
            if (Application.isPlaying) return;
            // UnityEditor.EditorUtility.SetDirty(this);

            var candidates = transform.GetComponentsInChildren<Transform>();
            if (triggerDrivers == null)
            {
                foreach (var candidate in candidates)
                {
                    if (candidate.name == "Trigger Drivers")
                    {
                        triggerDrivers = candidate;
                        Log("Found and assigned Trigger Drivers");
                        UnityEditor.EditorUtility.SetDirty(this);
                        break;
                    }
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);

            this.MarkDirty();
        }
#endif
    }
}
