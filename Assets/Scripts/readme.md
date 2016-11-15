How to use:

1. Make a map in Tiled. (TODO insert link here). When using collisions, create them with the new Tiled Collision Editor (accessable under View > Tile Collision Editor).

2. Place your .tmx files somewhere in the Assets/StreamingAssets folder. For example, "Assets/StreamingAssets/Tilemaps/mymap.tmx"

4. Add the ObjectPlacer script to a Gameobject, and drag-and-drop your tilesheets into the tilesheets list. Edit the path variable to the location relative to StreamingAssets. For example, "Tilemaps/mymap.tmx". Alternatively, you can give an absolute path (such as one that starts with a slash or backslash), but that is not reccomended. You can also

Tips:
To ignore a layer or tile, add the custom property "ignore" and set it to "true".
To make a collider into a trigger collider, add the custom property "isTrigger" and set it to "true"
If you want to give some of your tile gameobjects some extra components (or have a custom material, or have a certain tag, etc.), put those components in a prefab and then put the prefab in the "Prefabs" list in the tileRenderer. Then add the custom property "basePrefab" and set it to the name of your prefab to the tiles you want to be based off that prefab. If for whatever reason you don't want the sprite from the tilemap to be added to the Gameobject, simply set the "noSprite" property to "true" or add your own sprite to the base prefab (if you have one).s

Roadmap (order of importance)
✓ Android/webplayer/uwp integration (still need to test)  
✓ Flipx, flipy, flipdiagonal support  
✓ Polygon collision support    
✓ Box collision support    
✓ Trigger colliders  
Creating meshes of quads instead of gameobjects for certain layers
✓ Polyline collision support    
✓ Empty tile in tileset support  
✓ Prefab creation  
✓ Tile ignoring  
✓ Helper functions (get tile properties, get tile form world position, etc.)  
Quickupdate  
Editor integration  
Support for Hexagonal, Isometric, and Staggered
Render Order
