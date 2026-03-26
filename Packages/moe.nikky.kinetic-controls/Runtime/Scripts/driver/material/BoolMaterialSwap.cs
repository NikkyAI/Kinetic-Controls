using nikkyai.common;
using UnityEngine;
using VRC.SDKBase;

namespace nikkyai.driver.material
{
    public class BoolMaterialSwap : BoolDriver
    {
        [SerializeField] private Material disabledMat;
        [SerializeField] private Material enabledMat;
        [SerializeField] private Renderer meshRenderer;
        [SerializeField] private int materialSlot = 0;
        
        protected override string LogPrefix => nameof(BoolMaterialSwap);

        void Start()
        {
            _EnsureInit();
        }

        public override void UpdateBool(bool value)
        {
            if (!enabled) return;
            if (!Utilities.IsValid(meshRenderer))
            {
                return;
            }
            if (value)
            {
                meshRenderer.materials[materialSlot] = enabledMat;
            }
            else
            {
                meshRenderer.materials[materialSlot] = disabledMat;
            }
        }
    }
}
