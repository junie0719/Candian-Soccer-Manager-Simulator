
using UnityEngine;

public class PassingAI : MonoBehaviour
{
    [Header("References")]
    public BallController ballController;

    [Header("Passing Settings")]
    public float minimumPassDistance = 0.35f;
    public float maximumPassDistance = 3.2f;
    public float passSpeed = 3.5f;
    public float maximumPassSpeed = 7f;
    public float passCooldown = 0.65f;
    public float decisionInterval = 0.25f;

    [Header("Tactical Settings")]
    public float pressureRadius = 0.65f;
    public float interceptionRadius = 0.22f;
    public float shootingRange = 1.1f;
    public float goalX = 3.2f;
    public float throughBallDistance = 0.45f;
    public float forwardPassBonus = 1.3f;

    [Header("Passing Errors")]
    public float minimumPassError = 0.025f;
    public float maximumPassError = 0.18f;
    public float pressureErrorMultiplier = 1.5f;

    [Range(0f, 1f)]
    public float majorMistakeChance = 0.025f;

    public float majorMistakeDistance = 0.4f;

    [Header("Statistics")]
    public int homePassAttempts;
    public int awayPassAttempts;
    public int passingMistakes;
    public int throughBalls;

    private PlayerController[] players;
    private Rigidbody2D ballRigidbody;

    private float cooldownTimer;
    private float decisionTimer;

    void Start()
    {
        if (ballController != null)
        {
            ballRigidbody =
                ballController.GetComponent<Rigidbody2D>();
        }
    }

    void Update()
    {
        if (ballController == null ||
            Time.deltaTime <= 0f)
            return;

        if (players == null || players.Length == 0)
        {
            players = FindObjectsByType<PlayerController>(
                FindObjectsSortMode.None);
        }

        cooldownTimer -= Time.deltaTime;
        decisionTimer += Time.deltaTime;

        if (decisionTimer < decisionInterval)
            return;

        decisionTimer = 0f;

        if (ballController.owner == null ||
            cooldownTimer > 0f)
            return;

        EvaluatePassingDecision(ballController.owner);
    }

    void EvaluatePassingDecision(PlayerController owner)
    {
        if (owner.data == null ||
            owner.assignedPosition ==
            PlayerPosition.Goalkeeper)
            return;

        float direction = owner.isHomeTeam ? 1f : -1f;

        Vector2 ownerPosition = owner.transform.position;

        Vector2 goal = new Vector2(
            direction * goalX, 0f);

        float distanceToGoal =
            Vector2.Distance(ownerPosition, goal);

        bool underPressure =
            GetNearestOpponentDistance(
                ownerPosition, owner.isHomeTeam)
            < pressureRadius;

        // Prefer shooting from excellent positions.
        if (distanceToGoal <= shootingRange &&
            !underPressure &&
            Mathf.Abs(ownerPosition.y) < 0.65f)
            return;

        PlayerController bestReceiver = null;
        Vector2 bestTarget = Vector2.zero;
        float bestScore = float.NegativeInfinity;
        bool bestThroughBall = false;

        foreach (PlayerController teammate in players)
        {
            if (teammate == null ||
                teammate == owner ||
                teammate.data == null ||
                teammate.isHomeTeam != owner.isHomeTeam)
                continue;

            if (teammate.assignedPosition ==
                PlayerPosition.Goalkeeper)
                continue;

            Vector2 teammatePosition =
                teammate.transform.position;

            float distance = Vector2.Distance(
                ownerPosition, teammatePosition);

            if (distance < minimumPassDistance ||
                distance > maximumPassDistance)
                continue;

            float arrivalTime =
                Mathf.Clamp(distance / 3f, 0.2f, 0.9f);

            Vector2 predictedPosition =
                teammate.PredictPosition(arrivalTime);

            Vector2[] candidates =
            {
                teammatePosition,
                predictedPosition,
                predictedPosition +
                    Vector2.right * direction *
                    throughBallDistance
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                Vector2 target = ClampToPitch(candidates[i]);

                float passDistance = Vector2.Distance(
                    ballController.transform.position,
                    target);

                if (passDistance < minimumPassDistance ||
                    passDistance > maximumPassDistance)
                    continue;

                float travelTime =
                    Mathf.Clamp(
                        passDistance / 3f, 0.2f, 1.1f);

                float receiverTravel =
                    Vector2.Distance(
                        teammatePosition, target);

                if (receiverTravel >
                    teammate.moveSpeed * travelTime + 0.18f)
                    continue;

                float laneSafety =
                    EvaluatePassLane(
                        owner, target, travelTime);

                if (laneSafety < 0.12f)
                    continue;

                float openness =
                    GetNearestOpponentDistance(
                        target, owner.isHomeTeam);

                float progress =
                    (target.x - ownerPosition.x) *
                    direction;

                float technicalQuality =
                    teammate.data.dribbling / 100f;

                float score =
                    progress * forwardPassBonus +
                    Mathf.Min(openness, 1.2f) * 1.4f +
                    laneSafety * 1.8f +
                    technicalQuality * 0.25f -
                    passDistance * 0.25f;

                if (i == 2 && progress > 0f)
                    score += 0.25f;

                if (underPressure &&
                    progress < 0f)
                    score += 0.3f;

                // Avoid immediate return passes.
                if (teammate ==
                    ballController.lastTouchPlayer)
                {
                    score -= 0.2f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestReceiver = teammate;
                    bestTarget = target;
                    bestThroughBall = i == 2;
                }
            }
        }

        if (bestReceiver == null)
            return;

        float decisionQuality =
            Mathf.Clamp01(
                owner.data.decisionMaking / 100f);

        float passChance = underPressure
            ? 0.9f
            : 0.45f;

        passChance *= Mathf.Lerp(
            0.8f, 1.2f, decisionQuality);

        float minimumScore = underPressure
            ? 0.5f
            : 1.4f;

        if (bestScore >= minimumScore &&
            Random.value < Mathf.Clamp01(passChance))
        {
            ExecutePass(
                owner,
                bestReceiver,
                bestTarget,
                underPressure,
                bestThroughBall);
        }
    }

