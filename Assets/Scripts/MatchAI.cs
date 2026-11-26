using UnityEngine;
using System.Collections.Generic;

public class MatchAI : MonoBehaviour
{
    [Header("Match References")]
    public Transform ball;

    [Header("Tactical Settings")]
    [Range(0f, 1f)]
    public float formationShift = 0.35f;

    public float decisionInterval = 0.12f;
    public float minimumPlayerDistance = 0.25f;

    [Header("Defensive Settings")]
    public float pressingDistance = 1.2f;
    public float coverDistance = 0.4f;
    public float defensiveCompactness = 0.75f;

    [Header("Intelligent Defensive Marking")]
    public bool enableDefensiveMarking = true;
    public float markingDistance = 0.28f;
    public float markingSearchRadius = 1.45f;
    public float maximumMarkingDeviation = 0.85f;
    public float markingPersistence = 0.45f;

    [Range(0f, 1f)]
    public float midfieldLaneBlocking = 0.45f;

    [Header("Attacking Settings")]
    public float dribbleForwardDistance = 0.6f;
    public float dribbleWidth = 0.65f;
    public float opponentAvoidance = 0.35f;

    [Header("Off-Ball Movement")]
    public float supportDistance = 0.85f;
    public float attackingRunDistance = 0.65f;
    public float spaceSearchRadius = 0.45f;
    public float preferredTeammateSpacing = 0.55f;
    public float targetChangeThreshold = 0.12f;

    [Header("Goalkeeper Possession")]
    public float goalkeeperRetreatDistance = 1.15f;
    public float goalkeeperSupportDistance = 0.85f;
    public float goalkeeperSupportWidth = 1.1f;

    [Header("Pitch Settings")]
    public float pitchHalfWidth = 3.15f;
    public float pitchHalfHeight = 1.8f;

    private PlayerController[] players;
    private BallController ballController;
    private float decisionTimer;

    private readonly Dictionary<PlayerController, PlayerController>
        marks = new Dictionary<PlayerController, PlayerController>();

    void Start()
    {
        if (ball != null)
            ballController = ball.GetComponent<BallController>();
    }

    void Update()
    {
        if (ball == null ||
            ballController == null ||
            Time.deltaTime <= 0f)
            return;

        if (players == null || players.Length == 0)
            players = FindObjectsByType<PlayerController>();

        decisionTimer += Time.deltaTime;

        if (decisionTimer >= decisionInterval)
        {
            UpdateTeamAI(true);
            UpdateTeamAI(false);

            decisionTimer = 0f;
        }
    }

