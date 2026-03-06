using System;
using UnityEngine;

namespace nikkyai.driver
{
    public enum ComparisonType
    {
        GreaterThan,
        LessThan,
        EqualTo,
    }
    public class FloatToBoolDriver : FloatDriver
    {
        void Start()
        {
            _EnsureInit();
        }

        public ComparisonType compareType = ComparisonType.GreaterThan;
        public float compareTo = 0.0f;
    
        [SerializeField] private Transform boolDriverHolder;

        protected override string LogPrefix => nameof(FloatToBoolDriver);

        private BoolDriver[] _boolDrivers = {};
        protected override void _Init()
        {
            base._Init();

            _boolDrivers = boolDriverHolder.GetComponentsInChildren<BoolDriver>();
        }
    
        private bool _state = false;
        private bool _initialized = false;
        public override void UpdateFloat(float value)
        {
            if (!enabled) return;
            var prevState = _state;
            switch (compareType)
            {
                case ComparisonType.GreaterThan:
                    _state = value > compareTo;
                    Log($"compare {value} > {compareTo} =  {_state}");
                    break;
                case ComparisonType.LessThan:
                    _state = value < compareTo;
                    Log($"compare {value} < {compareTo} =  {_state}");
                    break;
                case ComparisonType.EqualTo:
                    _state = Mathf.Approximately(value, compareTo);
                    Log($"compare {value} == {compareTo} =  {_state}");
                    break;
                default:
                    LogError($"compareType {compareType} not implemented");
                    break;
            }

            if (_state != prevState || !_initialized)
            {
                for (var i = 0; i < _boolDrivers.Length; i++)
                {
                    _boolDrivers[i].UpdateBool(_state);
                }
            }
            _initialized = true;
        }
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public override void ApplyFloatValue(float value)
        {
            _boolDrivers = boolDriverHolder.GetComponentsInChildren<BoolDriver>();
            UpdateFloat(value);
        }
#endif
    }
}
