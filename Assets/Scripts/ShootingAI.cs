using UnityEngine;

public class ShootingAI : MonoBehaviour
{
    [Header("References")]
    public BallController ballController;
    public MatchRules matchRules;

    [Header("Shooting Settings")]
    public float shootingRange = 1.5f;
    public float longShotRange = 2.3f;
    public float minimumShotSpeed = 4.5f;
    public float maximumShotSpeed = 8f;
    public float goalX = 3.2f;
    public float goalHalfWidth = 0.25f;
    public float shotCooldown = 1.2f;
    public float decisionInterval = 0.25f;

    [Header("Accuracy Settings")]
    public float minimumShotError = 0.04f;
    public float maximumShotError = 0.4f;
    public float pressureRadius = 0.5f;
    public float pressurePenalty = 0.15f;
    public float longShotPenalty = 0.2f;
    public float anglePenalty = 0.15f;

    [Header("Decision Settings")]
    [Range(0f, 1f)]
    public float baseShotChance = 0.65f;

    [Range(0f, 1f)]
    public float longShotChance = 0.12f;

    [Header("Match Statistics")]
    public int homeShots;
    public int awayShots;
    public int homeShotsOnTarget;
    public int awayShotsOnTarget;
    public int longShots;

    private float cooldownTimer;
    private float decisionTimer;

    void Start()
    {
        if (matchRules == null)
            matchRules = FindFirstObjectByType<MatchRules>();

        homeShots = 0;
        awayShots = 0;
        homeShotsOnTarget = 0;
        awayShotsOnTarget = 0;
        longShots = 0;
    }

    void Update()
    {
        if (matchRules != null &&
            (matchRules.isRestarting ||
             matchRules.isHalfTime ||
             matchRules.isFullTime))
            return;

        cooldownTimer -= Time.deltaTime;
        decisionTimer -= Time.deltaTime;

        if (ballController == null ||
            ballController.owner == null ||
            cooldownTimer > 0f ||
            decisionTimer > 0f)
            return;

        decisionTimer = decisionInterval;

        PlayerController player = ballController.owner;

        if (player.data == null)
            return;

        if (player.data.preferredPosition ==
            PlayerPosition.Goalkeeper)
            return;

        EvaluateShot(player);
    }

    void EvaluateShot(PlayerController player)
    {
        float attackDirection =
            player.isHomeTeam ? 1f : -1f;

        Vector2 goalPosition = new Vector2(
            attackDirection * goalX, 0f);

        Vector2 playerPosition =
            player.transform.position;

        float distanceToGoal = Vector2.Distance(
            playerPosition, goalPosition);

        float shootingSkill = Mathf.Clamp01(
            player.data.shooting / 100f);

        float decisionSkill = Mathf.Clamp01(
            player.data.decisionMaking / 100f);

        float effectiveRange = Mathf.Lerp(
            shootingRange * 0.7f,
            shootingRange,
            shootingSkill);

        if (distanceToGoal > longShotRange)
            return;

        Vector2 forward = new Vector2(
            attackDirection, 0f);

        Vector2 toGoal =
            (goalPosition - playerPosition).normalized;

        float angle = Vector2.Angle(forward, toGoal);

        if (angle > 75f)
            return;

        float pressure = CalculatePressure(player);

        bool isLongShot =
            distanceToGoal > effectiveRange;

        float shotChance;

        if (isLongShot)
        {
            if (shootingSkill < 0.65f)
                return;

            shotChance = longShotChance *
                shootingSkill *
                (1f - pressure);
        }
        else
        {
            float distanceFactor = 1f -
                Mathf.Clamp01(
                    distanceToGoal / effectiveRange);

            shotChance = baseShotChance;

            shotChance += distanceFactor * 0.25f;
            shotChance += shootingSkill * 0.15f;
            shotChance += decisionSkill * 0.1f;

            shotChance -= pressure * 0.3f;
            shotChance -= (angle / 90f) * 0.25f;
        }

        switch (player.data.preferredPosition)
        {
            case PlayerPosition.Forward:
                shotChance *= 1.25f;
                break;

            case PlayerPosition.Midfielder:
                shotChance *= 0.9f;
                break;

            case PlayerPosition.Defender:
                shotChance *= 0.55f;
                break;
        }

        // Encourage shooting from excellent positions.
        if (distanceToGoal < 0.55f && angle < 45f)
            shotChance = Mathf.Max(shotChance, 0.85f);

        shotChance = Mathf.Clamp01(shotChance);

        if (Random.value <= shotChance)
        {
            Shoot(
                player,
                goalPosition,
                distanceToGoal,
                angle,
                pressure,
                isLongShot);
        }
    }

