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

        public override void OnUpdateBool(bool value)
        {
            if (!enabled) return;
            if (!Utilities.IsValid(meshRenderer))
            {
                LogWarning("meshRenderer is not valid");
                return;
            }

            //Material[] newMats = new Material[meshRenderer.sharedMaterials.Length];

            Material[] newMats = meshRenderer.sharedMaterials;
            
            if (value)
            {
                Log("setting material to enabled");
                newMats[materialSlot] = enabledMat;
                // meshRenderer.materials[materialSlot] = enabledMat;
            }
            else
            {
                Log("setting material to disabled");
                newMats[materialSlot] = disabledMat;
                // meshRenderer.materials[materialSlot] = disabledMat;
            }
            meshRenderer.sharedMaterials = newMats;
            return;
            
            for (int j = 0; j < newMats.Length; j++)
            {
                if (j == materialSlot)
                {
                    if (value)
                    {
                        Log("setting material to enabled");
                        newMats[j] = enabledMat;
                        // meshRenderer.materials[materialSlot] = enabledMat;
                    }
                    else
                    {
                        Log("setting material to disabled");
                        newMats[j] = disabledMat;
                        // meshRenderer.materials[materialSlot] = disabledMat;
                    }
                }
                else
                {
                    //newMats[j] = meshRenderer.sharedMaterials[j];
                }
            }

            meshRenderer.sharedMaterials = newMats;
        }
        
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public override void ApplyBoolValue(bool value)
        {
            base.ApplyBoolValue(value);
            OnUpdateBool(value);
        }
#endif
    }
}