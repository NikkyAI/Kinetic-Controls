using UnityEngine;
using VRC;

namespace nikkyai.driver.blendshape
{
    public class FloatBlendshapeDriver : FloatDriver
    {
        [SerializeField]
        private SkinnedMeshRenderer targetRenderer;
        
        [SerializeField]
        private string[] blendshapes;

        protected override string LogPrefix => nameof(FloatBlendshapeDriver);

        private int[] blendhshapeIndices = { };
        
        void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            base._Init();
            if (!targetRenderer)
            {
                LogError("missing target renderer");
                return;
            }
            
            blendhshapeIndices = new int[blendshapes.Length];
            for (var i = 0; i < blendshapes.Length; i++)
            {
                blendhshapeIndices[i] = targetRenderer.sharedMesh.GetBlendShapeIndex(blendshapes[i]);
            }
        }

        public override void UpdateFloat(float value)
        {
            if (!enabled) return;
            // if (value <= 0f)
            // {
            //     LogError("value must be greater than 0");
            //     return;
            // }

            foreach (var blendhshapeIndex in blendhshapeIndices)
            {
                targetRenderer.SetBlendShapeWeight(blendhshapeIndex, value);
            }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
            targetRenderer.MarkDirty();
#endif
        }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public override void ApplyFloatValue(float value)
        {
            UpdateFloat(value);
        }
#endif
    }
}
