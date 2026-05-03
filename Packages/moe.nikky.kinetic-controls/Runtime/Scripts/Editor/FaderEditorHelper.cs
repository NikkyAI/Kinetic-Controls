using System;
using nikkyai.attribute;
using nikkyai.common;
using nikkyai.control.kinetic;
using nikkyai.Kinetic_Controls;
using nikkyai.Utils;
using Texel;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using VRC;
using VRC.SDK3.Midi;
using VRC.SDKBase;
using VRC.SDKBase.Editor.Attributes;
using VRC.Udon;
using Object = UnityEngine.Object;

namespace nikkyai.Editor
{
    [ExecuteAlways]
    [RequireComponent(typeof(Fader))]
    public class FaderEditorHelper : MonoBehaviour, IEditorOnly, IPreprocessCallbackBehaviour
    {
        [Header("Hover over fields for details and setup instructions")]
        // [HelpBox("Hover over fields for details and setup instructions")]
        // [Space(1f)]
        [Header("Fader Settings")] //
        [SerializeField]
        private Axis axis = Axis.Y;

        [FormerlySerializedAs("synced")] //
        [SerializeField]
        [Tooltip(
            "whether network sync is enabled or not, this can be 'animated' at runtime to stage changes and apply them when syncing is enabled again")]
        public bool networkSynced = true;

        [Header("Value Remapping")] //
        // [SerializeField]
        // public bool useRemapRange = false;
        [FormerlySerializedAs("outputRange")]
        [SerializeField]
        public Vector2 remapTo = new Vector2(0, 1);

        [Header("Default Value")] //
        [SerializeField] //
        [Range(0, 1)]
        public float defaultValueNormalized = 0.25f;

        [FormerlySerializedAs("defaultValue")] //
        [SerializeField]
        public float defaultValueRemapped = 0;

        #region Handle

        [Header("Handle (required)")] //
        //[HelpBox("required for basic functionality")]
        [FormerlySerializedAs("handle")] //
        [SerializeField]
        public HandleAbstract handleReference;

        [FormerlySerializedAs("handleReset")]
        [SerializeField]
        [Tooltip("has to match the position of the handle mesh inside targetIndicator\n" +
                 "the handle pickup / contact receiver will be updated to this position")]
        public Transform handlePosition;

        [SerializeField] private Transform minPositionSource;

        [SerializeField] private Transform maxPositionSource;

        [FormerlySerializedAs("minPos")]
        [SerializeField] //
        [ReadOnly]
        private float minPosition = -1;

        [FormerlySerializedAs("maxPos")] //
        [SerializeField] //
        [ReadOnly]
        private float maxPosition = 1;

        [Header("Handle - Desktop Support (required)")]
        [Tooltip("required for desktop support - " +
                 "this needs to be a thin, flat box collider, " +
                 "it will be used for getting where the center of the screen is facing in desktop mode, " +
                 "if it is too thick then facing it even from a slight angle will provide the wrong values, " +
                 "leading to the the controls 'jumping' upon pickup in desktop mode")]
        [SerializeField]
        public Collider desktopRaycastCollider;

        #endregion


        #region Driver Sources

        [Header("Driver Sources (required)")] //
        //[HelpBox("object containing Driver Components, will be collected at build time")]
        [FormerlySerializedAs("floatTargetValueDrivers")] //
        [SerializeField] //
        [Tooltip("object containing FloatDrivers, will update to the current target/preview value")]
        public GameObject floatTargetDriverSource;

        [FormerlySerializedAs("floatSmoothedValueDrivers")] //
        [SerializeField] //
        [Tooltip("object containing FloatDrivers, will update to the current smoothed value")]
        public GameObject floatSmoothedDriverSource;

        [FormerlySerializedAs("boolAuthorizedDrivers")] //
        [SerializeField] //
        [Tooltip("object containing BoolDrivers, will update to the current auth status")]
        public GameObject boolAuthorizedDriverSource;

        #endregion

