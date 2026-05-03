using System;
using nikkyai.common;
using nikkyai.Utils;
using TMPro;
using UnityEngine;
using VRC;

namespace nikkyai.driver.text
{
    public class FloatTextDriver : FloatDriver
    {
        [Header("TextMeshPro")] // header
        [SerializeField]
        private TextMeshPro textMeshPro;

        [Tooltip(
            "What the slider value will be formated as.\n" +
            "- 0.0 means it will always at least show one digit with one decimal point\n" +
            "- 00 means it will fill always be formated as two digits with no decimal point\n" +
            "- P0 will format it as a percentage, number is the amount of decimals to show")]
        [SerializeField]
        private String valueDisplayFormat = "0.0";

        protected override string LogPrefix => nameof(FloatTextDriver);

        void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            base._Init();
            
            //TODO: check if all fields are valid
            // or find the TMP component
        }

        protected override void OnUpdateFloat(float value)
        {
            if(textMeshPro) {
                textMeshPro.text = value.ToString(valueDisplayFormat);
            }
        }
        
        protected float cachedValue = float.NaN;
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        
        protected override void OnValidate()
        {
            if(!Application.isPlaying) return;
            base.OnValidate();

            if (float.IsNaN(cachedValue)) return;
            
            if (
                ValidationCache.ShouldRunValidation(
                  this,
                    HashCode.Combine(
                        valueDisplayFormat,
                        cachedValue
                    )
                )
            )
            {
                UpdateFloatRescale(cachedValue);
            }
        }
        
        protected override bool UpdateInEditor => true;
        protected override void PostEditorUpdate(float value)
        {
            if (textMeshPro)
            {
                textMeshPro.MarkDirty();
            }
        }

        // protected override void EditorUpdateFloatValue(float value)
        // {
        //     // base.EditorUpdateFloatValue(value);
        //     _EnsureInit();
        //     cachedValue = value;
        //     OnUpdateFloat(value);
        //     if (textMeshPro)
        //     {
        //         textMeshPro.MarkDirty();
        //     }
        // }
#endif
    }
}