    float EvaluatePassLane(
        PlayerController passer,
        Vector2 target,
        float travelTime)
    {
        Vector2 start =
            ballController.transform.position;

        Vector2 line = target - start;

        if (line.sqrMagnitude < 0.001f)
            return 0f;

        float safety = 1f;

        foreach (PlayerController opponent in players)
        {
            if (opponent == null ||
                opponent.isHomeTeam ==
                passer.isHomeTeam)
                continue;

            Vector2 opponentPosition =
                opponent.transform.position;

            float t = Mathf.Clamp01(
                Vector2.Dot(
                    opponentPosition - start, line) /
                line.sqrMagnitude);

            if (t < 0.05f)
                continue;

            Vector2 closestPoint =
                start + line * t;

            float distance = Vector2.Distance(
                opponentPosition, closestPoint);

            float dangerRadius =
                interceptionRadius +
                opponent.moveSpeed *
                travelTime * t * 0.3f;

            float clearance = Mathf.Clamp01(
                distance /
                Mathf.Max(dangerRadius * 2f, 0.01f));

            safety = Mathf.Min(safety, clearance);
        }

        return safety;
    }

    float GetNearestOpponentDistance(
        Vector2 position,
        bool isHomeTeam)
    {
        float nearest = 5f;

        foreach (PlayerController opponent in players)
        {
            if (opponent == null ||
                opponent.isHomeTeam == isHomeTeam)
                continue;

            nearest = Mathf.Min(
                nearest,
                Vector2.Distance(
                    position,
                    opponent.transform.position));
        }

        return nearest;
    }

    void ExecutePass(
        PlayerController passer,
        PlayerController receiver,
        Vector2 target,
        bool underPressure,
        bool isThroughBall)
    {
        float accuracy = Mathf.Clamp01(
            passer.data.passing / 100f);

        float error = Mathf.Lerp(
            maximumPassError,
            minimumPassError,
            accuracy);

        if (underPressure)
            error *= pressureErrorMultiplier;

        target += Random.insideUnitCircle * error;

        float mistakeChance =
            majorMistakeChance *
            Mathf.Lerp(1.5f, 0.5f, accuracy);

        if (underPressure)
            mistakeChance *= 1.5f;

        if (Random.value < mistakeChance)
        {
            target += Random.insideUnitCircle *
                majorMistakeDistance;

            passingMistakes++;
        }

        target = ClampToPitch(target);

        Vector2 difference =
            target -
            (Vector2)ballController.transform.position;

        float distance = difference.magnitude;

        if (distance < 0.05f)
            return;

        float damping = ballRigidbody != null
            ? ballRigidbody.linearDamping
            : 1.5f;

        // Compensate for ball slowing due to damping.
        float speed = Mathf.Clamp(
            1.5f + distance * damping,
            passSpeed,
            maximumPassSpeed);

        passer.currentAction = isThroughBall
            ? "Through Ball"
            : "Passing";

        receiver.SetTarget(target, "Receiving Pass");

        ballController.StartPass(
            receiver,
            target,
            difference.normalized * speed);

        if (passer.isHomeTeam)
            homePassAttempts++;
        else
            awayPassAttempts++;

        if (isThroughBall)
            throughBalls++;

        cooldownTimer = passCooldown;

        Debug.Log(
            passer.data.playerName +
            " passes to " +
            receiver.data.playerName +
            (isThroughBall ? " (Through Ball)" : ""));
    }

    Vector2 ClampToPitch(Vector2 position)
    {
        position.x = Mathf.Clamp(position.x, -3.0f, 3.0f);
        position.y = Mathf.Clamp(position.y, -1.65f, 1.65f);

        return position;
    }
}