    void UpdateTeamAI(bool isHome)
    {
        PlayerController owner = ballController.owner;

        bool teamHasBall =
            owner != null &&
            owner.isHomeTeam == isHome;

        bool goalkeeperHasBall =
            owner != null &&
            owner.data != null &&
            owner.data.preferredPosition ==
            PlayerPosition.Goalkeeper;

        bool teamReceivingPass =
            ballController.passInProgress &&
            ballController.intendedReceiver != null &&
            ballController.intendedReceiver.isHomeTeam == isHome;

        bool attacking =
            teamHasBall || teamReceivingPass;

        PlayerController firstPresser = null;
        PlayerController secondPresser = null;

        float firstDistance = Mathf.Infinity;
        float secondDistance = Mathf.Infinity;

        Vector2 ballPosition = ball.position;

        foreach (PlayerController player in players)
        {
            if (player == null ||
                player.data == null ||
                player.isHomeTeam != isHome ||
                player == owner)
                continue;

            if (player.data.preferredPosition ==
                PlayerPosition.Goalkeeper)
                continue;

            float distance = Vector2.Distance(
                player.transform.position,
                ballPosition);

            if (distance < firstDistance)
            {
                secondPresser = firstPresser;
                secondDistance = firstDistance;

                firstPresser = player;
                firstDistance = distance;
            }
            else if (distance < secondDistance)
            {
                secondPresser = player;
                secondDistance = distance;
            }
        }

        UpdateMarkingAssignments(
            isHome,
            owner,
            firstPresser,
            secondPresser,
            secondDistance,
            ballPosition,
            !attacking &&
            !goalkeeperHasBall &&
            enableDefensiveMarking);

        foreach (PlayerController player in players)
        {
            if (player == null ||
                player.data == null ||
                player.isHomeTeam != isHome)
                continue;

            // GoalkeeperAI controls goalkeeper movement.
            if (player.data.preferredPosition ==
                PlayerPosition.Goalkeeper)
                continue;

            Vector2 target;
            string action;

            float attackDirection = isHome ? 1f : -1f;

            // PRIORITY 1: Receive incoming passes.
            if (ballController.passInProgress &&
                player == ballController.intendedReceiver)
            {
                player.SetTarget(
                    ClampToPitch(
                        ballController.GetReceptionTarget()),
                    "Receiving Pass");

                continue;
            }

            // PRIORITY 2: Goalkeeper possession.
            if (goalkeeperHasBall)
            {
                if (teamHasBall)
                {
                    target = CalculateKeeperSupport(
                        player, owner);

                    action = "Supporting Goalkeeper";
                }
                else
                {
                    target = CalculateKeeperRetreat(
                        player, owner);

                    action = "Retreating From Goalkeeper";
                }

                target += CalculateSeparation(player);

                if (!teamHasBall)
                {
                    float keeperX =
                        owner.transform.position.x;

                    float boundary =
                        keeperX -
                        attackDirection *
                        goalkeeperRetreatDistance;

                    if (isHome)
                        target.x = Mathf.Min(
                            target.x, boundary);
                    else
                        target.x = Mathf.Max(
                            target.x, boundary);
                }

                player.SetTarget(
                    ClampToPitch(target), action);

                continue;
            }

            // PRIORITY 3: Ball carrier.
            if (player == owner)
            {
                target = CalculateDribbleTarget(player);
                action = "Dribbling";
            }

            // PRIORITY 4: Primary defensive pressure.
            else if (!attacking &&
                     player == firstPresser)
            {
                target = owner != null
                    ? (Vector2)owner.transform.position
                    : ballPosition;

                if (owner == null)
                {
                    target +=
                        ballController.BallVelocity * 0.12f;
                }

                action = "Pressing Ball";
            }

            // PRIORITY 5: Secondary defensive cover.
            else if (!attacking &&
                     owner != null &&
                     player == secondPresser &&
                     secondDistance < pressingDistance)
            {
                target = owner.transform.position;

                target.x -=
                    attackDirection * coverDistance;

                action = "Providing Defensive Cover";
            }

            // PRIORITY 6: Attacking movement.
            else if (attacking)
            {
                Vector2 attackAnchor = teamReceivingPass
                    ? ballController.passTarget
                    : ballPosition;

                target = CalculateAttackingTarget(
                    player, attackAnchor);

                switch (player.data.preferredPosition)
                {
                    case PlayerPosition.Defender:
                        action = "Providing Passing Outlet";
                        break;

                    case PlayerPosition.Midfielder:
                        action = "Finding Passing Space";
                        break;

                    case PlayerPosition.Forward:
                        action = "Making Attacking Run";
                        break;

                    default:
                        action = "Supporting Attack";
                        break;
                }
            }

            // PRIORITY 7: Intelligent defensive marking.
            else
            {
                target = DefensiveShape(
                    player, ballPosition);

                action = "Holding Defensive Shape";

                if (marks.TryGetValue(
                    player,
                    out PlayerController opponent) &&
                    opponent != null &&
                    opponent.data != null)
                {
                    target = CalculateMarkingTarget(
                        player,
                        opponent,
                        ballPosition,
                        target);

                    action =
                        "Marking " +
                        opponent.data.playerName;
                }
            }

            // Do not prevent the primary presser
            // from getting close enough to tackle.
            if (attacking || player != firstPresser)
            {
                target += CalculateSeparation(player);
            }

            target = ClampToPitch(target);

            bool urgentMovement =
                player == owner ||
                (!attacking && player == firstPresser);

            if (urgentMovement ||
                player.currentAction != action ||
                Vector2.Distance(
                    target,
                    player.targetPosition) >
                targetChangeThreshold)
            {
                player.SetTarget(target, action);
            }
        }
    }

    // ========================================
    // INTELLIGENT DEFENSIVE MARKING
    // ========================================

