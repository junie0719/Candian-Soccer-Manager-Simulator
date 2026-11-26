using UnityEngine;

public class GoalkeeperAI : MonoBehaviour
{
    [Header("References")]
    public BallController ballController;

    [Header("Goalkeeper Settings")]
    public float goalLineX = 3.15f;
    public float reactionDistance = 1.5f;
    public float saveRadius = 0.28f;
    public float maximumMovementY = 0.45f;
    public float distributionDelay = 1.5f;
    public float distributionSpeed = 3.5f;
    public float maximumDistributionSpeed = 6f;

    [Header("Statistics")]
    public int homeSaves;
    public int awaySaves;
    public int successfulDistributions;

    private PlayerController[] players;
    private Rigidbody2D ballRb;

    private PlayerController currentKeeper;
    private float holdTimer;
    private float saveCooldown;

    void Start()
    {
        if (ballController != null)
            ballRb = ballController.GetComponent<Rigidbody2D>();
    }

    void LateUpdate()
    {
        if (ballController == null ||
            ballRb == null ||
            Time.deltaTime <= 0f)
            return;

        if (players == null || players.Length == 0)
            players = FindObjectsByType<PlayerController>();

        saveCooldown -= Time.deltaTime;

        HandleDistribution();

        foreach (PlayerController player in players)
        {
            if (player == null || player.data == null)
                continue;

            if (player.data.preferredPosition ==
                PlayerPosition.Goalkeeper)
            {
                UpdateGoalkeeper(player);
            }
        }
    }

    void UpdateGoalkeeper(PlayerController keeper)
    {
        if (ballController.owner == keeper)
        {
            keeper.SetTarget(
                keeper.homePosition,
                "Holding Ball");

            return;
        }

        Vector2 ballPosition =
            ballController.transform.position;

        Vector2 velocity = ballRb.linearVelocity;

        float side = keeper.isHomeTeam ? -1f : 1f;

        Vector2 target = keeper.homePosition;

        target.y = Mathf.Clamp(
            ballPosition.y * 0.4f,
            -maximumMovementY,
            maximumMovementY);

        bool incomingShot =
            ballController.owner == null &&
            velocity.x * side > 0.5f &&
            Mathf.Abs(ballPosition.x -
            side * goalLineX) < reactionDistance;

        if (incomingShot)
        {
            float timeToKeeper =
                (keeper.homePosition.x -
                ballPosition.x) / velocity.x;

            if (timeToKeeper >= 0f)
            {
                float predictedY =
                    ballPosition.y +
                    velocity.y * timeToKeeper;

                target.y = Mathf.Clamp(
                    predictedY,
                    -maximumMovementY,
                    maximumMovementY);
            }

            keeper.SetTarget(
                target,
                "Reacting to Shot");

            TrySave(keeper);
        }
        else
        {
            keeper.SetTarget(
                target,
                "Guarding Goal");
        }
    }

    void TrySave(PlayerController keeper)
    {
        if (saveCooldown > 0f ||
            ballController.owner != null)
            return;

        float distance = Vector2.Distance(
            keeper.transform.position,
            ballController.transform.position);

        if (distance > saveRadius)
            return;

        float ability =
            keeper.data.goalkeeping / 100f;

        float saveChance = Mathf.Lerp(
            0.2f, 0.9f, ability);

        saveCooldown = 0.5f;

        if (Random.value > saveChance)
        {
            keeper.currentAction = "Missed Save";
            return;
        }

        if (keeper.isHomeTeam)
            homeSaves++;
        else
            awaySaves++;

        float catchChance = Mathf.Lerp(
            0.35f, 0.85f, ability);

        if (Random.value < catchChance)
        {
            ballController.SetOwner(keeper);

            keeper.SetTarget(
                keeper.homePosition,
                "Caught Ball");

            Debug.Log(
                keeper.data.playerName +
                " catches the ball!");
        }
        else
        {
            float direction =
                keeper.isHomeTeam ? 1f : -1f;

            Vector2 deflection = new Vector2(
                direction,
                Random.Range(-1f, 1f)
            ).normalized;

            ballController.ReleaseBall(
                deflection * 3.5f);

            ballController.RegisterTouch(keeper);

            keeper.currentAction = "Parried Shot";
        }
    }

