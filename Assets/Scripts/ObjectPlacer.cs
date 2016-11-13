using System.IO;
using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using TiledSharp;

public class ObjectPlacer : MonoBehaviour
{
    public string TmxPath;
    public bool createMapOnAwake = true;
    public float pixelsPerUnit = -1;
    public List<Texture2D> tileSheets = new List<Texture2D>();
    public List<GameObject> prefabs = new List<GameObject>();

    List<Sprite> spriteList = new List<Sprite> { null };
    Dictionary<int, TmxTilesetTile> tileIdMap = new Dictionary<int, TmxTilesetTile>();
    Dictionary<TmxLayerTile, GameObject> tileGameobjectMap = new Dictionary<TmxLayerTile, GameObject>();

    TmxMap map;

    void Start () {
        if (createMapOnAwake) createTilemap();
    }

    public void createTilemap(string path)
    {
        StartCoroutine(RenderMap(path));
    }

    public void createTilemap()
    {
        var url = Path.Combine(Application.streamingAssetsPath, TmxPath);

        #if UNITY_EDITOR || !UNITY_ANDROID // Android doesn't need "file://" added because Application.streamingAssetsPath comes with "file:"
           url = "file://" + url; 
        #endif

        StartCoroutine(RenderMap(url));
    }

    IEnumerator RenderMap(string path)
    {
        var tmx = new WWW(path);
        yield return tmx;
        map = new TmxMap(XDocument.Parse(tmx.text));

        if (map.Orientation != OrientationType.Orthogonal) throw new System.Exception("Modes other than Orthagonal are not supported at this time");  

        foreach (var tileSet in map.Tilesets) // we need to get the tilesheets, slice them up into sprites, and put them in spriteList
        {
            var sheets = tileSheets.Where(c => c.name == tileSet.Name).ToList();
            if (sheets.Count == 0) throw new System.Exception("Couldn't find tileset " + tileSet.Name + " in the tileSheets list");

            var slicer = new TileSlicer(sheets.First(), tileSet.TileWidth, tileSet.TileHeight, tileSet.Spacing, tileSet.Margin);
            spriteList.AddRange(slicer.sprites);

            if (pixelsPerUnit == -1) pixelsPerUnit = spriteList[spriteList.Count - 1].pixelsPerUnit; // if PixelsPerUnit hasn't been changed by the user, we just guess
            
            foreach (var tile in tileSet.Tiles.Values)
            {
                tileIdMap[tile.Id + 1] = tile; // tileset ids are always one lower than the tilemap Gid. yeah it's dumb
            }
        }


        for (int layerindex = 0; layerindex < map.Layers.Count; layerindex++)
        {
            var layer = map.Layers[layerindex];
            if (!(layer.Properties.ContainsKey("ignore") && layer.Properties["ignore"].ToLower() == "true")) // if they've opted to ignore the layer
            {
                var lparent = new GameObject(layer.Name); // object to parent the tiles created under
                lparent.transform.SetParent(transform);

                foreach (var tile in layer.Tiles)
                {
                    if (tile.Gid > 0 && !(tileIdMap.ContainsKey(tile.Gid) && tileIdMap[tile.Gid].Properties.ContainsKey("ignore") && tileIdMap[tile.Gid].Properties["ignore"].ToLower() == "true"))
                    {
                        var tex = spriteList[tile.Gid];
                        var destx = tile.X * (map.TileWidth / pixelsPerUnit);
                        var desty = -tile.Y * (map.TileWidth / pixelsPerUnit);
                        var go = addTileObject(tile, tex, destx, desty, layerindex);
                        if (go) go.transform.SetParent(lparent.transform);
                    }
                }
            }
        }
    }