    void UpdateMarkingAssignments(
        bool isHome,
        PlayerController owner,
        PlayerController firstPresser,
        PlayerController secondPresser,
        float secondDistance,
        Vector2 ballPosition,
        bool markingActive)
    {
        Dictionary<PlayerController, PlayerController>
            previousMarks =
            new Dictionary<PlayerController, PlayerController>();

        List<PlayerController> removeList =
            new List<PlayerController>();

        foreach (var pair in marks)
        {
            if (pair.Key == null)
            {
                removeList.Add(pair.Key);
                continue;
            }

            if (pair.Key.isHomeTeam == isHome)
            {
                previousMarks[pair.Key] = pair.Value;
                removeList.Add(pair.Key);
            }
        }

        foreach (PlayerController player in removeList)
        {
            marks.Remove(player);
        }

        if (!markingActive)
            return;

        List<PlayerController> availableMarkers =
            new List<PlayerController>();

        List<PlayerController> availableOpponents =
            new List<PlayerController>();

        foreach (PlayerController player in players)
        {
            if (player == null || player.data == null)
                continue;

            if (player.data.preferredPosition ==
                PlayerPosition.Goalkeeper)
                continue;

            if (player.isHomeTeam == isHome)
            {
                if (player == firstPresser)
                    continue;

                if (owner != null &&
                    player == secondPresser &&
                    secondDistance < pressingDistance)
                    continue;

                if (player.data.preferredPosition ==
                    PlayerPosition.Defender ||
                    player.data.preferredPosition ==
                    PlayerPosition.Midfielder)
                {
                    availableMarkers.Add(player);
                }
            }
            else
            {
                // The primary presser handles the ball carrier.
                if (player == owner)
                    continue;

                availableOpponents.Add(player);
            }
        }

        // Assign the highest-value defender/opponent
        // combination first, then remove both from
        // the available lists.
        while (availableMarkers.Count > 0 &&
               availableOpponents.Count > 0)
        {
            PlayerController bestMarker = null;
            PlayerController bestOpponent = null;

            float bestScore = float.NegativeInfinity;

            foreach (PlayerController marker
                     in availableMarkers)
            {
                Vector2 defensivePosition =
                    DefensiveShape(marker, ballPosition);

                foreach (PlayerController opponent
                         in availableOpponents)
                {
                    Vector2 opponentPosition =
                        opponent.transform.position;

                    float distance = Vector2.Distance(
                        defensivePosition,
                        opponentPosition);

                    if (distance > markingSearchRadius)
                        continue;

                    float score = EvaluateMarkingAssignment(
                        marker,
                        opponent,
                        defensivePosition,
                        ballPosition);

                    if (previousMarks.TryGetValue(
                        marker,
                        out PlayerController previousOpponent) &&
                        previousOpponent == opponent)
                    {
                        score += markingPersistence;
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMarker = marker;
                        bestOpponent = opponent;
                    }
                }
            }

            if (bestMarker == null ||
                bestOpponent == null ||
                bestScore < 0.15f)
                break;

            marks[bestMarker] = bestOpponent;

            availableMarkers.Remove(bestMarker);
            availableOpponents.Remove(bestOpponent);
        }
    }

    float EvaluateMarkingAssignment(
        PlayerController marker,
        PlayerController opponent,
        Vector2 defensivePosition,
        Vector2 ballPosition)
    {
        float direction =
            marker.isHomeTeam ? 1f : -1f;

        Vector2 ownGoal = new Vector2(
            -direction * pitchHalfWidth,
            0f);

        Vector2 opponentPosition =
            opponent.transform.position;

        float formationDistance =
            Vector2.Distance(
                defensivePosition,
                opponentPosition);

        float movementDistance =
            Vector2.Distance(
                marker.transform.position,
                opponentPosition);

        float distanceToGoal =
            Vector2.Distance(
                opponentPosition,
                ownGoal);

        float distanceToBall =
            Vector2.Distance(
                opponentPosition,
                ballPosition);

        float goalThreat =
            1f - Mathf.Clamp01(
                distanceToGoal /
                (pitchHalfWidth * 2f));

        float ballThreat =
            1f - Mathf.Clamp01(
                distanceToBall / 3f);

        float defensiveAbility =
            Mathf.Clamp01(
                marker.data.defending / 100f);

        float positioningAbility =
            Mathf.Clamp01(
                marker.data.positioning / 100f);

        float decisionAbility =
            Mathf.Clamp01(
                marker.data.decisionMaking / 100f);

        float opponentThreat =
            Mathf.Clamp01(
                opponent.data.shooting / 100f);

        float score =
            goalThreat * 2f +
            ballThreat * 1.3f +
            opponentThreat * 0.4f +
            defensiveAbility * 0.3f +
            positioningAbility * 0.4f +
            decisionAbility * 0.3f -
            formationDistance * 1.1f -
            movementDistance * 0.3f;

        // Defenders prioritize dangerous forwards.
        if (marker.data.preferredPosition ==
            PlayerPosition.Defender)
        {
            if (opponent.data.preferredPosition ==
                PlayerPosition.Forward)
            {
                score += 0.65f;
            }

            if (opponent.data.preferredPosition ==
                PlayerPosition.Defender)
            {
                score -= 0.5f;
            }
        }

        // Midfielders prioritize opposing midfielders.
        if (marker.data.preferredPosition ==
            PlayerPosition.Midfielder)
        {
            if (opponent.data.preferredPosition ==
                PlayerPosition.Midfielder)
            {
                score += 0.45f;
            }
        }

        // Encourage players to defend their own side
        // of the pitch instead of constantly crossing.
        if (Mathf.Abs(marker.homePosition.y) > 0.2f &&
            Mathf.Abs(opponentPosition.y) > 0.2f &&
            Mathf.Sign(marker.homePosition.y) !=
            Mathf.Sign(opponentPosition.y))
        {
            score -= 0.35f;
        }

        return score;
    }