        // [Header("Drivers - Readonly")] [ReadOnly]
        // public FloatDriver[] targetValueDrivers;
        //
        // [ReadOnly] public FloatDriver[] smoothedValueDrivers;
        // [ReadOnly] public BoolDriver[] authDrivers;

        #region Indicators

        [Header("Indicators (optional)")]
        //[HelpBox("transforms that will be updated to indicate state as well as range limits")]
        [SerializeField] //
        [Tooltip("will be moved to follow the handle (target value)")]
        public Transform targetIndicator;

        [FormerlySerializedAs("valueIndicator")]
        [SerializeField] //
        [Tooltip("will be moved to follow the smoothed value")]
        public Transform smoothedIndicator;

        [SerializeField] //
        [Tooltip("this transform will move to indicate the lower bound of the fader handle movement range")]
        public Transform minLimitIndicator;

        [SerializeField] //
        [Tooltip("this transform will move to indicate the upper bound of the fader handle movement range")]
        public Transform maxLimitIndicator;

        #endregion


        #region Value Smoothing

        [Header("Value Smoothing")]
        //[HelpBox("smoothes out value updates over time, may impact CPU frame times AND cause more updates sent to FloatDrivers")]
        [Tooltip(
            "smoothes out value updates over time, may impact CPU frametimes AND cause more updates to FloatDrivers")]
        [SerializeField]
        public bool enableValueSmoothing = true;

        [Tooltip("amount of frames to skip when approaching target value," +
                 "higher number == less load, but more choppy smoothing"),
         SerializeField, Range(1, 10)]
        public int smoothingUpdateInterval = 3;

        [Tooltip("higher values -> faster synchronization with the target maxSpeed \n" +
                 "(see Unity Mathf.SmoothDamp smoothTime parameter)")]
        [SerializeField]
        [Range(0f, 2.5f)]
        public float smoothingTime = 0.1f;

        [Tooltip("Maximum speed that smoothing can move at (see Unity Mathf.SmoothDamp maxSpeed parameter)")]
        [SerializeField]
        [Range(0f, 1f)]
        public float smoothingMaxSpeed = 0.25f;

        #endregion

        #region Access Control

        [Header("Access Control")] //  
        //[HelpBox("You either need to assign AccessControl here or disable enforceACL, otherwise the Fader WILL NOT work")]
        [SerializeField]
        public bool enforceACL = true;

        [Tooltip("ACL used to check who can use the toggle")] //
        [SerializeField]
        public AccessControl accessControl;

        #endregion

        #region MIDI

        [Header("MIDI")] //
        [SerializeField, Tooltip("Sets up the required VRC MIDI Listener")]
        public bool addMidiListenerComponent = false;

        [SerializeField, Tooltip("Requires a VRC MIDI Listened with CC enabled")]
        public bool midiEnabled = true;

        [SerializeField, Range(0, 15)] public int midiChannel = 0;
        [SerializeField, Range(0, 127)] public int midiNumber = 0;
        [SerializeField, Range(0, 127)] public int midiInputRangeStart = 0;
        [SerializeField, Range(0, 127)] public int midiInputRangeEnd = 127;

        #endregion

        #region Debug

        [Header("Debug")] //
        [SerializeField]
        [Tooltip("enabled and moves this transform to where the raytrace collision happens, desktop only")]
        internal Transform debugDesktopRaytrace;

        [SerializeField] public DebugLog debugLog;

        #endregion

        private bool _initialized = false;
        private float outputRangeMin => remapTo.x;
        private float outputRangeMax => remapTo.y;

        private float prevDefaultNormalized = 0.25f;
        private float prevDefault = 0.25f;
        private float prevMin = 0;
        private float prevMax = 1;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        // private Fader GetFader()
        // {
        //     if (Utilities.IsValid(fader)) return fader;
        //     Debug.Log("getting fader component", this);
        //     fader = gameObject.GetComponent<Fader>();
        //     return fader;
        // }

