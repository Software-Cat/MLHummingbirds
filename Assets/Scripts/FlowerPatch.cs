using System.Collections;
using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// Manages a collection of flower plants and attached flowers
/// </summary>
public class FlowerPatch : MonoBehaviour
{
    /// <summary>
    /// The diameter of the area where the agent and flowers can be.
    /// Used for normalization of distance from agent to flower.
    /// </summary>
    public const float AreaDiameter = 20f;

    /// <summary>
    /// The list of all flowers in the flower area
    /// </summary>
    public List<Flower> Flowers { get; private set; } = new List<Flower>();

    /// <summary>
    /// List of all flower plants in the area (each plant has multiple flowers)
    /// </summary>
    private List<GameObject> flowerPlants = new List<GameObject>();

    /// <summary>
    /// Dictionary for looking up a flower from a nectar collider
    /// </summary>
    private Dictionary<Collider, Flower> flowerLookup = new Dictionary<Collider, Flower>();

    /// <summary>
    /// Reset the flowers and flower plants
    /// </summary>
    public void ResetFlowers()
    {
        // Randomize rotations for each flower plant
        foreach (GameObject flowerPlant in flowerPlants)
        {
            float xRotation = Random.Range(-5f, 5f);
            float yRotation = Random.Range(-180f, 180f);
            float zRotation = Random.Range(-5f, 5f);
            flowerPlant.transform.localRotation = Quaternion.Euler(xRotation, yRotation, zRotation);
        }

        // Reset each flower
        foreach (Flower flower in Flowers)
        {
            flower.ResetFlower();
        }
    }

    /// <summary>
    /// Get the <see cref="Flower"/> that a nectar collider belongs to
    /// </summary>
    /// <param name="nectarCollider">The nectar collider</param>
    /// <returns>The matching flower</returns>
    public Flower GetFlowerFromNectar(Collider nectarCollider)
    {
        return flowerLookup[nectarCollider];
    }

    /// <summary>
    /// Called when the game starts
    /// </summary>
    private void Start()
    {
        // Find all flowers that are children of this GameObject's transform
        FindChildFlowers(transform);

    }

    /// <summary>
    /// Recursively finds all flowers and flower plants that are children of a parent transform
    /// </summary>
    /// <param name="parent">The parent transform to check for flowers in</param>
    private void FindChildFlowers(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.CompareTag(GameManager.FlowerPlantTag)) // Found a flower plant
            {
                // Add flower plant to flowerPlants list
                flowerPlants.Add(child.gameObject);

                // Find flowers with in flower plant
                FindChildFlowers(child);
            }
            else if (child.gameObject.HasComponent<Flower>()) // Found a flower
            {
                Flower flower = child.gameObject.GetComponent<Flower>();

                // Add flower to Flowers list
                Flowers.Add(flower);

                // Add nectar collider to the lookup dictionary
                flowerLookup.Add(flower.NectarCollider, flower);
            }
            else // Flower not found
            {
                FindChildFlowers(child);
            }
        }
    }
}
