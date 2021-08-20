using System.Collections;
using System.Collections.Generic;

using UnityEngine.InputSystem;

using Unity.MLAgents;
using Unity.MLAgents.Sensors;

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
    /// Movement input obtained from InputSystem
    /// </summary>
    private Vector2 moveInput;

    /// <summary>
    /// Float up and down input obtained from InputSystem
    /// </summary>
    private float floatInput;

    /// <summary>
    /// Movement input obtained from InputSystem
    /// </summary>
    private Vector2 lookInput;

    /// <summary>
    /// InputAction callback for move input
    /// </summary>
    /// <param name="context">The CallbackContext for the InputAction</param>
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// InputAction callback for look input
    /// </summary>
    /// <param name="context">The CallbackContext for the InputAction</param>
    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// InputAction callback for float up and down input
    /// </summary>
    /// <param name="context">The CallbackContext for the InputAction</param>
    public void OnFloat(InputAction.CallbackContext context)
    {
        floatInput = context.ReadValue<float>();
    }

    /// <summary>
    /// Initialize the agent
    /// </summary>
    public override void Initialize()
    {
        rigidbody = GetComponent<Rigidbody>();
        flowerPatch = FindObjectOfType<FlowerPatch>();

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
        MoveToSafeRandomPosition(inFrontOfFlower);

        // Recalculate nearest flower after agent moved
        UpdateNearestFlower();
    }

    /// <summary>
    /// <para>Called when an action is received from either the player input or the neural network</para>
    /// 
    /// 
    /// <list type="table">
    ///    <para><i>For vectorAction[i]:</i></para>
    ///    <item>
    ///        <term>Index 0</term>
    ///        <description>move vector x (right-positive)</description>
    ///    </item>
    ///    <item>
    ///        <term>Index 1</term>
    ///        <description>move vector y (up-positive)</description>
    ///    </item>
    ///    <item>
    ///        <term>Index 2</term>
    ///        <description>move vector z (forward-positive)</description>
    ///    </item>
    ///    <item>
    ///        <term>Index 3</term>
    ///        <description>pitch angle (up-positive)</description>
    ///    </item>
    ///    <item>
    ///        <term>Index 4</term>
    ///        <description>yaw angle (right-positive)</description>
    ///    </item>
    /// </list>
    /// </summary>
    /// <param name="vectorAction">The actions to take</param>
    public override void OnActionReceived(float[] vectorAction)
    {
        // Don't take actions if frozen
        if (frozen) return;

        // Calculate movement vector
        Vector3 move = new Vector3(vectorAction[0], vectorAction[1], vectorAction[2]).normalized;

        // Add force in direction of move vector
        rigidbody.AddForce(move * moveForce);

        // Get current rotation
        Vector3 rotationVector = transform.rotation.eulerAngles;

        // Calculate pitch and yaw
        float pitchChange = vectorAction[3];
        float yawChange = vectorAction[4];

        // Calculate smooth rotation changes
        smoothPitchChange = Mathf.MoveTowards(smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
        smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);

        // Calculate new clamped pitch and yaw based on smoothed values
        float pitch = rotationVector.x + (smoothPitchChange * Time.fixedDeltaTime * pitchSpeed);
        if (pitch > 180f) pitch -= 360f;
        pitch = Mathf.Clamp(pitch, -MaxPitchAngle, MaxPitchAngle);

        float yaw = rotationVector.y + (smoothYawChange * Time.fixedDeltaTime * yawSpeed);

        // Apply new rotation
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    /// <summary>
    /// Collect vector observations from the envrionement
    /// </summary>
    /// <param name="sensor">The vector sensor (10 total observations)</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        // If nearest flower is null, observe an empty array
        if (nearestFlower == null)
        {
            sensor.AddObservation(new float[10]);
            return;
        }

        // Observe the agent's local rotation (4 observations)
        sensor.AddObservation(transform.localRotation.normalized);

        // Get a vector from beak tip to neaerest flower
        Vector3 toFlower = nearestFlower.FlowerCenterPosition - beakTip.position;

        // Observe the direction vector to nearest flower (3 observations)
        sensor.AddObservation(toFlower.normalized);

        // Observe a dot product that indicates whether the beak is in front of the flower (1 observation)
        sensor.AddObservation(Vector3.Dot(toFlower.normalized, -nearestFlower.FlowerUpVector.normalized));

        // Obsere a dot product that indicates whether the beak is pointing torwards the flower (1 observation)
        sensor.AddObservation(Vector3.Dot(beakTip.forward.normalized, -nearestFlower.FlowerUpVector.normalized));

        // Observe the normalized distance from the beak to the flower (1 observation)
        sensor.AddObservation(toFlower.magnitude / FlowerPatch.AreaDiameter);
    }

    /// <summary>
    /// When Behavior Type is set to "Heuristic Only" on the agent's Behaviour 
    /// Parameters, this function will be called. Its return values will be fed into 
    /// <see cref="OnActionReceived(float[])"/> instead of using the neural network.
    /// </summary>
    /// <param name="actionsOut">Output action array</param>
    public override void Heuristic(float[] actionsOut)
    {
        // Move inputs
        Vector3 forward = moveInput.y * transform.forward;
        Vector3 right = moveInput.x * transform.right;
        Vector3 up = floatInput * transform.up;

        // Look inputs
        float pitch = -lookInput.y;
        float yaw = lookInput.x;

        // Combine movement vectors and normalize
        Vector3 combined = (forward + right + up).normalized;

        // Add inputs to actionsOut array
        actionsOut[0] = combined.x;
        actionsOut[1] = combined.y;
        actionsOut[2] = combined.z;
        actionsOut[3] = pitch;
        actionsOut[4] = yaw;
    }

    /// <summary>
    /// Prevent the agent from moving and taking actions
    /// </summary>
    public void FreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/Unfreeze not supported in training");

        frozen = true;
        rigidbody.Sleep();
    }

    /// <summary>
    /// Resume agent movement and actions
    /// </summary>
    public void UnfreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/Unfreeze not supported in training");

        frozen = false;
        rigidbody.WakeUp();
    }

    /// <summary>
    /// Move the agent to a safe random position (i.e. does not collide with anything).
    /// If in front of flower, also point the beak at the flower.
    /// </summary>
    /// <param name="inFrontOfFlower">Whether to choose a spot in front of a flower</param>
    private void MoveToSafeRandomPosition(bool inFrontOfFlower)
    {
        bool safePositionFound = false;
        int attemptsRemaining = 100;
        Vector3 potentialPosition = Vector3.zero;
        Quaternion potentialRotation = Quaternion.identity;

        // Loop until a safe position is found or we run out of attempts
        while (!safePositionFound && attemptsRemaining > 0)
        {
            if (inFrontOfFlower)
            {
                // Pick random flower
                Flower randomFlower = flowerPatch.Flowers[Random.Range(0, flowerPatch.Flowers.Count)];

                // Position near the flower
                float distanceFromFlower = Random.Range(.1f, .2f);
                potentialPosition = randomFlower.transform.position
                    + (randomFlower.FlowerUpVector * distanceFromFlower);

                // Point beak at flower (bird's head is center of transform)
                Vector3 toFlower = randomFlower.FlowerCenterPosition - potentialPosition;
                potentialRotation = Quaternion.LookRotation(toFlower, Vector3.up);
            }
            else
            {
                // Pick a random height from the ground
                float height = Random.Range(1.2f, 2.5f);

                //  Pick a random radius from the center of the area
                float radius = Random.Range(2f, 7f);

                // Pick a random direction rotated around the y azis
                Quaternion direction = Quaternion.Euler(
                    0f,
                    Random.Range(-180f, 180f),
                    0f);

                // Combine height, radius, and direction to pick a potential position
                potentialPosition = flowerPatch.transform.position
                    + (Vector3.up * height)
                    + (direction * Vector3.forward * radius);

                // Choose and set random starting pitch and yaw
                float pitch = Random.Range(-60f, 60f);
                float yaw = Random.Range(-180f, 180f);
                potentialRotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            // Check if agent collides with anything
            Collider[] colliders = Physics.OverlapSphere(potentialPosition, 0.05f);

            // Is safe position found
            safePositionFound = colliders.Length is 0;

            attemptsRemaining--;
        }

        Debug.Assert(safePositionFound, "Could not find safe positon to spawn");

        // Set the positon and rotation
        transform.position = potentialPosition;
        transform.rotation = potentialRotation;
    }

    /// <summary>
    /// Update the nearest flower to the agent
    /// </summary>
    private void UpdateNearestFlower()
    {
        foreach (Flower flower in flowerPatch.Flowers)
        {
            if (nearestFlower == null && flower.HasNectar)
            {
                // If nearest flower is not set, set to this flower with nectar
                nearestFlower = flower;
            }
            else if (flower.HasNectar)
            {
                // Compare distance to this flower and distance to current flower
                float distanceToNewFlower = Vector3.Distance(flower.transform.position, beakTip.position);
                float distanceToCurrentFlower = Vector3.Distance(nearestFlower.transform.position, beakTip.position);

                // If current flower is empty or this flower is closer, update the nearest flower
                if (!nearestFlower.HasNectar || distanceToNewFlower < distanceToCurrentFlower)
                {
                    nearestFlower = flower;
                }
            }
        }
    }

    /// <summary>
    /// Called when the agent's collider enters a trigger
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerEnter(Collider other)
    {
        OnTriggerEndterOrStay(other);
    }

    /// <summary>
    /// Called when the agent's collider stays in a trigger
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerStay(Collider other)
    {
        OnTriggerEndterOrStay(other);
    }

    /// <summary>
    /// Called when the agent's collider enters or stays in a trigger
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerEndterOrStay(Collider other)
    {
        // Check if colliding with nectar
        if (other.CompareTag(GameManager.NectarTag))
        {
            Vector3 closestPointToBeak = other.ClosestPoint(beakTip.position);

            // Check if beak tip is in nectar collider
            if (Vector3.Distance(beakTip.position, closestPointToBeak) < BeakTipRadius)
            {
                // Look up the flower for this nectar collider
                Flower flower = flowerPatch.GetFlowerFromNectar(other);

                // Attempt to take nectar
                float nectarReceived = flower.Feed(.01f);

                // Keep track of nectar obtained
                NectarObtained += nectarReceived;

                if (trainingMode)
                {
                    // Calculate reward for getting nectar
                    float bonus = .02f * Mathf.Clamp01(Vector3.Dot(transform.forward.normalized, -nearestFlower.FlowerUpVector.normalized));
                    AddReward(.01f + bonus);
                }

                // If flower is empty, update nearest flower
                if (!flower.HasNectar)
                {
                    UpdateNearestFlower();
                }
            }
        }
    }

    /// <summary>
    /// Called when the agent collides with something solid
    /// </summary>
    /// <param name="collision">The collision information</param>
    private void OnCollisionEnter(Collision collision)
    {
        if (trainingMode && collision.collider.CompareTag(GameManager.BoundaryTag))
        {
            // Give punishment for colliding with boundary
            AddReward(-.5f);
        }
    }

    /// <summary>
    /// Called every frame
    /// </summary>
    private void Update()
    {
        if (nearestFlower != null)
        {
            Debug.DrawLine(beakTip.position, nearestFlower.FlowerCenterPosition, Color.green);
        }
    }

    /// <summary>
    /// Called every fixed time interval
    /// </summary>
    private void FixedUpdate()
    {
        // If flower got stolen by opponent before agent could get to it.
        if (nearestFlower != null && !nearestFlower.HasNectar)
        {
            UpdateNearestFlower();
        }
    }
}