    void HandleDistribution()
    {
        PlayerController owner = ballController.owner;

        if (owner == null ||
            owner.data == null ||
            owner.data.preferredPosition !=
            PlayerPosition.Goalkeeper)
        {
            currentKeeper = null;
            holdTimer = 0f;
            return;
        }

        if (currentKeeper != owner)
        {
            currentKeeper = owner;
            holdTimer = 0f;
        }

        holdTimer += Time.deltaTime;

        if (holdTimer >= distributionDelay)
        {
            DistributeBall(owner);

            holdTimer = 0f;
            currentKeeper = null;
        }
    }

    void DistributeBall(PlayerController keeper)
    {
        PlayerController bestTeammate = null;
        Vector2 bestTarget = Vector2.zero;
        float bestScore = float.NegativeInfinity;

        Vector2 ballPosition =
            ballController.transform.position;

        float direction =
            keeper.isHomeTeam ? 1f : -1f;

        foreach (PlayerController teammate in players)
        {
            if (teammate == null ||
                teammate == keeper ||
                teammate.data == null ||
                teammate.isHomeTeam != keeper.isHomeTeam)
                continue;

            if (teammate.data.preferredPosition ==
                PlayerPosition.Goalkeeper)
                continue;

            float distance = Vector2.Distance(
                keeper.transform.position,
                teammate.transform.position);

            if (distance < 0.45f || distance > 3f)
                continue;

            Vector2 target =
                teammate.PredictPosition(0.45f);

            float openness = 5f;

            foreach (PlayerController opponent in players)
            {
                if (opponent == null ||
                    opponent.isHomeTeam == keeper.isHomeTeam)
                    continue;

                openness = Mathf.Min(
                    openness,
                    Vector2.Distance(
                        target,
                        opponent.transform.position));
            }

            float laneSafety = EvaluateLane(
                keeper, ballPosition, target);

            if (laneSafety < 0.25f)
                continue;

            float progress =
                (target.x - ballPosition.x) * direction;

            float score =
                Mathf.Min(openness, 1.5f) * 1.8f +
                laneSafety * 2f +
                progress * 0.35f -
                distance * 0.25f;

            if (teammate.data.preferredPosition ==
                PlayerPosition.Defender)
                score += 0.4f;

            if (score > bestScore)
            {
                bestScore = score;
                bestTeammate = teammate;
                bestTarget = target;
            }
        }

        if (bestTeammate != null)
        {
            Vector2 difference =
                bestTarget - ballPosition;

            float speed = Mathf.Clamp(
                1.5f + difference.magnitude *
                ballRb.linearDamping,
                distributionSpeed,
                maximumDistributionSpeed);

            bestTeammate.SetTarget(
                bestTarget,
                "Receiving Goalkeeper Pass");

            ballController.StartPass(
                bestTeammate,
                bestTarget,
                difference.normalized * speed);

            successfulDistributions++;

            Debug.Log(
                keeper.data.playerName +
                " distributes to " +
                bestTeammate.data.playerName);
        }
        else
        {
            Vector2 clearanceTarget = new Vector2(
                direction * 0.8f,
                Random.Range(-0.8f, 0.8f));

            Vector2 difference =
                clearanceTarget - ballPosition;

            ballController.ReleaseBall(
                difference.normalized *
                maximumDistributionSpeed);

            Debug.Log(
                keeper.data.playerName +
                " clears the ball!");
        }

        keeper.SetTarget(
            keeper.homePosition,
            "Returning to Goal");
    }

    float EvaluateLane(
        PlayerController keeper,
        Vector2 start,
        Vector2 end)
    {
        Vector2 line = end - start;

        if (line.sqrMagnitude < 0.001f)
            return 0f;

        float safety = 1f;

        foreach (PlayerController opponent in players)
        {
            if (opponent == null ||
                opponent.isHomeTeam == keeper.isHomeTeam)
                continue;

            Vector2 position = opponent.transform.position;

            float t = Mathf.Clamp01(
                Vector2.Dot(position - start, line) /
                line.sqrMagnitude);

            Vector2 closestPoint = start + line * t;

            float distance = Vector2.Distance(
                position, closestPoint);

            safety = Mathf.Min(
                safety,
                Mathf.Clamp01(distance / 0.45f));
        }

        return safety;
    }
}