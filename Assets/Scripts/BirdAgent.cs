using System.Collections;
using System.Collections.Generic;

using Unity.MLAgents;

using UnityEngine;

/// <summary>
/// A hummingbird Machine Learning Agent
/// </summary>
public class BirdAgent : Agent
{
    [Tooltip("Force to apply when moving")]
    [SerializeField]
    private float moveForce = 2f;

    [Tooltip("Speed to pitch up or down")]
    [SerializeField]
    private float pitchSpeed = 100f;

    [Tooltip("Speed to rotate around the up axis")]
    [SerializeField]
    private float yawSpeed = 100f;

    [Tooltip("Transform at the tip of the beak")]
    [SerializeField]
    private Transform beakTip;

    [Tooltip("The agent's camera")]
    [SerializeField]
    private Camera agentCamera;

    [Tooltip("Whetehr this is training mode or gameplay mode")]
    [SerializeField]
    private bool trainingMode;

    /// <summary>
    /// The amount of nectar the agent has obtained this episode
    /// </summary>
    public float NectarObtained { get; private set; }

    /// <summary>
    /// The rigidbody of the agent
    /// </summary>
    new private Rigidbody rigidbody;

    /// <summary>
    /// The flower patch that the agent is in
    /// </summary>
    private FlowerPatch flowerPatch;

    /// <summary>
    /// The nearest flower to the agent
    /// </summary>
    private Flower nearestFlower;

    /// <summary>
    /// Allows for smoother pitch changes
    /// </summary>
    private float smoothPitchChange = 0f;

    /// <summary>
    /// Allows for smoother yaw changes
    /// </summary>
    private float smoothYawChange = 0f;

    /// <summary>
    /// Maximum angle that the bird can bitch up or down
    /// </summary>
    private const float MaxPitchAngle = 80f;

    /// <summary>
    /// Maximum distance from the beak tip to accept nectar collision
    /// </summary>
    private const float BeakTipRadius = 0.008f;

    /// <summary>
    /// Whether the agent is frozen (intentionally not flying)
    /// </summary>
    private bool frozen = false;

    /// <summary>
    /// Initialize the agent
    /// </summary>
    public override void Initialize()
    {
        rigidbody = GetComponent<Rigidbody>();
        flowerPatch = GetComponentInParent<FlowerPatch>();

        // Play forever if not training
        if (!trainingMode)
        {
            MaxStep = 0;
        }
    }

    /// <summary>
    /// Reset the agent when an episode begins
    /// </summary>
    public override void OnEpisodeBegin()
    {
        // Only reset flowers in training when there is one agent per area
        if (trainingMode)
        {
            flowerPatch.ResetFlowers();
        }

        // Reset nectar obtained
        NectarObtained = 0;

        // Re-zero velocities (so movement stops)
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;

        // Spawn in front of flower 50% of the time during training
        bool inFrontOfFlower = true;
        if (trainingMode)
        {
            inFrontOfFlower = Random.value > .5f;
        }

        // Move the agent to a new random position
        MoveTOSafeRandomPosition(inFrontOfFlower);

        // Recalculate nearest flower after agent moved
        UpdateNearestFlower();
    }

    private void UpdateNearestFlower()
    {
        throw new System.NotImplementedException();
    }

    private void MoveTOSafeRandomPosition(bool inFrontOfFlower)
    {
        throw new System.NotImplementedException();
    }
}
