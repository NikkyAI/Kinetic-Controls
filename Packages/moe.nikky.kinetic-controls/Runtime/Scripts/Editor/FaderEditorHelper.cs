using System;
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
using VRC.Udon;
using Object = UnityEngine.Object;

namespace nikkyai.Editor
{
    [ExecuteAlways]
    [RequireComponent(typeof(Fader))]
    public class FaderEditorHelper : MonoBehaviour, IEditorOnly, IPreprocessCallbackBehaviour
    {
        public Fader fader;

        [Header("Fader Settings")] [SerializeField]
        private Axis axis = Axis.Y;

        [Header("Fader Settings - Default Value")] [SerializeField]
        public Vector2 outputRange = new Vector2(0, 1);

        [SerializeField] [Range(0, 1)] public float defaultValueNormalized = 0.25f;

        [SerializeField] public float defaultValue = 0;

        [Header("Fader Settings - Smoothing")]
        [Tooltip(
             "smoothes out value updates over time, may impact CPU frametimes AND cause more updates to FloatDrivers"),
         SerializeField]
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

        [Header("Network Syncing")]
        [SerializeField]
        [Tooltip(
            "whether network sync is enabled or not, this can be 'animated' at runtime to stage changes and apply them when syncing is enabled again")]
        public bool synced = true;

        [Header("VR Support")] [Tooltip("switches between finger contacts and pickup")]
        public bool useContactsInVR = true;

        [Header("Desktop Support")]
        [SerializeField]
        [Tooltip("this needs to be a thin, flat box collider, " +
                 "it will be used for getting where the center of the screen is facing in desktop mode, " +
                 "if it is too thick then facing it even from a slight angle will provide the wrong values, " +
                 "leading to the the controls 'jumping' upon pickup in desktop mode")]
        public Collider desktopRaycastCollider;


        [Header("MIDI")] //
        [SerializeField, Tooltip("Sets up the required VRC MIDI Listener")]
        public bool addMidiListenerComponent = false;

        [SerializeField, Tooltip("Requires a VRC MIDI Listened with CC enabled")]
        public bool midiEnabled = true;

        [SerializeField, Range(0, 15)] public int midiChannel = 0;
        [SerializeField, Range(0, 127)] public int midiNumber = 0;
        [SerializeField, Range(0, 127)] public int midiInputRangeStart = 0;
        [SerializeField, Range(0, 127)] public int midiInputRangeEnd = 127;

        [Header("Components - Handle")] [SerializeField]
        public Handle handle;

        [SerializeField]
        [Tooltip("should be the same as targetIndicator or a child, " +
                 "handle will be reset to the given transform position / rotation on release" +
                 " (proxied to handle)")]
        public Transform handleReset;

        [Header("Components - Indicators")]
        [SerializeField] //
        [Tooltip("move this on the configured axis to the minumum fader range")]
        public Transform minLimitIndicator;

        [SerializeField] //
        [Tooltip("move this on the configured axis to the maximum fader range")]
        public Transform maxLimitIndicator;

        [SerializeField] //
        [Tooltip("will be moved to follow the handle (target value)")]
        public Transform targetIndicator;

        [SerializeField] //
        [Tooltip("will be moved to follow the smoothed value")]
        public Transform valueIndicator;

        [Header("Drivers")]
        [SerializeField] //
        [Tooltip("object containing FloatDrivers, will update to the current target/preview value")]
        public GameObject floatTargetValueDrivers;


        [SerializeField] //
        [Tooltip("object containing FloatDrivers, will update to the current smoothed value")]
        public GameObject floatSmoothedValueDrivers;

        [SerializeField] //
        [Tooltip("object containing BoolDrivers, will update to the current auth status")]
        public GameObject boolAuthorizedDrivers;

        // [Header("Drivers - Readonly")] [ReadOnly]
        // public FloatDriver[] targetValueDrivers;
        //
        // [ReadOnly] public FloatDriver[] smoothedValueDrivers;
        // [ReadOnly] public BoolDriver[] authDrivers;