    Vector2 DefensiveShape(
        PlayerController player,
        Vector2 ballPosition)
    {
        float direction =
            player.isHomeTeam ? 1f : -1f;

        Vector2 target =
            player.homePosition +
            ballPosition * formationShift;

        target.y = Mathf.Lerp(
            ballPosition.y,
            target.y,
            defensiveCompactness);

        target.x -= direction * 0.2f;

        return ClampToPitch(target);
    }

    Vector2 CalculateMarkingTarget(
        PlayerController marker,
        PlayerController opponent,
        Vector2 ballPosition,
        Vector2 defensivePosition)
    {
        float direction =
            marker.isHomeTeam ? 1f : -1f;

        Vector2 ownGoal = new Vector2(
            -direction * pitchHalfWidth,
            0f);

        float defensiveAbility =
            Mathf.Clamp01(
                marker.data.defending / 100f);

        float positioningAbility =
            Mathf.Clamp01(
                marker.data.positioning / 100f);

        float decisionAbility =
            Mathf.Clamp01(
                marker.data.decisionMaking / 100f);

        // Better defenders anticipate opponent movement.
        float predictionTime = Mathf.Lerp(
            0.05f,
            0.35f,
            decisionAbility);

        Vector2 opponentPosition =
            opponent.PredictPosition(predictionTime);

        Vector2 goalDirection =
            (ownGoal - opponentPosition).normalized;

        float adjustedMarkingDistance =
            markingDistance *
            Mathf.Lerp(
                1.3f,
                0.85f,
                defensiveAbility);

        // Stay between the opponent and our own goal.
        Vector2 goalSideTarget =
            opponentPosition +
            goalDirection * adjustedMarkingDistance;

        Vector2 target = goalSideTarget;

        // Midfielders also try to block passing lanes.
        if (marker.data.preferredPosition ==
            PlayerPosition.Midfielder)
        {
            Vector2 passingLaneTarget =
                Vector2.Lerp(
                    opponentPosition,
                    ballPosition,
                    0.25f);

            float laneWeight =
                midfieldLaneBlocking *
                Mathf.Lerp(
                    0.5f,
                    1f,
                    positioningAbility);

            target = Vector2.Lerp(
                goalSideTarget,
                passingLaneTarget,
                laneWeight);
        }

        // Limit how far a defender can abandon formation.
        float allowedDeviation =
            maximumMarkingDeviation *
            Mathf.Lerp(
                0.8f,
                1.15f,
                positioningAbility);

        Vector2 deviation =
            target - defensivePosition;

        if (deviation.magnitude > allowedDeviation)
        {
            target =
                defensivePosition +
                deviation.normalized * allowedDeviation;
        }

        return ClampToPitch(target);
    }

    // ========================================
    // GOALKEEPER POSSESSION
    // ========================================

