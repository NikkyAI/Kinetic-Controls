using nikkyai.common;
using UnityEngine;
using UnityEngine.Serialization;

namespace nikkyai.driver.converter
{
    public enum ComparisonType
    {
        GreaterThan,
        LessThan,
        EqualTo,
    }
    public class FloatToBool : FloatDriver
    {
        void Start()
        {
            _EnsureInit();
        }

        public ComparisonType compareType = ComparisonType.GreaterThan;
        public float compareTo = 0.0f;
    
        [FormerlySerializedAs("boolDriverHolder")] //
        [SerializeField] private GameObject boolDrivers;

        protected override string LogPrefix => nameof(FloatToBool);

        private BoolDriver[] _boolDrivers = {};
        protected override void _Init()
        {
            base._Init();

            _boolDrivers = boolDrivers.GetComponentsInChildren<BoolDriver>();
        }
    
        private bool _state = false;
        private bool _initialized = false;
        protected override void OnUpdateFloat(float value)
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
                    _boolDrivers[i].OnUpdateBool(_state);
                }
            }
            _initialized = true;
        }
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        protected override bool UpdateInEditor => true;
        protected override void EditorUpdateFloatValue(float value)
        {
            _boolDrivers = boolDrivers.GetComponentsInChildren<BoolDriver>();
            base.EditorUpdateFloatValue(value);
        }
#endif
    }
}