        [Header("Access Control")] //  
        [SerializeField]
        public bool enforceACL = true;

        [Tooltip("ACL used to check who can use the toggle")] //
        [SerializeField]
        public AccessControl accessControl;

        [Header("Debug")] //
        [SerializeField]
        [Tooltip("enabled and moves this transform to where the raytrace collision happens, desktop only")]
        internal Transform debugDesktopRaytrace;

        [SerializeField] public DebugLog debugLog;

        private bool initialized = false;
        private float outputRangeMin => outputRange.x;
        private float outputRangeMax => outputRange.y;

        private float prevDefaultNormalized, prevDefault, prevMin, prevMax;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        private Fader GetFader()
        {
            if (Utilities.IsValid(fader)) return fader;
            Debug.Log("getting fader component", this);
            fader = gameObject.GetComponent<Fader>();
            return fader;
        }

        [ContextMenu("ApplyValues")]
        private void ApplyValues()
        {
            ApplyValues(onValidate: false);
        }

        private void ApplyValues(bool onValidate = false)
        {
            if (!initialized)
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
                defaultValue = Mathf.Lerp(outputRangeMin, outputRangeMax, defaultValueNormalized);
                prevDefault = defaultValue;
                prevMin = outputRangeMin;
                prevMax = outputRangeMax;
            }

            if (!Mathf.Approximately(prevDefault, defaultValue))
            {
                defaultValueNormalized = Mathf.InverseLerp(outputRangeMin, outputRangeMax, defaultValue);
                prevDefaultNormalized = defaultValueNormalized;
                prevMin = outputRangeMin;
                prevMax = outputRangeMax;
            }

            if (!Mathf.Approximately(prevMin, outputRangeMin) || !Mathf.Approximately(prevMax, outputRangeMax))
            {
                defaultValueNormalized = Mathf.InverseLerp(outputRangeMin, outputRangeMax, defaultValue);
                prevDefaultNormalized = defaultValueNormalized;
                prevMin = outputRangeMin;
                prevMax = outputRangeMax;
            }

            prevDefaultNormalized = defaultValueNormalized;
            prevDefault = defaultValue;

            f.outputRange = outputRange;
            f.defaultValueNormalized = defaultValueNormalized;
            f.defaultValue = defaultValue;

            f.enableValueSmoothing = enableValueSmoothing;
            f.smoothingUpdateInterval = smoothingUpdateInterval;
            f.smoothingTime = smoothingTime;
            f.smoothingMaxSpeed = smoothingMaxSpeed;

            f.synced = synced;

            f.desktopRaycastCollider = desktopRaycastCollider;

            SetupMidiListener(onValidate: onValidate);
            f.midiEnabled = midiEnabled;
            f.midiChannel = midiChannel;
            f.midiNumber = midiNumber;
            f.midiInputRangeStart = midiInputRangeStart;
            f.midiInputRangeEnd = midiInputRangeEnd;

            f.minLimitIndicator = minLimitIndicator;
            f.maxLimitIndicator = maxLimitIndicator;
            f.targetIndicator = targetIndicator;
            f.valueIndicator = valueIndicator;
            f._EnsureInit();
            f.UpdateIndicatorsInEditor();

            f.handle = handle;
            // f.SetupHandle();
            if (Utilities.IsValid(handle))
            {
                handle.resetTransform = handleReset;
                handle.UseContactsInVR = useContactsInVR;
                handle.EditorACL = accessControl;
                handle.EditorEnforceACL = enforceACL;
                handle.EditorDebugLog = debugLog;

                handle._EnsureInit();
                handle.Register(f);
                handle.SetupPickup();
                handle.SetupPickupRigidbody();
                handle.ResetTransform();
                handle.MarkDirty();
            }
            else
            {
                Debug.LogError($"missing handle in {name}", this);
            }