        private Fader GetFader() => gameObject.GetComponent<Fader>();

        [ContextMenu("ApplyValues")]
        private void ApplyValues()
        {
            ApplyValues(onValidate: false);
        }

        private void ApplyValues(bool onValidate = false)
        {
            if (!_initialized)
            {
                Debug.LogWarning("not initalized yet");
                return;
            }

            Debug.Log("Applying values", this);

            var f = GetFader();
            if (!Utilities.IsValid(f))
            {
                Debug.LogError("fader was not assigned");
                return;
            }

            if (!Mathf.Approximately(prevDefaultNormalized, defaultValueNormalized))
            {
                defaultValueRemapped = Mathf.Lerp(outputRangeMin, outputRangeMax, defaultValueNormalized);
                prevDefault = defaultValueRemapped;
                prevMin = outputRangeMin;
                prevMax = outputRangeMax;
            }

            if (!Mathf.Approximately(prevDefault, defaultValueRemapped))
            {
                defaultValueNormalized = Mathf.InverseLerp(outputRangeMin, outputRangeMax, defaultValueRemapped);
                prevDefaultNormalized = defaultValueNormalized;
                prevMin = outputRangeMin;
                prevMax = outputRangeMax;
            }

            if (!Mathf.Approximately(prevMin, outputRangeMin) || !Mathf.Approximately(prevMax, outputRangeMax))
            {
                defaultValueNormalized = Mathf.InverseLerp(outputRangeMin, outputRangeMax, defaultValueRemapped);
                prevDefaultNormalized = defaultValueNormalized;
                prevMin = outputRangeMin;
                prevMax = outputRangeMax;
            }

            prevDefaultNormalized = defaultValueNormalized;
            prevDefault = defaultValueRemapped;

            f.axis = axis;
            f.outputRange = new Vector2(outputRangeMin, outputRangeMax);
            f.defaultValueNormalized = defaultValueNormalized;
            f.defaultValue = defaultValueRemapped;

            if (Utilities.IsValid(minPositionSource))
            {
                var minLocalPos = transform.InverseTransformPoint(minPositionSource.position);
                minPosition = minLocalPos[(int)axis];
            }

            if (Utilities.IsValid(maxPositionSource))
            {
                var maxLocalPos = transform.InverseTransformPoint(maxPositionSource.position);
                maxPosition = maxLocalPos[(int)axis];
            }

            f.minPos = minPosition;
            f.maxPos = maxPosition;

            f.enableValueSmoothing = enableValueSmoothing;
            f.smoothingUpdateInterval = smoothingUpdateInterval;
            f.smoothingTime = smoothingTime;
            f.smoothingMaxSpeed = smoothingMaxSpeed;

            f.synced = networkSynced;

            f.desktopRaycastCollider = desktopRaycastCollider;

            if (Utilities.IsValid(minLimitIndicator))
                f.minLimitIndicator = minLimitIndicator;
            if (Utilities.IsValid(maxLimitIndicator))
                f.maxLimitIndicator = maxLimitIndicator;
            if (Utilities.IsValid(targetIndicator))
                f.targetIndicator = targetIndicator;
            if (Utilities.IsValid(smoothedIndicator))
                f.valueIndicator = smoothedIndicator;
            f._EnsureInit();
            f.UpdateIndicatorsInEditor();

            // f.SetupHandle();
            if (Utilities.IsValid(handleReference))
            {
                f.handle = handleReference;
                // handle.resetTransform = targetIndicator;
                if (Utilities.IsValid(handlePosition))
                {
                    //TODO: validate that handlePosition is a child of targetIndicator

                    handleReference.resetTransform = handlePosition;
                }
                else
                {
                    handleReference.resetTransform = targetIndicator;
                }

                // handle.UseContactsInVR = useContactsInVR;
                handleReference.EditorACL = accessControl;
                handleReference.EditorEnforceACL = enforceACL;
                handleReference.EditorDebugLog = debugLog;
                handleReference.EditorBoolAuthorizedDrivers = boolAuthorizedDriverSource;

                handleReference.RegisterRuntime(f);
                handleReference.Setup();
                handleReference._EnsureInit();
                // handle.Register(f);
                // handle.SetupPickup();

                // handle.SetupPickupRigidbody();
                handleReference.ResetTransform();
                handleReference.MarkDirty();
            }
            else
            {
                Debug.LogError($"missing handle in {name}", this);
            }

            f.floatTargetValueDrivers = floatTargetDriverSource;
            f.floatSmoothedValueDrivers = floatSmoothedDriverSource;
            f.EditorBoolAuthorizedDrivers = boolAuthorizedDriverSource;

            f.EditorEnforceACL = enforceACL;
            f.EditorACL = accessControl;

            f.debugDesktopRaytrace = debugDesktopRaytrace;
            f.EditorDebugLog = debugLog;

            SetupMidiListener(onValidate: onValidate);
            f.midiEnabled = midiEnabled;
            f.midiChannel = midiChannel;
            f.midiNumber = midiNumber;
            f.midiInputRangeStart = midiInputRangeStart;
            f.midiInputRangeEnd = midiInputRangeEnd;

            var minValue = Mathf.Min(f.MinValue, f.MaxValue);
            var maxValue = Mathf.Max(f.MinValue, f.MaxValue);

            f.FindDrivers();
            // targetValueDrivers = f._targetValueFloatDrivers;
            // smoothedValueDrivers = f._smoothedValueFloatDrivers;
            f.FindBoolAuthDrivers();
            // authDrivers = f.IsAuthorizedBoolDrivers;

            foreach (var valueFloatDriver in f.smoothedValueFloatDrivers)
            {
                valueFloatDriver.EditorUpdateFloatRescale(
                    Math.Clamp(defaultValueRemapped, minValue, maxValue)
                );
            }

            foreach (var targetFloatDriver in f.targetValueFloatDrivers)
            {
                targetFloatDriver.EditorUpdateFloatRescale(
                    Math.Clamp(defaultValueRemapped, minValue, maxValue)
                );
            }

            f.MarkDirty();
        }