    Vector2 CalculateKeeperSupport(
        PlayerController player,
        PlayerController keeper)
    {
        float direction =
            player.isHomeTeam ? 1f : -1f;

        Vector2 keeperPosition =
            keeper.transform.position;

        Vector2 target = player.homePosition;

        switch (player.data.preferredPosition)
        {
            case PlayerPosition.Defender:

                target.x =
                    keeperPosition.x +
                    direction * goalkeeperSupportDistance;

                target.y = Mathf.Clamp(
                    player.homePosition.y * 1.15f,
                    -goalkeeperSupportWidth,
                    goalkeeperSupportWidth);

                break;

            case PlayerPosition.Midfielder:

                target.x =
                    keeperPosition.x +
                    direction * 1.8f;

                target.y = player.homePosition.y;

                break;

            case PlayerPosition.Forward:

                target.x += direction * 0.2f;
                target.y = player.homePosition.y;

                break;
        }

        return ClampToPitch(target);
    }

    Vector2 CalculateKeeperRetreat(
        PlayerController player,
        PlayerController keeper)
    {
        float direction =
            player.isHomeTeam ? 1f : -1f;

        Vector2 target = player.homePosition;

        target.x += direction * 0.2f;

        float retreatBoundary =
            keeper.transform.position.x -
            direction * goalkeeperRetreatDistance;

        if (player.isHomeTeam)
        {
            target.x = Mathf.Min(
                target.x, retreatBoundary);
        }
        else
        {
            target.x = Mathf.Max(
                target.x, retreatBoundary);
        }

        return ClampToPitch(target);
    }

    // ========================================
    // ATTACKING MOVEMENT
    // ========================================