            f.floatTargetValueDrivers = floatTargetValueDrivers;
            f.floatSmoothedValueDrivers = floatSmoothedValueDrivers;
            f.EditorBoolAuthorizedDrivers = boolAuthorizedDrivers;

            f.EditorEnforceACL = enforceACL;
            f.EditorACL = accessControl;

            f.debugDesktopRaytrace = debugDesktopRaytrace;
            f.EditorDebugLog = debugLog;

            var minValue = Mathf.Min(f.MinValue, f.MaxValue);
            var maxValue = Mathf.Max(f.MinValue, f.MaxValue);

            f.FindDrivers();
            // targetValueDrivers = f._targetValueFloatDrivers;
            // smoothedValueDrivers = f._smoothedValueFloatDrivers;
            f.FindBoolAuthDrivers();
            // authDrivers = f.IsAuthorizedBoolDrivers;

            foreach (var valueFloatDriver in f._smoothedValueFloatDrivers)
            {
                valueFloatDriver.ApplyFloatValue(
                    Math.Clamp(defaultValue, minValue, maxValue)
                );
            }

            foreach (var targetFloatDriver in f._targetValueFloatDrivers)
            {
                targetFloatDriver.ApplyFloatValue(
                    Math.Clamp(defaultValue, minValue, maxValue)
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
            if (!initialized)
            {
                CopyFromFader();
            }

            if (
                initialized && ValidationCache.ShouldRunValidation(
                    this,
                    HashCode.Combine(
                        HashCode.Combine(
                            outputRange,
                            defaultValueNormalized,
                            defaultValue
                        ),
                        HashCode.Combine(
                            enableValueSmoothing,
                            smoothingUpdateInterval,
                            smoothingTime,
                            smoothingMaxSpeed
                        ),
                        HashCode.Combine(
                            synced,
                            addMidiListenerComponent,
                            midiEnabled,
                            midiChannel,
                            midiNumber,
                            midiInputRangeStart,
                            midiInputRangeEnd
                        ),
                        HashCode.Combine(
                            handle,
                            handleReset,
                            minLimitIndicator,
                            maxLimitIndicator,
                            targetIndicator,
                            valueIndicator),
                        HashCode.Combine(
                            floatTargetValueDrivers,
                            floatSmoothedValueDrivers,
                            boolAuthorizedDrivers,
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
            fader = f;
            outputRange = f.outputRange;
            defaultValueNormalized = f.defaultValueNormalized;
            defaultValue = f.defaultValue;

            enableValueSmoothing = f.enableValueSmoothing;
            smoothingUpdateInterval = f.smoothingUpdateInterval;
            smoothingTime = f.smoothingTime;
            smoothingMaxSpeed = f.smoothingMaxSpeed;

            synced = f.synced;
            useContactsInVR = f.handle.useContactsInVR;
            desktopRaycastCollider = f.desktopRaycastCollider;

            midiEnabled = f.midiEnabled;
            midiChannel = f.midiChannel;
            midiNumber = f.midiNumber;
            midiInputRangeStart = f.midiInputRangeStart;
            midiInputRangeEnd = f.midiInputRangeEnd;

            handle = f.handle;
            handleReset = f.handle.resetTransform;
            handleReset = f.handleResetDeprecated;

            minLimitIndicator = f.minLimitIndicator;
            maxLimitIndicator = f.maxLimitIndicator;
            targetIndicator = f.targetIndicator;
            valueIndicator = f.valueIndicator;

            floatTargetValueDrivers = f.floatTargetValueDrivers;
            floatSmoothedValueDrivers = f.floatSmoothedValueDrivers;
            boolAuthorizedDrivers = f.EditorBoolAuthorizedDrivers;

            enforceACL = f.EditorEnforceACL;
            accessControl = f.EditorACL;

            debugDesktopRaytrace = f.debugDesktopRaytrace;
            debugLog = f.EditorDebugLog;

            initialized = true;
            this.MarkDirty();
        }

        private void Awake()
        {
            CopyFromFader();
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