    float CalculatePressure(PlayerController player)
    {
        PlayerController[] players =
            FindObjectsByType<PlayerController>(
                FindObjectsSortMode.None);

        float pressure = 0f;

        foreach (PlayerController opponent in players)
        {
            if (opponent == player ||
                opponent.isHomeTeam == player.isHomeTeam)
                continue;

            float distance = Vector2.Distance(
                player.transform.position,
                opponent.transform.position);

            if (distance < pressureRadius)
            {
                pressure += 1f -
                    distance / pressureRadius;
            }
        }

        return Mathf.Clamp01(pressure);
    }

    void Shoot(
        PlayerController player,
        Vector2 goalPosition,
        float distance,
        float angle,
        float pressure,
        bool isLongShot)
    {
        float shootingSkill = Mathf.Clamp01(
            player.data.shooting / 100f);

        float strength = Mathf.Clamp01(
            player.data.strength / 100f);

        // Better players have less shooting error.
        float error = Mathf.Lerp(
            maximumShotError,
            minimumShotError,
            shootingSkill);

        error += pressure * pressurePenalty;

        error +=
            Mathf.Clamp01(angle / 90f) * anglePenalty;

        if (isLongShot)
        {
            error += longShotPenalty;
            longShots++;
        }

        // Aim toward either side of the goal.
        float targetY = Random.Range(
            -goalHalfWidth * 0.75f,
             goalHalfWidth * 0.75f);

        targetY += Random.Range(-error, error);

        goalPosition.y = targetY;

        Vector2 ballPosition =
            ballController.transform.position;

        Vector2 direction =
            (goalPosition - ballPosition).normalized;

        float shotSpeed = Mathf.Lerp(
            minimumShotSpeed,
            maximumShotSpeed,
            strength);

        player.currentAction = isLongShot
            ? "Long Shot"
            : "Shooting";

        RecordShot(player, direction, ballPosition);

        ballController.ReleaseBall(
            direction * shotSpeed);

        cooldownTimer = shotCooldown;

        Debug.Log(
            player.data.playerName +
            (isLongShot ? " takes a long shot!" : " shoots!") +
            " | Distance: " + distance.ToString("F2") +
            " | Pressure: " + pressure.ToString("F2"));
    }

    void RecordShot(
        PlayerController player,
        Vector2 direction,
        Vector2 ballPosition)
    {
        if (player.isHomeTeam)
            homeShots++;
        else
            awayShots++;

        if (Mathf.Abs(direction.x) < 0.001f)
            return;

        float targetX = player.isHomeTeam
            ? goalX
            : -goalX;

        if (matchRules != null)
            targetX = player.isHomeTeam
                ? matchRules.goalLineX
                : -matchRules.goalLineX;

        float travel =
            (targetX - ballPosition.x) / direction.x;

        if (travel <= 0f)
            return;

        float projectedY =
            ballPosition.y + direction.y * travel;

        float targetHalfWidth = matchRules != null
            ? matchRules.goalHalfWidth
            : goalHalfWidth;

        bool onTarget =
            Mathf.Abs(projectedY) <= targetHalfWidth;

        if (onTarget)
        {
            if (player.isHomeTeam)
                homeShotsOnTarget++;
            else
                awayShotsOnTarget++;
        }
    }
}