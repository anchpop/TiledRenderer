How to use:

1. Make a map in Tiled. (TODO insert link here). When using collisions, create them with the new Tiled Collision Editor, which supports complex polygons with holes.

2. Ensure your tilesheets are placed somewhere within the Assets/Resources folder so you know they will be included in the final build

3. Mark your tilesheets' Texture Type as Sprite in the import settings. Make sure the sprite mode is set to Multiple. Then open the Sprite Editor and slice up your sheets. If you have spritesheets of different sizes (which isn't recommended), you may need to adjust the pivot point. Set the Filter Mode to Point (No Filter) as well.

4. Add the tilemap script to the Gameobject whose parent you want the tile objects to be!

Tips:
To ignore a layer or tile, add the custom property of "ignore" and set it to "true". 

checklist (order of importance)
prefab creation
check-what-tile-im-in support
android/webplayer/uwp integration ✓
empty tile in tileset support
flipx ✓, flipy ✓, flipdiagonal support ✓
collision support ✓
Quickupdate
Editor integration
