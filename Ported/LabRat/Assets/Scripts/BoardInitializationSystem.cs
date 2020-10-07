﻿using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public class BoardInitializationSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities.WithStructuralChanges()
            .ForEach((Entity entity, in Board board) =>
            {
                var rand = new Unity.Mathematics.Random((uint) DateTime.Now.Millisecond);
                var boardQuarter = board.size / 4;
                
                var holeProbability = board.holeCount / (float) (board.size * board.size);
                var holeCount = board.holeCount;
                
                var wallProbability = board.wallCount / (float) (board.size * board.size);
                var wallCount = board.wallCount;

                for (int x = 0; x < board.size; ++x)
                for (int z = 0; z < board.size; ++z)
                {
                    uint localWall = 0;
                    
                    var posX = x;
                    var posY = rand.NextFloat(0.0f, board.yNoise);
                    var posZ = z;
                    
                    var tileInstance = EntityManager.Instantiate(board.tilePrefab);
                    SetComponent(tileInstance, new Translation { Value = new float3(posX, posY, posZ) });
                    SetComponent(tileInstance,
                        (x + z) % 2 == 0
                            ? new URPMaterialPropertyBaseColor {Value = new float4(0.8f, 0.8f, 0.8f, 1.0f)}
                            : new URPMaterialPropertyBaseColor {Value = new float4(0.6f, 0.6f, 0.6f, 1.0f)});

                    // Place border walls
                    if (x == 0) // East
                    {
                        PlaceWall(posX, posZ, 2, board.wallPrefab, posY, tileInstance);
                        localWall |= 4;
                    }
                    else if (x == (board.size - 1)) // West
                    {
                        PlaceWall(posX, posZ, 3, board.wallPrefab, posY, tileInstance);
                        localWall |= 8;
                    }

                    if (z == 0) // South
                    {
                        PlaceWall(posX, posZ, 1, board.wallPrefab, posY, tileInstance);
                        localWall |= 2;
                    }
                    else if (z == (board.size - 1)) // North
                    {
                        PlaceWall(posX, posZ, 0, board.wallPrefab, posY, tileInstance);
                        localWall |= 1;
                    }

                    // Place home base
                    if (x == boardQuarter && z == boardQuarter)
                        PlaceHomeBase(posX, posZ, 0, board.homeBasePrefab, posY, tileInstance);
                    else if (x == boardQuarter && z == board.size - boardQuarter - 1)
                        PlaceHomeBase(posX, posZ, 1, board.homeBasePrefab, posY, tileInstance);
                    else if (x == board.size - boardQuarter - 1 && z == board.size - boardQuarter - 1)
                        PlaceHomeBase(posX, posZ, 2, board.homeBasePrefab, posY, tileInstance);
                    else if (x == board.size - boardQuarter - 1 && z == boardQuarter)
                        PlaceHomeBase(posX, posZ, 3, board.homeBasePrefab, posY, tileInstance);
                    else  // Not a base: can be a hole
                    {
                        if (holeCount > 0 && rand.NextFloat(1f) < holeProbability)
                        {
                            EntityManager.DestroyEntity(tileInstance);

                            tileInstance = EntityManager.Instantiate(board.invisibleTilePrefab);
                            SetComponent(tileInstance, new Translation { Value = new float3(posX, posY, posZ) });
                            EntityManager.AddComponent<Hole>(tileInstance);
                            holeCount--;
                        }
                    }
                    
                    // Place other wall
                    if (wallCount > 0 && localWall < 15 && rand.NextFloat(1f) < wallProbability)
                    {
                        var coordinate = 2 ^ rand.NextUInt(0, 3);
                        if ((localWall & coordinate) == 0)
                        {
                            PlaceWall(posX, posZ, coordinate, board.wallPrefab, posY, tileInstance);
                            wallCount--;
                        }
                    }
                }

                var playerArchetypes = EntityManager.CreateArchetype(typeof(AICursor), typeof(PlayerTransform), typeof(Position));
                var mainPlayerArchetypes = EntityManager.CreateArchetype(typeof(PlayerCursor), typeof(PlayerTransform), typeof(Position));
                for (int i = 0; i < 4; ++i)
                {
                    var player =  EntityManager.CreateEntity(i == 0 ? mainPlayerArchetypes : playerArchetypes);
                    SetComponent(player, new PlayerTransform
                    {
                        Index = i
                    });
                    var position = new Position();
                    switch (i)
                    {
                        case 0:
                            position.Value = new int2(boardQuarter, boardQuarter);
                            break;
                        case 1:
                            position.Value = new int2(boardQuarter, board.size - boardQuarter - 1);
                            break;
                        case 2:
                            position.Value = new int2(board.size - boardQuarter - 1, boardQuarter);
                            break;
                        case 3:
                            position.Value = new int2(board.size - boardQuarter - 1, board.size - boardQuarter - 1);
                            break;
                    }
                    SetComponent(player, position);
                    if (i > 0)
                    {
                        SetComponent(player, new AICursor
                        {
                            Destination = position.Value
                        });
                    }
                }

                var gameInfo = EntityManager.CreateEntity(typeof(GameInfo));
                SetComponent(gameInfo, new GameInfo(){boardSize = new int2(board.size, board.size)});
                
                EntityManager.DestroyEntity(entity);
        }).Run();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <param name="coordinate">North (0), South (1), East (2) or West (3)</param>
    /// <param name="board"></param>
    /// <param name="yOffset"></param>
    void PlaceWall(
        float posX, 
        float posZ, 
        uint coordinate, 
        Entity wallPrefab, 
        float yOffset,
        Entity tileInstance)
    {
        switch (coordinate)
        {
            case 0: // North
                posZ += 0.5f;
                AddWallToTile(tileInstance, DirectionDefines.North);
                break;
            case 1: // South
                posZ -= 0.5f;
                AddWallToTile(tileInstance, DirectionDefines.South);
                break;
            case 2: // West 
                posX -= 0.5f;
                AddWallToTile(tileInstance, DirectionDefines.West);
                break;
            case 3: // East
                posX += 0.5f;
                AddWallToTile(tileInstance, DirectionDefines.East);
                break;
            default:
                break;
        }

        var instance = EntityManager.Instantiate(wallPrefab);
        SetComponent(instance, new Translation { Value = new float3(posX, 0.725f + yOffset, posZ) });

        if (coordinate <= 1)  // Turn the wall if it's placed on North or South
            SetComponent(instance, new Rotation { Value = quaternion.RotateY(math.radians(90f)) });

    }

    void AddWallToTile(Entity tileEntity, byte wallDirection)
    {
        if (HasComponent<Wall>(tileEntity))
        {
            var wallComponent = GetComponent<Wall>(tileEntity);
            wallComponent.Value |= wallDirection;
            SetComponent<Wall>(tileEntity, wallComponent);
        }
        else
        {
            EntityManager.AddComponent<Wall>(tileEntity);
            SetComponent<Wall>(tileEntity, new Wall { Value = wallDirection });
        }
    }

    void PlaceHomeBase(
        int posX, 
        int posZ, 
        int playerIndex, 
        Entity homeBasePrefab, 
        float yOffset,
        Entity tileInstance)
    {
        var homebaseInstance = EntityManager.Instantiate(homeBasePrefab);
        SetComponent(homebaseInstance, new Translation { Value = new float3(posX, yOffset, posZ) });

        URPMaterialPropertyBaseColor color;
        switch (playerIndex)
        {
            case 0: // Red
                color = new URPMaterialPropertyBaseColor {Value = new float4(1.0f, 0.0f, 0.0f, 1.0f)};
                break;
            case 1: // Green
                color = new URPMaterialPropertyBaseColor {Value = new float4(0.0f, 1.0f, 0.0f, 1.0f)};
                break;
            case 2: // Blue
                color = new URPMaterialPropertyBaseColor {Value = new float4(0.0f, 0.0f, 1.0f, 1.0f)};
                break;
            default: // Black
                color = new URPMaterialPropertyBaseColor {Value = new float4(1.0f, 1.0f, 1.0f, 1.0f)};
                break;
        }
        SetComponent(homebaseInstance, color);

        EntityManager.AddComponent<HomeBase>(tileInstance);
        SetComponent<HomeBase>(tileInstance, new HomeBase
        {
            playerIndex = (byte)playerIndex,
            playerScore = 0
        });
    }
}

