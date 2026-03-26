# Kinetic Controls

## Install

VCC: https://nikkyai.github.io/vpm  
using https://vrc-get.anatawa12.com/en/alcom/ is highly recommended

## Basics

Integrates with AccessControls and logs from AccessTXL / CommonTXL
(with soba we might even be able to handle other auth provider implementations in the future) 

## Value drivers

Control surfaces work with several forms of value drivers, simple abstract classes that can be implemented as udonbehaviours

the control surfaces look for them inside specified objects (transforms in the hierarchy)

Faders, Levers and such use `FloatDriver`  
Selectors use `IntDriver`  
Toggles use `BoolDriver`  
Buttons use `TriggerDriver`  

### authorization indicators

every component that can be configured to use ACL will alos have the ability to update `BoolDriver` to indicate authorization state

this can help with making a component visually distinct when it is disabled due to lacking permissions

### Value smoothing and "target" values

for float based controls local value smoothing can be enabled
this means the target wil lbe synced but locally the current value gradually moves towards the target

this can help keeping intense visual changes gradual and avoid flashing or such things

there is a seperate set of float drivers to show the "target" value and the "actual" smoothhed value

this is useful for preview purposes


### builtin drivers

this list is incomplete and may grow, change or be refactored as needed

├───uncategorized  
│   BoolLoopRunning  
│   BoolObjectToggleDriver  
│   BoolScaleToggleByPostfixDriver (TODO: shorter name)  
│   BoolSyncedDriver  
│   BoolTransformScaleDriver  
│   FloatCyclingRate  
│   TriggerRandom  
│   TriggerResetToggleButton  
│  
├───animator  
│       AnimatorBoolDriver  
│       AnimatorFloatDriver  
│       AnimatorIntDriver  
│  
├───audiolink  
│       AudiolinkDriver  
│  
├───blendshape  
│       FloatBlendshapeDriver  
│  
├───converters  
│       FloatToBoolDriver  
│  
├───fader  
│       FloatSmoothingRateDriver  
│       IntSmoothingFramesDriver  
│       TriggerResetFader  
│  
├───material  
│       BoolMaterialSwap  
│       FloatMaterialPropertyDriver  
│       IntMaterialPropertyDriver  
│       MaterialIntToggleBoolDriver (to be renamed?)  
│  
├───text  
│       FloatTextDriver  
│       IntTextDriver  
│  
├───udon  
│       BoolUdonDriver  
│       FloatUdonDriver  
