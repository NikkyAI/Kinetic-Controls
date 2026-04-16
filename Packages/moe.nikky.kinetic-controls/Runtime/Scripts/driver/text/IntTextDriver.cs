using System;
using nikkyai.common;
using nikkyai.Kinetic_Controls;
using nikkyai.Utils;
using TMPro;
using UnityEngine;
using VRC;

namespace nikkyai.driver.text
{
    public class IntTextDriver : IntDriver
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
        private String valueDisplayFormat = "00";

        protected override string LogPrefix => $"IntTextDriver {name}";

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

        public override void OnUpdateInt(int value)
        {
            if(textMeshPro) {
                textMeshPro.text = value.ToString(valueDisplayFormat);
            }
        }
        
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        // protected override int ValidationHash => HashCode.Combine(base.GetHashCode(), valueDisplayFormat, cachedValue);
        //
        // public override void OnValidateApplyValues()
        // {
        //     if (Application.isPlaying) return;
        //     base.OnValidateApplyValues();
        //
        //     if(cachedValue == int.MinValue) return;
        //     
        //     OnUpdateInt(cachedValue);
        //     if (textMeshPro)
        //     {
        //         textMeshPro.MarkDirty();
        //     }
        // }

        protected override void OnValidate()
        {
            if(!Application.isPlaying) return;
            base.OnValidate();

            if (cachedValue == int.MinValue) return;
            
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
                OnUpdateInt(cachedValue);
            }
        }

        
        // [ContextMenu("Update UI")]
        // protected override void OnValidate()
        // {
        //     base.OnValidate();
        //     if (Application.isPlaying) return;
        //     UnityEditor.EditorUtility.SetDirty(this);
        //
        //     if (valueDisplayFormat != prevFormat && !float.IsNaN(cachedValue))
        //     {
        //         
        //         prevFormat = valueDisplayFormat;
        //     }
        // }
        //
        // public override void ApplyIntValue(int value)
        // {
        //     OnUpdateInt(value);
        //     cachedValue = value;
        //     if (textMeshPro)
        //     {
        //         textMeshPro.MarkDirty();
        //     }
        // }
#endif
    }
}