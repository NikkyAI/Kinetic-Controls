using System;
using JetBrains.Annotations;
using nikkyai.common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace nikkyai.driver.converter
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ModernUISelectorToBool : LoggingSimple
    {
        [SerializeField] private Vector2Int selectedIdMatch = Vector2Int.up;
        [SerializeField] private GameObject boolDrivers;
        private BoolDriver[] _boolDrivers = {};
    
        protected override string LogPrefix => nameof(ModernUISelectorToBool);

        void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            base._Init();

            if (Utilities.IsValid(boolDrivers))
            {
                _boolDrivers = boolDrivers.GetComponentsInChildren<BoolDriver>();
                Log($"found {_boolDrivers.Length} bool drivers");
            }
        }
        // ReSharper disable once InconsistentNaming
        [HideInInspector, UsedImplicitly] public int selectionId;
        [UsedImplicitly]
        public void _SelectionChanged()
        {
            if (!enabled) return;
            if (!Initialized)
            {
                _EnsureInit();
            }
            Log($"Selection changed: {selectionId}");
            if (selectionId == selectedIdMatch.x)
            {
                // OnUpdateBool(false);
                for (var i = 0; i < _boolDrivers.Length; i++)
                {
                    var boolDriver = _boolDrivers[i];
                    if (Utilities.IsValid(boolDriver) && boolDriver.enabled)
                    {
                        boolDriver.OnUpdateBool(false);
                    }
                }
            } else if(selectionId == selectedIdMatch.y)
            {
                // OnUpdateBool(true);
                for (var i = 0; i < _boolDrivers.Length; i++)
                {
                    var boolDriver = _boolDrivers[i];
                    if (Utilities.IsValid(boolDriver) && boolDriver.enabled)
                    {
                        boolDriver.OnUpdateBool(true);
                    }
                }
            }
        }
    
    }
}