    Vector2 CalculateAttackingTarget(
        PlayerController player,
        Vector2 ballPosition)
    {
        float direction =
            player.isHomeTeam ? 1f : -1f;

        Vector2 baseTarget =
            player.homePosition +
            ballPosition * formationShift;

        float side =
            player.homePosition.y >= 0f ? 1f : -1f;

        float forwardPreference = 0.3f;

        switch (player.data.preferredPosition)
        {
            case PlayerPosition.Defender:

                baseTarget.x += direction * 0.2f;

                if ((baseTarget.x - ballPosition.x) *
                    direction > -0.35f)
                {
                    baseTarget.x =
                        ballPosition.x -
                        direction * 0.35f;
                }

                forwardPreference = 0.1f;
                break;

            case PlayerPosition.Midfielder:

                Vector2 supportTarget =
                    ballPosition +
                    new Vector2(
                        direction * 0.3f,
                        side * supportDistance);

                baseTarget = Vector2.Lerp(
                    baseTarget,
                    supportTarget,
                    0.5f);

                forwardPreference = 0.65f;
                break;

            case PlayerPosition.Forward:

                baseTarget.x +=
                    direction * attackingRunDistance;

                if ((baseTarget.x - ballPosition.x) *
                    direction < 0.3f)
                {
                    baseTarget.x =
                        ballPosition.x +
                        direction * attackingRunDistance;
                }

                baseTarget.y = Mathf.Lerp(
                    baseTarget.y,
                    player.homePosition.y,
                    0.35f);

                forwardPreference = 1.1f;
                break;
        }

        baseTarget = ClampToPitch(baseTarget);

        Vector2 bestTarget = baseTarget;

        float bestScore = EvaluateSpace(
            player,
            baseTarget,
            baseTarget,
            ballPosition,
            forwardPreference);

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2 candidate =
                    baseTarget +
                    new Vector2(x, y) *
                    spaceSearchRadius;

                candidate = ClampToPitch(candidate);

                float score = EvaluateSpace(
                    player,
                    candidate,
                    baseTarget,
                    ballPosition,
                    forwardPreference);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = candidate;
                }
            }
        }

        return bestTarget;
    }

    float EvaluateSpace(
        PlayerController player,
        Vector2 candidate,
        Vector2 baseTarget,
        Vector2 ballPosition,
        float forwardPreference)
    {
        float direction =
            player.isHomeTeam ? 1f : -1f;

        float nearestOpponent = 3f;
        float nearestTeammate = 3f;
        float laneSafety = 1f;

        Vector2 passLine =
            candidate - ballPosition;

        foreach (PlayerController other in players)
        {
            if (other == null || other == player)
                continue;

            Vector2 otherPosition =
                other.transform.position;

            float distance = Vector2.Distance(
                candidate,
                otherPosition);

            if (other.isHomeTeam == player.isHomeTeam)
            {
                nearestTeammate = Mathf.Min(
                    nearestTeammate,
                    distance);
            }
            else
            {
                nearestOpponent = Mathf.Min(
                    nearestOpponent,
                    distance);

                if (passLine.sqrMagnitude > 0.001f)
                {
                    float t = Mathf.Clamp01(
                        Vector2.Dot(
                            otherPosition - ballPosition,
                            passLine) /
                        passLine.sqrMagnitude);

                    if (t > 0.05f)
                    {
                        Vector2 closestPoint =
                            ballPosition + passLine * t;

                        float clearance =
                            Vector2.Distance(
                                otherPosition,
                                closestPoint);

                        laneSafety = Mathf.Min(
                            laneSafety,
                            Mathf.Clamp01(
                                clearance / 0.4f));
                    }
                }
            }
        }

        float openness =
            Mathf.Min(nearestOpponent, 1.2f);

        float spacingPenalty = Mathf.Max(
            0f,
            preferredTeammateSpacing -
            nearestTeammate);

        float forwardProgress =
            (candidate.x - baseTarget.x) *
            direction;

        float distanceFromBall =
            Vector2.Distance(
                candidate,
                ballPosition);

        float distanceFromFormation =
            Vector2.Distance(
                candidate,
                baseTarget);

        float movementCost =
            Vector2.Distance(
                candidate,
                player.transform.position);

        float score =
            openness * 1.6f +
            laneSafety * 1.4f +
            forwardProgress * forwardPreference -
            spacingPenalty * 3f -
            distanceFromFormation * 0.65f -
            movementCost * 0.2f;

        if (distanceFromBall < 0.4f)
            score -= 1.5f;

        if (distanceFromBall > 2.8f)
            score -= distanceFromBall - 2.8f;

        return score;
    }

    // ========================================
    // DRIBBLING
    // ========================================

    Vector2 CalculateDribbleTarget(
        PlayerController player)
    {
        Vector2 position =
            player.transform.position;

        float direction =
            player.isHomeTeam ? 1f : -1f;

        Vector2 target =
            position +
            Vector2.right *
            direction *
            dribbleForwardDistance;

        float avoidanceY = 0f;

        foreach (PlayerController opponent in players)
        {
            if (opponent == null ||
                opponent == player ||
                opponent.isHomeTeam == player.isHomeTeam)
                continue;

            Vector2 difference =
                (Vector2)opponent.transform.position -
                position;

            if (difference.x * direction < 0f)
                continue;

            float distance = difference.magnitude;

            if (distance < 0.65f)
            {
                float side =
                    difference.y >= 0f ? -1f : 1f;

                avoidanceY +=
                    side *
                    opponentAvoidance *
                    (1f - distance / 0.65f);
            }
        }

        target.y += Mathf.Clamp(
            avoidanceY,
            -dribbleWidth,
            dribbleWidth);

        if (Mathf.Abs(position.x) > 1.9f)
        {
            target.y = Mathf.Lerp(
                target.y,
                0f,
                0.25f);
        }

        return target;
    }

    // ========================================
    // PLAYER SEPARATION
    // ========================================

    Vector2 CalculateSeparation(
        PlayerController player)
    {
        Vector2 separation = Vector2.zero;

        foreach (PlayerController other in players)
        {
            if (other == null || other == player)
                continue;

            Vector2 difference =
                (Vector2)player.transform.position -
                (Vector2)other.transform.position;

            float distance = difference.magnitude;

            if (distance < minimumPlayerDistance)
            {
                Vector2 direction;

                if (distance > 0.001f)
                {
                    direction = difference.normalized;
                }
                else
                {
                    int playerIndex =
                        player.transform.GetSiblingIndex();

                    int otherIndex =
                        other.transform.GetSiblingIndex();

                    bool moveUp =
                        playerIndex > otherIndex ||
                        (playerIndex == otherIndex &&
                         player.isHomeTeam);

                    direction = moveUp
                        ? Vector2.up
                        : Vector2.down;
                }

                separation += direction *
                    (minimumPlayerDistance - distance);
            }
        }

        return separation;
    }

    Vector2 ClampToPitch(Vector2 target)
    {
        target.x = Mathf.Clamp(
            target.x,
            -pitchHalfWidth + 0.1f,
            pitchHalfWidth - 0.1f);

        target.y = Mathf.Clamp(
            target.y,
            -pitchHalfHeight + 0.1f,
            pitchHalfHeight - 0.1f);

        return target;
    }
}