        protected void SetupMidiListener(bool onValidate = false)
        {
            if (addMidiListenerComponent)
            {
                var listener = GetComponent<VRCMidiListener>();
                if (listener == null)
                {
                    listener = gameObject.AddComponent<VRCMidiListener>();
                }

                if (listener != null)
                {
                    SerializedObject listenerSerialized = new SerializedObject(listener);
                    var behaviourProperty = listenerSerialized.FindProperty("behaviour");
                    var fader = GetFader();
                    if (Utilities.IsValid(fader))
                    {
                        behaviourProperty.objectReferenceValue = new SerializedObject(fader)
                            .FindProperty("_udonSharpBackingUdonBehaviour")
                            .objectReferenceValue;
                        listenerSerialized.ApplyModifiedProperties();
                    }
                }

                if (listener != null)
                {
                    listener.activeEvents = VRCMidiListener.MidiEvents.CC;
                    listener.enabled = true;
                }
            }
            else
            {
                var listeners = GetComponents<VRCMidiListener>();
                foreach (var vrcMidiListener in listeners)
                {
                    if (vrcMidiListener == null) continue;
                    // vrcMidiListener.enabled = false;
                    if (onValidate)
                    {
                        vrcMidiListener.enabled = false;
                        EditorApplication.delayCall += () => { Undo.DestroyObjectImmediate(vrcMidiListener); };
                    }
                    else if (Application.isEditor)
                    {
                        DestroyImmediate(vrcMidiListener);
                    }
                    else
                    {
                        Destroy(vrcMidiListener);
                    }
                }
            }
        }

