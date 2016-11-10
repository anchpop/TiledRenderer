using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using TiledSharp;



public class ObjectPlacer : MonoBehaviour {

    public bool createMapOnAwake = true;
    public bool flipY = true;
    //public List<Texture2D> sheets = new List<Texture2D>();
    List<Sprite> spriteList = new List<Sprite> { null };
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
        }

        foreach (var layer in map.Layers)
        {
            foreach (var tile in layer.Tiles)
            {
                var destx = tile.X * (map.TileWidth/spriteList[tile.Gid].pixelsPerUnit);
                var desty = (flipY ? -1 : 1) * tile.Y * (map.TileWidth / spriteList[tile.Gid].pixelsPerUnit);
                addTileAsObject(tile, destx, desty);
            }
        }
    }

    void addTileAsObject(TmxLayerTile tile, float x, float y)
    {
        if (tile.Gid > 0)
        {
            var go = new GameObject("Tile");
            go.transform.SetParent(transform);
            go.transform.position = new Vector3(x, y, 0);

            var sprend = go.AddComponent<SpriteRenderer>();
            sprend.sprite = spriteList[tile.Gid];
            sprend.flipX = tile.HorizontalFlip;
            sprend.flipX = tile.VerticalFlip;

            tileGameobjectMap[tile] = go;
        }
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
