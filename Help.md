
## Common issues and settings

### Check compatability

Vrchat has 3 SDKs they release: [Link](https://vrchat.com/home/download)
Make sure you update both VRC's SDK and your copy of the marker.
* SDK2
    * For 2.0 avatars.
    * Use an [SDK2 release](https://github.com/theepicsnail/Marker/releases)
* SDK3 - Avatars
    * For 3.0 avatars.
    * Use an [SDK3 release](https://github.com/theepicsnail/Marker/releases)
* SDK3 - Worlds
    * The marker is only for avatars. For worlds [this](https://booth.pm/en/items/1555789) is a common solution.

### Fixing errors

If something goes wrong, you can clean things up. 

If you did not use your own FX controller (or you clicked a "setup 3.0 defaults" button):

* You can just delete the stuff generated in /Snail/Marker3.0/Generated and try again.

If you are using your own FX controller
1. Open your FX controller and delete the layers Gesture<hand>Maker and ToggleMarker
2. Delete from your params list "ToggleMarker"
3. Delete from your menu "Toggle Marker"


### Upload checklist

1. Always make sure you avatar **DOES NOT** have a Animator Controller assigned in the default Animator component.
    *  This has a random chance of breaking things
2. Always check your layers and make sure the weight is 1.0
3. On the trail renderer:
    * Make sure Emitting is off
        * People forget to turn it back off after testing
        * Your marker will start out drawing by itself if you forgot
    * Make sure the time is set to 120 by default
        * Changing this incorrectly may make the pen not work


### Common configs

* If the speed for your marker is too slow
    * You can adjust this in the animation controller
    * Change the transition state exit time to something very low.

* If you want the color to match the Trail Renderer components Gradient Color
    * Set the materials shader to UI/Default

* If you want to control the marker with the opposite hand
    * When running the setup script select opposite hand

* There are other Materials available with color choices in /Assets/Snail/ExampleMaterials