        void OnValidate()
        {
            if (!_initialized)
            {
                CopyFromFader();
            }

            if (
                _initialized && ValidationCache.ShouldRunValidation(
                    this,
                    HashCode.Combine(
                        HashCode.Combine(
                            axis,
                            networkSynced,
                            remapTo,
                            defaultValueNormalized,
                            defaultValueRemapped
                        ),
                        HashCode.Combine(
                            minPositionSource,
                            maxPositionSource,
                            minPosition,
                            maxPosition
                        ),
                        HashCode.Combine(
                            enableValueSmoothing,
                            smoothingUpdateInterval,
                            smoothingTime,
                            smoothingMaxSpeed
                        ),
                        HashCode.Combine(
                            addMidiListenerComponent,
                            midiEnabled,
                            midiChannel,
                            midiNumber,
                            midiInputRangeStart,
                            midiInputRangeEnd
                        ),
                        HashCode.Combine(
                            handleReference,
                            handlePosition,
                            minLimitIndicator,
                            maxLimitIndicator,
                            targetIndicator,
                            smoothedIndicator),
                        HashCode.Combine(
                            floatTargetDriverSource,
                            floatSmoothedDriverSource,
                            boolAuthorizedDriverSource,
                            enforceACL,
                            accessControl,
                            debugLog
                        )
                    )
                )
            )
            {
                ApplyValues(onValidate: true);
            }
        }

        [ContextMenu("Setup Values from Fader")]
        internal void CopyFromFader()
        {
            var f = GetFader();
            // fader = f;
            axis = f.axis;
            // valueOutputMin = f.outputRange.x;
            // valueOutputMax = f.outputRange.y;
            remapTo = f.outputRange;

            defaultValueNormalized = f.defaultValueNormalized;
            defaultValueRemapped = f.defaultValue;

            if (!Utilities.IsValid(minPositionSource))
            {
                minPositionSource = f.minLimitIndicator;
            }

            if (!Utilities.IsValid(maxPositionSource))
            {
                maxPositionSource = f.maxLimitIndicator;
            }

            enableValueSmoothing = f.enableValueSmoothing;
            smoothingUpdateInterval = f.smoothingUpdateInterval;
            smoothingTime = f.smoothingTime;
            smoothingMaxSpeed = f.smoothingMaxSpeed;

            networkSynced = f.synced;
            // useContactsInVR = f.handle.useContactsInVR;
            desktopRaycastCollider = f.desktopRaycastCollider;

            handleReference = f.handle;
            if (Utilities.IsValid(handleReference))
            {
                handlePosition = handleReference.resetTransform;
            }

            minLimitIndicator = f.minLimitIndicator;
            maxLimitIndicator = f.maxLimitIndicator;
            targetIndicator = f.targetIndicator;
            smoothedIndicator = f.valueIndicator;

            floatTargetDriverSource = f.floatTargetValueDrivers;
            floatSmoothedDriverSource = f.floatSmoothedValueDrivers;
            boolAuthorizedDriverSource = f.EditorBoolAuthorizedDrivers;

            enforceACL = f.EditorEnforceACL;
            accessControl = f.EditorACL;

            debugDesktopRaytrace = f.debugDesktopRaytrace;
            debugLog = f.EditorDebugLog;

            midiEnabled = f.midiEnabled;
            midiChannel = f.midiChannel;
            midiNumber = f.midiNumber;
            midiInputRangeStart = f.midiInputRangeStart;
            midiInputRangeEnd = f.midiInputRangeEnd;

            _initialized = true;
            this.MarkDirty();
        }

        private void Awake()
        {
            // var f = GetFader();
            // if (Utilities.IsValid(f))
            // {
            //     fader = f;
            // }
            // initialized = true;
            if (!_initialized)
            {
                CopyFromFader();
            }

            ApplyValues(onValidate: true);
        }
#endif

        public bool OnPreprocess()
        {
            Debug.Log($"Preprocess: is editor: {Application.isEditor}");
#if UNITY_EDITOR && !COMPILER_UDONSHARP
            ApplyValues();
#else
            Debug.LogWarning("Preprocess: is not running");
#endif
            return true;
        }

        public int PreprocessOrder { get; }
    }
}