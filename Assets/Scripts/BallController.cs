using UnityEngine;

public class BallController : MonoBehaviour
{
    [Header("Possession")]
    public PlayerController owner;
    public float possessionRadius = 0.16f;
    public float tackleRadius = 0.22f;
    public float dribbleOffset = 0.13f;

    [Header("Last Touch")]
    public PlayerController lastTouchPlayer;
    public bool lastTouchHome = true;

    [Header("Timing")]
    public float possessionCooldown = 1.5f;
    public float passerPickupDelay = 0.25f;

    [Header("Goalkeeper Settings")]
    public float goalkeeperPickupSpeed = 0.6f;

    [Header("Pass Tracking")]
    public PlayerController intendedReceiver;
    public Vector2 passTarget;
    public bool passInProgress;
    public float receiverPickupBonus = 0.05f;
    public float maximumPassDuration = 2.5f;

    [Header("Passing Statistics")]
    public int homeCompletedPasses;
    public int awayCompletedPasses;
    public int interceptedPasses;

    private float cooldownTimer;
    private float releaseTimer;
    private float passTimer;

    private PlayerController lastPasser;
    private PlayerController releasedBy;

    private Rigidbody2D rb;
    private PlayerController[] players;

    public Vector2 BallVelocity
    {
        get
        {
            return rb != null
                ? rb.linearVelocity
                : Vector2.zero;
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (Time.deltaTime <= 0f)
            return;

        if (players == null || players.Length == 0)
        {
            players = FindObjectsByType<PlayerController>(
                FindObjectsSortMode.None);
        }

        cooldownTimer -= Time.deltaTime;
        releaseTimer -= Time.deltaTime;

        if (passInProgress)
        {
            passTimer -= Time.deltaTime;

            if (passTimer <= 0f ||
                intendedReceiver == null)
            {
                ClearPass();
            }
        }

        if (owner == null)
        {
            FindNewOwner();
        }
        else if (cooldownTimer <= 0f)
        {
            CheckForTackle();
        }
    }

    void FixedUpdate()
    {
        if (owner != null)
            FollowOwner();
    }

    void FindNewOwner()
    {
        if (cooldownTimer > 0f)
            return;

        PlayerController closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (PlayerController player in players)
        {
            if (player == null || player.data == null)
                continue;

            if (player == releasedBy &&
                releaseTimer > 0f)
                continue;

            bool isGoalkeeper =
                player.data.preferredPosition ==
                PlayerPosition.Goalkeeper;

            if (isGoalkeeper &&
                rb.linearVelocity.magnitude >
                goalkeeperPickupSpeed)
                continue;

            float pickupRadius = possessionRadius;

            if (passInProgress &&
                player == intendedReceiver)
            {
                pickupRadius += receiverPickupBonus;
            }

            float distance = Vector2.Distance(
                player.transform.position,
                transform.position);

            if (distance <= pickupRadius &&
                distance < closestDistance)
            {
                closest = player;
                closestDistance = distance;
            }
        }

        if (closest != null)
            SetOwner(closest);
    }

    public void RegisterTouch(PlayerController player)
    {
        if (player == null)
            return;

        lastTouchPlayer = player;
        lastTouchHome = player.isHomeTeam;
    }

    public void SetOwner(PlayerController newOwner)
    {
        if (passInProgress &&
            lastPasser != null &&
            newOwner != null &&
            newOwner != lastPasser)
        {
            if (newOwner.isHomeTeam ==
                lastPasser.isHomeTeam)
            {
                if (newOwner.isHomeTeam)
                    homeCompletedPasses++;
                else
                    awayCompletedPasses++;
            }
            else
            {
                interceptedPasses++;
            }
        }

        ClearPass();

        if (owner != null)
            owner.hasBall = false;

        owner = newOwner;

        if (owner != null)
        {
            RegisterTouch(owner);
            owner.hasBall = true;

            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        else
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
        }

        releasedBy = null;
        releaseTimer = 0f;

        cooldownTimer = possessionCooldown;
    }

    void FollowOwner()
    {
        Vector2 direction = owner.isHomeTeam
            ? Vector2.right
            : Vector2.left;

        Vector2 ballPosition =
            (Vector2)owner.transform.position +
            direction * dribbleOffset;

        rb.MovePosition(ballPosition);
    }

    void CheckForTackle()
    {
        if (owner == null || owner.data == null)
            return;

        if (owner.data.preferredPosition ==
            PlayerPosition.Goalkeeper)
            return;

        foreach (PlayerController opponent in players)
        {
            if (opponent == null ||
                opponent.data == null ||
                opponent.isHomeTeam == owner.isHomeTeam)
                continue;

            float distance = Vector2.Distance(
                opponent.transform.position,
                owner.transform.position);

            if (distance > tackleRadius)
                continue;

            float tacklePower =
                opponent.data.defending * 0.7f +
                opponent.data.strength * 0.3f;

            float dribblePower =
                owner.data.dribbling * 0.7f +
                owner.data.strength * 0.3f;

            float chance = Mathf.Clamp(
                0.5f +
                (tacklePower - dribblePower) / 100f,
                0.1f,
                0.9f);

            if (Random.value < chance)
                SetOwner(opponent);

            cooldownTimer = possessionCooldown;
            break;
        }
    }

    public void StartPass(
        PlayerController receiver,
        Vector2 target,
        Vector2 velocity)
    {
        PlayerController passer = owner;

        ReleaseBall(velocity);

        lastPasser = passer;
        intendedReceiver = receiver;
        passTarget = target;

        passInProgress =
            passer != null && receiver != null;

        passTimer = maximumPassDuration;

        // Allow immediate reception and interception.
        cooldownTimer = 0f;
    }

    public Vector2 GetReceptionTarget()
    {
        if (!passInProgress ||
            intendedReceiver == null)
        {
            return transform.position;
        }

        Vector2 ballPosition = transform.position;
        Vector2 velocity = BallVelocity;

        if (velocity.magnitude < 0.15f)
            return ballPosition;

        Vector2 receiverPosition =
            intendedReceiver.transform.position;

        Vector2 bestPoint = ballPosition;
        float bestError = Mathf.Infinity;

        // Find a reachable point along the ball's path.
        for (float t = 0.05f; t <= 1.5f; t += 0.05f)
        {
            float damping = rb.linearDamping;

            float travelFactor = damping > 0.001f
                ? (1f - Mathf.Exp(-damping * t)) / damping
                : t;

            Vector2 predicted =
                ballPosition + velocity * travelFactor;

            if (Mathf.Abs(predicted.x) > 3.05f ||
                Mathf.Abs(predicted.y) > 1.7f)
                break;

            float distance = Vector2.Distance(
                receiverPosition, predicted);

            float reachableDistance =
                intendedReceiver.moveSpeed * t +
                possessionRadius +
                receiverPickupBonus;

            if (distance <= reachableDistance)
                return predicted;

            float error = distance - reachableDistance;

            if (error < bestError)
            {
                bestError = error;
                bestPoint = predicted;
            }
        }

        return bestPoint;
    }

    public void ClearPass()
    {
        passInProgress = false;
        intendedReceiver = null;
        lastPasser = null;
        passTimer = 0f;
        passTarget = Vector2.zero;
    }

    public void ReleaseBall(Vector2 velocity)
    {
        ClearPass();

        releasedBy = owner;

        if (owner != null)
        {
            RegisterTouch(owner);
            owner.hasBall = false;
        }

        owner = null;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = velocity;
        rb.angularVelocity = 0f;

        releaseTimer = passerPickupDelay;

        // Preserve existing shot/restart pickup delay.
        cooldownTimer = 0.2f;
    }

    public void ResetBall(Vector2 position)
    {
        ReleaseBall(Vector2.zero);

        rb.position = position;
        transform.position = position;

        releasedBy = null;
        releaseTimer = 0f;
        cooldownTimer = 0.2f;
    }
}