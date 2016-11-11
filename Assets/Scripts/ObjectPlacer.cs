using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using TiledSharp;



public class ObjectPlacer : MonoBehaviour {

    public bool createMapOnAwake = true;
    //public List<Texture2D> sheets = new List<Texture2D>();
    List<Sprite> spriteList = new List<Sprite> { null };
    Dictionary<int, TmxTilesetTile> tileGidMap = new Dictionary<int, TmxTilesetTile>();
    Dictionary<TmxLayerTile, GameObject> tileGameobjectMap = new Dictionary<TmxLayerTile, GameObject>();
    TmxMap map;

    void Start () {
        if (createMapOnAwake) RenderMap("Assets/Resources/Tilemaps/test2.tmx");
    }

    public void RenderMap(string path)
    {
        map = new TmxMap(path);


        foreach (var tileSet in map.Tilesets)
        {
            spriteList.AddRange(Resources.LoadAll<Sprite>("Tilesheets/" + tileSet.Name));
            foreach (var tile in tileSet.Tiles.Values)
            {
                tileGidMap[tile.Id + 1] = tile; // tile ids are always one lower than the tile Gid. yeah it's dumb
            }
        }

        for (int layerindex = 0; layerindex < map.Layers.Count; layerindex++)
        {
            var lparent = new GameObject(map.Layers[layerindex].Name); // object to parent the tiles created under
            lparent.transform.SetParent(transform);

            foreach (var tile in map.Layers[layerindex].Tiles)
            {
                if (tile.Gid > 0)
                {
                    var tex = spriteList[tile.Gid];
                    var destx = tile.X * (map.TileWidth / tex.pixelsPerUnit);
                    var desty = -tile.Y * (map.TileWidth / tex.pixelsPerUnit);
                    var go = addTileAsObject(tile, tex, destx, desty, layerindex);
                    if (go) go.transform.SetParent(lparent.transform);
                }
            }
        }
    }

    GameObject addTileAsObject(TmxLayerTile tile, Sprite tex, float x, float y, int orderInLayer = 0)
    {
        if (tile.Gid > 0)
        {
            var go = new GameObject("Tile");
            go.transform.position = new Vector3(x, y, 0);

            var sprend = go.AddComponent<SpriteRenderer>();
            sprend.sprite = tex;
            sprend.flipX = tile.HorizontalFlip;
            sprend.flipX = tile.VerticalFlip;
            sprend.sortingOrder = orderInLayer;

            if (tileGidMap.ContainsKey(tile.Gid)) // if we have special information about that tile (such as collision information)
                foreach (TmxObjectGroup oGroup in tileGidMap[tile.Gid].ObjectGroups)
                {
                    foreach (TmxObject collisionObject in oGroup.Objects)
                    {
                        if (collisionObject.ObjectType == TmxObjectType.Basic) // add a box collider, square object
                        {
                            var boxCol = go.AddComponent<BoxCollider2D>();

                            // Don't forget we have to convert from pixels to unity units!
                            float width = (float)collisionObject.Width / tex.pixelsPerUnit;
                            float height = (float)collisionObject.Height / tex.pixelsPerUnit;
                            
                            float centerXPos = (float)(collisionObject.X / tex.pixelsPerUnit);
                            var centerYPos = (float)(-collisionObject.Y / tex.pixelsPerUnit); // the positive y cord in Tiled goes down so we have to flip it

                            boxCol.offset = new Vector2(centerXPos, centerYPos);
                            boxCol.size = new Vector2(width, height);
                        }
                        else if (collisionObject.ObjectType == TmxObjectType.Polygon) 
                        {
                            var polCol = go.AddComponent<PolygonCollider2D>();
                            // set the path of the polygon collider
                            polCol.SetPath(0, collisionObject.Points.Select(p => new Vector2((float)p.X, -(float)p.Y)/ tex.pixelsPerUnit).ToArray()); // we must convert the TmxPoints to Vector2s
                            polCol.offset = new Vector2((float)collisionObject.X, (float)collisionObject.Y) / 2 / tex.pixelsPerUnit;
                        }
                    }
                }

            tileGameobjectMap[tile] = go;
            return go;
        }
        return null;
    }

    TmxLayerTile getTmxTile(string layer, int gridLocationX, int gridLocationY)
    {
        return map.Layers[layer].Tiles.ToList().Find(t => t.X == gridLocationX && t.Y == gridLocationY);
    }

    GameObject getTile(string layer, int gridLocationX, int gridLocationY)
    {
        return tileGameobjectMap[map.Layers[layer].Tiles.ToList().Find(t => t.X == gridLocationX && t.Y == gridLocationY)];
    }


    // Update is called once per frame
    void Update () {
	
	}
}
