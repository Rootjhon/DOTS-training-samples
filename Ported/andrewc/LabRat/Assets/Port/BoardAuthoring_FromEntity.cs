﻿using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.Transforms;


[RequiresEntityConversion]
public class BoardAuthoring_FromEntity : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public GameObject TilePrefab;
    public GameObject WallPrefab;
    public GameObject HomebasePrefab;
    public float yNoise;

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(TilePrefab);
        referencedPrefabs.Add(WallPrefab);
        referencedPrefabs.Add(HomebasePrefab);
    }

    //private void 

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        // Board Generation
        Debug.Log("generating board");

        // Grab entity prefabs
        Entity WallEntity = conversionSystem.GetPrimaryEntity(WallPrefab);
        Entity TileEntity = conversionSystem.GetPrimaryEntity(TilePrefab);
        Entity HomebaseEntity = conversionSystem.GetPrimaryEntity(HomebasePrefab);

        BoardSystem boardSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BoardSystem>();
        Vector2 CellSize = boardSystem.Board.CellSize;

        for (int i = 0; i < Board.k_Width; i++)
        {
            for(int j = 0; j < Board.k_Height; j++)
            {

                Tile currTile = boardSystem.Board[i, j];

                switch(currTile.TileType)
                {
                    case eTileType.Blank:
                        {
                            Entity tile = dstManager.Instantiate(TileEntity);
                            dstManager.SetComponentData(tile, new Translation { Value = new Vector3(i * CellSize.x, Random.value * yNoise, j * CellSize.y) });
                            break;
                        }
                    case eTileType.Hole:
                        break;
                    case eTileType.HomeBase:
                        {
                            Vector3 spawnTrans = new Vector3(i * CellSize.x, Random.value * yNoise, j * CellSize.y);
                            Entity tile = dstManager.Instantiate(TileEntity);
                            Entity homebase = dstManager.Instantiate(HomebaseEntity);
                            dstManager.SetComponentData(tile, new Translation { Value = spawnTrans });
                            dstManager.SetComponentData(homebase, new Translation { Value = spawnTrans + new Vector3(0, 1, 0) });
                            break;
                        }
                    default:
                        break;
                }

                // Spawn wall logic
                if(currTile.HasWall(eDirection.East))
                {
                    Entity wall = dstManager.Instantiate(WallEntity);
                    Quaternion rot = Quaternion.Euler(0, 0, 0);
                    dstManager.SetComponentData(wall, new Translation { Value = new Vector3(i * CellSize.x + 0.5f , 0.7f , j*CellSize.y) });
                    dstManager.SetComponentData(wall, new Rotation { Value = rot });
                }
                if (currTile.HasWall(eDirection.West))
                {
                    Entity wall = dstManager.Instantiate(WallEntity);
                    Quaternion rot = Quaternion.Euler(0, 0, 0);
                    dstManager.SetComponentData(wall, new Translation { Value = new Vector3(i * CellSize.x - 0.5f , 0.7f, j * CellSize.y) });
                    dstManager.SetComponentData(wall, new Rotation { Value = rot });
                }
                if (currTile.HasWall(eDirection.North))
                {
                    Entity wall = dstManager.Instantiate(WallEntity);
                    Quaternion rot = Quaternion.Euler(0, 90, 0);
                    dstManager.SetComponentData(wall, new Translation { Value = new Vector3(i * CellSize.x, 0.7f, j * CellSize.y + 0.5f) });
                    dstManager.SetComponentData(wall, new Rotation { Value = rot });
                }

                if (currTile.HasWall(eDirection.South))
                {
                    Entity wall = dstManager.Instantiate(WallEntity);
                    Quaternion rot = Quaternion.Euler(0, 90, 0);
                    dstManager.SetComponentData(wall, new Translation { Value = new Vector3(i * CellSize.x, 0.7f, j * CellSize.y - 0.5f) });
                    dstManager.SetComponentData(wall, new Rotation { Value = rot });
                }
            }
        }
    }
}
