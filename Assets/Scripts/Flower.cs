using System.Collections;
using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// Manages a single flower with nectar
/// </summary>
public class Flower : MonoBehaviour
{
    /// <summary>
    /// A vector pointing straight out of the flower
    /// </summary>
    public Vector3 FlowerUpVector
    {
        get { return NectarCollider.transform.up; }
    }

    /// <summary>
    /// The center position of the nectar collider
    /// </summary>
    public Vector3 FlowerCenterPosition
    {
        get { return NectarCollider.transform.position; }
    }

    /// <summary>
    /// The amount of nectar remaining in the flower
    /// </summary>
    public float NectarAmount { get; private set; }

    /// <summary>
    /// Whether the flower has any nectar remaining
    /// </summary>
    public bool HasNectar
    {
        get { return NectarAmount > 0f; }
    }

    /// <summary>
    /// The trigger representing the nectar
    /// </summary>
    public Collider NectarCollider { get; private set; }

    /// <summary>
    /// The solid collider representing the flower petals
    /// </summary>
    public Collider FlowerCollider { get; private set; }

    [Tooltip("The color when the flower is full")]
    [SerializeField]
    private Color fullFlowerColor = new Color(1f, 0f, .3f);

    [Tooltip("The color when the flower is full")]
    [SerializeField]
    private Color emptyFlowerColor = new Color(.5f, 0f, 1f);

    /// <summary>
    /// The flower's material
    /// </summary>
    private Material flowerMaterial;


    /// <summary>
    /// Attempt to remove nectar from the flower
    /// </summary>
    /// <param name="amount">The amount of nectar to remove</param>
    /// <returns>Actual amount successfully removed</returns>
    public float Feed(float amount)
    {
        // Track nectar successfully taken (cannot take more than available)
        float nectarTaken = Mathf.Clamp(amount, 0f, NectarAmount);

        // Subtract nectar from flower
        NectarAmount -= amount;

        if (!HasNectar)
        {
            // No nectar remaining
            NectarAmount = 0;

            // Disable the flower and nectar colliders
            FlowerCollider.gameObject.SetActive(false);
            NectarCollider.gameObject.SetActive(false);

            // Change flower color to indicate emptyness
            flowerMaterial.SetColor("_BaseColor", emptyFlowerColor);
        }

        // Return amount of nectar successfully taken
        return nectarTaken;
    }

    /// <summary>
    /// Reset the flower
    /// </summary>
    public void ResetFlower()
    {
        // Refill nectar
        NectarAmount = 1f;

        // Enable the flower and nectar colliders
        FlowerCollider.gameObject.SetActive(true);
        NectarCollider.gameObject.SetActive(true);

        // Change flower color to indicate fullness
        flowerMaterial.SetColor("_BaseColor", fullFlowerColor);
    }

    /// <summary>
    /// Called when the flower instance is being loaded
    /// </summary>
    private void Awake()
    {
        // Find the flower's mesh renderer and get the material
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        flowerMaterial = meshRenderer.material;

        // Find flower and nectar colliders
        FlowerCollider = transform.Find("FlowerCollider").GetComponent<Collider>();
        NectarCollider = transform.Find("FlowerNectarCollider").GetComponent<Collider>();
    }
}
