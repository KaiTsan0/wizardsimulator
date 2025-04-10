using UnityEngine;

[CreateAssetMenu(fileName = "PlaceableData", menuName = "BuildingSystem/PlaceableData", order = 0)]
public class PlaceableData : ScriptableObject
{
    // Cost of the placeable object
    public float cost;

    // Add additional attributes here in the future
    // Example:
    // public int durability;
    // public string description;
}