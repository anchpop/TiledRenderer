using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using TiledSharp;

/*[Serializable]
public struct TilesetFiles
{
    public Texture2D tilesetTexture;
    public int horisontalTiles;
    public int verticalTiles;
}*/ // This part is a work in progress designed to make it so the tilesets don't need to be in the resources folder and blank tiles aren't ignored

public class ObjectPlacer : MonoBehaviour
{
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

            //var texture = tilesets.Find(c => c.tilesetTexture.name == tileSet.Name);
            foreach (var tile in tileSet.Tiles.Values)
            {
                //spriteList.Add(texture, new Rect(), new Vector2(0.5f, 0.5f));
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
                    var go = addTileObject(tile, tex, destx, desty, layerindex);
                    if (go) go.transform.SetParent(lparent.transform);
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
            
            var go = new GameObject("Tile");
            go.transform.position = new Vector3(x, y, 0);
            go.transform.Rotate(new Vector3(0, 0, rotate90 ? 90 : 0));

            var sprend = go.AddComponent<SpriteRenderer>();
            sprend.sprite = tex;
            sprend.flipX = flipX;
            sprend.flipY = flipY;
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
                            float width  = (float)collisionObject.Width / tex.pixelsPerUnit;
                            float height = (float)collisionObject.Height / tex.pixelsPerUnit;

                            var centerXPos = (flipX ? -1 : 1) * ( collisionObject.X * 2 + collisionObject.Width - map.TileWidth)  / tex.pixelsPerUnit;
                            var centerYPos = (flipY ? -1 : 1) * -( collisionObject.Y * 2 + collisionObject.Height - map.TileHeight)  / tex.pixelsPerUnit; // the positive y cord in Tiled goes down so we have to flip it

                            boxCol.offset = new Vector2((float)centerXPos, (float)centerYPos) / 2;
                            boxCol.size = new Vector2(width, height);
                        }
                        else if (collisionObject.ObjectType == TmxObjectType.Polygon) 
                        {
                            var polCol = go.AddComponent<PolygonCollider2D>();

                            // set the path of the polygon collider
                            // we must convert the TmxPoints to Vector2s
                            var XPos = (flipX ? -1 : 1) * (float)(collisionObject.X - map.TileWidth/2 );
                            var YPos = (flipY ? -1 : 1) * -(float)(collisionObject.Y - map.TileHeight/2); // the positive y cord in Tiled goes down so we have to flip it

                            polCol.SetPath(0, collisionObject.Points.Select(p => new Vector2( (flipX ? -1 : 1) * ((float)p.X), 
                                                                                              (flipY ? -1 : 1) * -((float)p.Y) ) / tex.pixelsPerUnit).ToArray()); 

                            polCol.offset = new Vector2(XPos, YPos) / tex.pixelsPerUnit;
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

    public static void Swap<T>(ref T lhs, ref T rhs)
    {
        T temp = lhs;
        lhs = rhs;
        rhs = temp;
    }


    // Update is called once per frame
    void Update () {
	
	}
}
