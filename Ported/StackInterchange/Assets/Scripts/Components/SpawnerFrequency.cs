using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[GenerateAuthoringComponent]
public struct SpawnerFrequency : IComponentData
{
    public float Value;
}