    GameObject addTileObject(TmxLayerTile tile, Sprite tex, float x, float y, int orderInLayer = 0)
    {
        if (tile.Gid > 0)
        {
            // determine whether we should flip horisontally, flip vertically, or rotate 90 degrees
            bool flipX = tile.HorizontalFlip;
            bool flipY = tile.VerticalFlip;
            bool rotate90 = tile.DiagonalFlip;
            if (rotate90)
            {
                Swap(ref flipX, ref flipY);
                flipX = !flipX;
            }
            
            var go = getBaseGameobject(tile);
            go.transform.position = new Vector3(x, y, 0);
            go.transform.Rotate(new Vector3(0, 0, rotate90 ? 90 : 0));

            var sprend = go.GetComponent<SpriteRenderer>();
            sprend.sprite = tex;
            sprend.flipX = flipX;
            sprend.flipY = flipY;
            sprend.sortingOrder = orderInLayer;

            if (tileIdMap.ContainsKey(tile.Gid)) // if we have special information about that tile (such as collision information)
                foreach (TmxObjectGroup oGroup in tileIdMap[tile.Gid].ObjectGroups)
                {
                    foreach (TmxObject collisionObject in oGroup.Objects)
                    {
                        if (collisionObject.ObjectType == TmxObjectType.Basic) // add a box collider, square object
                        {
                            var boxCol = go.AddComponent<BoxCollider2D>();

                            // Don't forget we have to convert from pixels to unity units!
                            float width  = (float)collisionObject.Width / tex.pixelsPerUnit;
                            float height = (float)collisionObject.Height / tex.pixelsPerUnit;

                            var centerXPos = (flipX ? -1 : 1) * ( collisionObject.X * 2 + collisionObject.Width - map.TileWidth)  / tex.pixelsPerUnit;
                            var centerYPos = (flipY ? -1 : 1) * -( collisionObject.Y * 2 + collisionObject.Height - map.TileHeight)  / tex.pixelsPerUnit; // the positive y cord in Tiled goes down so we have to flip it

                            boxCol.offset = new Vector2((float)centerXPos, (float)centerYPos) / 2;
                            boxCol.size = new Vector2(width, height);

                            if (collisionObject.Properties.ContainsKey("isTrigger") && collisionObject.Properties["isTrigger"].ToLower() == "true")
                                boxCol.isTrigger = true;
                        }
                        else if (collisionObject.ObjectType == TmxObjectType.Polygon) 
                        {
                            var polCol = go.AddComponent<PolygonCollider2D>();

                            // set the path of the polygon collider
                            // we must convert the TmxPoints to Vector2s
                            var XPos = (flipX ? -1 : 1) *  (float)(collisionObject.X - map.TileWidth  / 2);
                            var YPos = (flipY ? -1 : 1) * -(float)(collisionObject.Y - map.TileHeight / 2); // the positive y cord in Tiled goes down so we have to flip it

                            polCol.SetPath(0, collisionObject.Points.Select(p => new Vector2( (flipX ? -1 : 1) * ((float)p.X), 
                                                                                              (flipY ? -1 : 1) * -((float)p.Y) ) / tex.pixelsPerUnit).ToArray()); 

                            polCol.offset = new Vector2(XPos, YPos) / tex.pixelsPerUnit;
                            if (collisionObject.Properties.ContainsKey("isTrigger") && collisionObject.Properties["isTrigger"].ToLower() == "true")
                                polCol.isTrigger = true;
                        }
                    }
                }


            tileGameobjectMap[tile] = go;
            return go;
        }
        return null;
    }
    
    GameObject getBaseGameobject(TmxLayerTile tile)
    {
        GameObject go = null;
        if (tileIdMap.ContainsKey(tile.Gid) && tileIdMap[tile.Gid].Properties.ContainsKey("basePrefab"))
        {
            var fabs = prefabs.Where(p => p.name == tileIdMap[tile.Gid].Properties["basePrefab"]).ToList();
            if (fabs.Count != 0)
            {
                go = Instantiate(fabs.First());
            }
        }
        else
        {
            go = new GameObject("Tile");
        }
        if (go.GetComponent<SpriteRenderer>() == null)
            go.AddComponent<SpriteRenderer>();
        
        return go;
    }



    public TmxLayer getTmxLayer(string layer)
    {
        return map.Layers[layer];
    }

    public Vector2 convertWorldPosToTilePos(Vector3 point)
    {
        Vector3 p = (point - transform.position) * pixelsPerUnit;
        return new Vector3(Mathf.Floor(p.x / map.TileWidth), Mathf.Floor(p.y / map.TileHeight), 0); // Round the vector to get those nice integer values
    }


    public Dictionary<string, string> getLayerProperties(string layer)
    {
        return getTmxLayer(layer).Properties;
    }
    public Dictionary<string, string> getTileProperties(string layer, Vector3 gridlocation)
    {
        return getTileProperties(layer, Mathf.FloorToInt(gridlocation.x), Mathf.FloorToInt(gridlocation.y));
    }
    public Dictionary<string, string> getTileProperties(string layer, int gridLocationX, int gridLocationY)
    {
        if (tileIdMap.ContainsKey(getTmxTile(layer, gridLocationX, gridLocationY).Gid)) return tileIdMap[getTmxTile(layer, gridLocationX, gridLocationY).Gid].Properties;
        else return new Dictionary<string, string>();
    }



    public TmxLayerTile getTmxTile(string layer, Vector3 gridlocation)
    {
        return getTmxTile(layer, Mathf.FloorToInt(gridlocation.x), Mathf.FloorToInt(gridlocation.y));
    }
    public TmxLayerTile getTmxTile(string layer, int gridLocationX, int gridLocationY)
    {
        return getTmxLayer(layer).Tiles.ToList().Find(t => t.X == gridLocationX && t.Y == gridLocationY);
    }


    public GameObject getTileObject(string layer, Vector3 gridlocation)
    {
        return getTileObject(layer, Mathf.FloorToInt(gridlocation.x), Mathf.FloorToInt(gridlocation.y));
    }
    public GameObject getTileObject(string layer, int gridLocationX, int gridLocationY)
    {
        return tileGameobjectMap[map.Layers[layer].Tiles.ToList().Find(t => t.X == gridLocationX && t.Y == gridLocationY)];
    }


    static void Swap<T>(ref T lhs, ref T rhs)
    {
        T temp = lhs;
        lhs = rhs;
        rhs = temp;
    }


    // Update is called once per frame
    void Update () {
	
	}
}
