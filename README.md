# Kinetic Controls

## Install

VCC: https://nikkyai.github.io/vpm  
using https://vrc-get.anatawa12.com/en/alcom/ is highly recommended

## Basics

Integrates with AccessControls and logs from AccessTXL / CommonTXL
(with soba we might even be able to handle other auth provider implementations in the future) 

## Drivers

Controls work with several forms of drivers, simple abstract classes that can be implemented.

The control surfaces look for them inside specified game objects and their children

the types of drivers being available are

- `FloatDriver`
- `IntDriver`
- `BoolDriver`
- `VectorDriver`
- `ColorDriver`
- `TriggerDriver`

see which drivers are being used below

(the list below is a incomplete overview)

Faders, Levers and such use `FloatDriver` for the values  
Toggles use `BoolDriver`  
Selectors use `IntDriver` and `BoolDriver` in each selector item  
Buttons use `TriggerDriver`  

## Access Control and authorization indicators

Kinetic Controls implements Access control with `TXL Access`

every component that can be configured to use ACL will alos have the ability to update `BoolDriver` to indicate authorization state
this can help with making a component visually distinct when it is disabled due to lacking permissions

## Value smoothing: `target` and `smoothed` values

Applies to `Fader` and `Lever`

for float based controls local value smoothing can be enabled
this means the target value will be synced but locally the smoothed value gradually moves towards the target

this can help keeping intense visual changes gradual and avoid flashing or such things

there is a seperate set of float drivers to show the "target" value and the "smoothed" smoothhed value

this is useful for preview purposes

## Kinetic Controls VR input mode: Finger Contacts & Pickups

Moveable controls can be controlled via 
finger contacts while gripping your hand and sticking out the index finger
or pickups
this can be switched at runtime using a provided bool driver

## builtin drivers

This list is incomplete and will grow, change or be refactored as needed

Existing drivers are implemented for:

- Kinetic Controls Internals
  - Reset, Smoothing, Randomization and more
- Animator Parameters
- Material Properties
  - Float, Int, Vector and more 
- Property Blocks
  - everything that Materials can do and more while only applying to a single renderer
- UdonBehaviour Program Variables and Events
- GameObject SetActive
- Transforms
- Audiolink Thresholds and Gain
- Text (TextMeshPro)
  - format strings to display values
- Blendshapes
- Postprocessing Volumes

Please feel free to request more things

## ModernUI integration

there is integration to trigger drivers via ModernUI sliders and selectors

