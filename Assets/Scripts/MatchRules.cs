
using UnityEngine;
using System.Collections;

public class MatchRules : MonoBehaviour
{
    [Header("References")]
    public BallController ballController;

    [Header("Score")]
    public int homeScore;
    public int awayScore;

    [Header("Pitch Boundaries")]
    public float goalLineX = 3.15f;
    public float goalHalfWidth = 0.25f;
    public float touchLineY = 1.8f;

    [Header("Restart Settings")]
    public float restartPassSpeed = 3.5f;
    public float kickoffDelay = 2f;
    public float kickoffPassSpeed = 2.8f;
    public float centreCircleRadius = 0.55f;

    [Header("Set Piece Timing")]
    public float throwInDelay = 3f;
    public float cornerDelay = 4f;
    public float goalKickDelay = 3f;
    public float throwInSpeed = 2.5f;

    [Header("Match State")]
    public bool isRestarting;
    public bool isHalfTime;
    public bool isFullTime;
    public bool firstKickoffHome = true;
    public string restartLabel = "KICKOFF";

    [Header("Match Statistics")]
    public int homeThrowIns;
    public int awayThrowIns;
    public int homeCorners;
    public int awayCorners;
    public int homeGoalKicks;
    public int awayGoalKicks;

    private PlayerController[] players;
    private MonoBehaviour[] aiSystems;
    private Rigidbody2D ballRb;
    private Coroutine restartRoutine;

    IEnumerator Start()
    {
        aiSystems = new MonoBehaviour[]
        {
            GetComponent<MatchAI>(),
            GetComponent<PassingAI>(),
            GetComponent<ShootingAI>(),
            GetComponent<GoalkeeperAI>(),
            ballController
        };

        ballRb =
            ballController.GetComponent<Rigidbody2D>();

        isRestarting = true;

        SetAIEnabled(false);

        yield return null;

        players = FindObjectsByType<PlayerController>(
            FindObjectsSortMode.None);

        BeginKickoff(firstKickoffHome);
    }

    void Update()
    {
        if (ballController == null ||
            isRestarting ||
            isHalfTime ||
            isFullTime)
            return;

        Vector2 position =
            ballController.transform.position;

        if (position.x >= goalLineX &&
            Mathf.Abs(position.y) <= goalHalfWidth)
        {
            homeScore++;

            Debug.Log("GOAL! Edmonton FC");

            BeginKickoff(false);
            return;
        }

        if (position.x <= -goalLineX &&
            Mathf.Abs(position.y) <= goalHalfWidth)
        {
            awayScore++;

            Debug.Log("GOAL! Calgary FC");

            BeginKickoff(true);
            return;
        }

        if (Mathf.Abs(position.y) > touchLineY)
        {
            HandleThrowIn(position);
            return;
        }

        if (Mathf.Abs(position.x) > goalLineX)
        {
            HandleGoalLineRestart(position);
        }
    }

    public void BeginKickoff(bool homeTeam)
    {
        if (isFullTime)
            return;

        CancelRestart();

        restartRoutine =
            StartCoroutine(KickoffSequence(homeTeam));
    }

    IEnumerator KickoffSequence(bool homeTeam)
    {
        isRestarting = true;
        restartLabel = "KICKOFF";

        SetAIEnabled(false);

        FreezeBall(Vector2.zero);

        PlayerController taker =
            ResetFormation(homeTeam);

        Debug.Log(
            "KICKOFF: " + GetTeamName(homeTeam));

        yield return new WaitForSeconds(kickoffDelay);

        if (taker != null)
        {
            PlayerController receiver =
                FindKickoffReceiver(taker);

            Vector2 target = receiver != null
                ? (Vector2)receiver.transform.position
                : new Vector2(
                    homeTeam ? -1f : 1f, 0f);

            Vector2 direction = target.normalized;

            float speed = Mathf.Max(
                kickoffPassSpeed,
                target.magnitude * 1.5f + 0.5f);

            ballController.RegisterTouch(taker);

            ballController.ReleaseBall(
                direction * speed);

            taker.currentAction = "Taking Kickoff";

            if (receiver != null)
            {
                receiver.SetTarget(
                    target,
                    "Receiving Kickoff");
            }
        }
        else
        {
            ballController.ReleaseBall(Vector2.zero);
        }

        ResumePlay();
    }

    PlayerController ResetFormation(bool homeKickoff)
    {
        PlayerController taker = null;
        float closest = Mathf.Infinity;

        foreach (PlayerController player in players)
        {
            if (player.data == null)
                continue;

            Vector2 position = player.homePosition;

            if (player.isHomeTeam)
                position.x = Mathf.Min(position.x, -0.1f);
            else
                position.x = Mathf.Max(position.x, 0.1f);

            if (player.isHomeTeam != homeKickoff &&
                position.magnitude < centreCircleRadius)
            {
                position = position.normalized *
                    centreCircleRadius;
            }

            PlacePlayer(player, position);

            if (player.isHomeTeam != homeKickoff ||
                player.data.preferredPosition !=
                PlayerPosition.Forward)
                continue;

            float distance = position.magnitude;

            if (distance < closest)
            {
                closest = distance;
                taker = player;
            }
        }

        if (taker != null)
        {
            Vector2 position = new Vector2(
                homeKickoff ? -0.13f : 0.13f,
                0f);

            PlacePlayer(taker, position);
        }

        return taker;
    }

    void PlacePlayer(
        PlayerController player,
        Vector2 position)
    {
        Rigidbody2D playerRb =
            player.GetComponent<Rigidbody2D>();

        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
            playerRb.angularVelocity = 0f;
            playerRb.position = position;
        }

        player.transform.position = position;

        player.SetTarget(
            position,
            "Waiting for Restart");
    }

    PlayerController FindKickoffReceiver(
        PlayerController taker)
    {
        PlayerController receiver = null;
        float bestScore = float.NegativeInfinity;

        foreach (PlayerController player in players)
        {
            if (player == taker ||
                player.data == null ||
                player.isHomeTeam != taker.isHomeTeam)
                continue;

            if (player.data.preferredPosition !=
                PlayerPosition.Midfielder)
                continue;

            float distance =
                player.transform.position.magnitude;

            float score =
                player.data.passing / 100f - distance;

            if (score > bestScore)
            {
                bestScore = score;
                receiver = player;
            }
        }

        return receiver;
    }

    void SetAIEnabled(bool value)
    {
        foreach (MonoBehaviour system in aiSystems)
        {
            if (system != null)
                system.enabled = value;
        }
    }

    void FreezeBall(Vector2 position)
    {
        ballController.ResetBall(position);

        ballRb.linearVelocity = Vector2.zero;
        ballRb.angularVelocity = 0f;
        ballRb.bodyType = RigidbodyType2D.Kinematic;
    }

    void CancelRestart()
    {
        if (restartRoutine != null)
        {
            StopCoroutine(restartRoutine);
            restartRoutine = null;
        }
    }

    void ResumePlay()
    {
        SetAIEnabled(true);

        isRestarting = false;
        restartLabel = "";
        restartRoutine = null;
    }

    public void StartHalfTime()
    {
        if (isHalfTime || isFullTime)
            return;

        CancelRestart();

        isHalfTime = true;
        isRestarting = true;
        restartLabel = "HALF TIME";

        SetAIEnabled(false);

        FreezeBall(Vector2.zero);

        ResetFormation(!firstKickoffHome);

        Debug.Log("HALF TIME");
    }

    public void StartSecondHalf()
    {
        isHalfTime = false;

        BeginKickoff(!firstKickoffHome);

        Debug.Log("SECOND HALF");
    }

    public void EndMatch()
    {
        isFullTime = true;
        isRestarting = true;
        restartLabel = "FULL TIME";

        CancelRestart();

        SetAIEnabled(false);

        FreezeBall(ballController.transform.position);

        Debug.Log(
            "FULL TIME: Edmonton FC " +
            homeScore + " - " + awayScore +
            " Calgary FC");
    }

    void HandleThrowIn(Vector2 position)
    {
        bool homeRestart =
            !ballController.lastTouchHome;

        Vector2 restartPosition = new Vector2(
            Mathf.Clamp(
                position.x,
                -goalLineX + 0.2f,
                goalLineX - 0.2f),

            Mathf.Sign(position.y) *
                (touchLineY - 0.12f));

        if (homeRestart)
            homeThrowIns++;
        else
            awayThrowIns++;

        BeginSetPiece(
            homeRestart,
            restartPosition,
            false,
            throwInDelay,
            "THROW-IN",
            throwInSpeed);
    }

    void HandleGoalLineRestart(Vector2 position)
    {
        bool defendingHome = position.x < 0f;

        bool defenderTouchedLast =
            ballController.lastTouchHome ==
            defendingHome;

        if (defenderTouchedLast)
        {
            bool attackingHome = !defendingHome;

            Vector2 cornerPosition = new Vector2(
                Mathf.Sign(position.x) *
                    (goalLineX - 0.15f),

                Mathf.Sign(position.y) *
                    (touchLineY - 0.15f));

            if (attackingHome)
                homeCorners++;
            else
                awayCorners++;

            BeginSetPiece(
                attackingHome,
                cornerPosition,
                false,
                cornerDelay,
                "CORNER",
                restartPassSpeed);
        }
        else
        {
            Vector2 goalKickPosition = new Vector2(
                defendingHome
                    ? -goalLineX + 0.4f
                    : goalLineX - 0.4f,
                0f);

            if (defendingHome)
                homeGoalKicks++;
            else
                awayGoalKicks++;

            BeginSetPiece(
                defendingHome,
                goalKickPosition,
                true,
                goalKickDelay,
                "GOAL KICK",
                restartPassSpeed);
        }
    }

    void BeginSetPiece(
        bool homeTeam,
        Vector2 position,
        bool preferGoalkeeper,
        float delay,
        string label,
        float passSpeed)
    {
        CancelRestart();

        restartRoutine = StartCoroutine(
            SetPieceSequence(
                homeTeam,
                position,
                preferGoalkeeper,
                delay,
                label,
                passSpeed));
    }

    IEnumerator SetPieceSequence(
        bool homeTeam,
        Vector2 position,
        bool preferGoalkeeper,
        float delay,
        string label,
        float passSpeed)
    {
        isRestarting = true;
        restartLabel = label;

        SetAIEnabled(false);

        FreezeBall(position);

        Debug.Log(
            label + ": " + GetTeamName(homeTeam));

        // Stop players from chasing the ball.
        foreach (PlayerController player in players)
        {
            player.SetTarget(
                player.transform.position,
                "Waiting for Restart");
        }

        PlayerController taker =
            FindRestartTaker(
                homeTeam,
                position,
                preferGoalkeeper);

        // Short pause before the player moves.
        yield return new WaitForSeconds(0.5f);

        if (taker != null)
        {
            taker.SetTarget(
                position,
                "Preparing " + label);
        }

        // Allow the player to approach the ball.
        float remainingTime =
            Mathf.Max(0.1f, delay - 1f);

        yield return new WaitForSeconds(
            remainingTime);

        if (taker != null)
        {
            PlacePlayer(taker, position);

            taker.currentAction =
                "Taking " + label;
        }

        PlayerController receiver =
            FindRestartReceiver(
                homeTeam,
                taker,
                position);

        // Brief pause before releasing the ball.
        yield return new WaitForSeconds(0.5f);

        Vector2 passTarget = receiver != null
            ? (Vector2)receiver.transform.position
            : Vector2.zero;

        Vector2 difference =
            passTarget - position;

        float speed = Mathf.Max(
            passSpeed,
            difference.magnitude * 1.5f + 0.5f);

        if (taker != null)
            ballController.RegisterTouch(taker);

        ballController.ReleaseBall(
            difference.normalized * speed);

        if (receiver != null)
        {
            receiver.SetTarget(
                passTarget,
                "Receiving " + label);
        }

        ResumePlay();
    }

    PlayerController FindRestartTaker(
        bool homeTeam,
        Vector2 position,
        bool preferGoalkeeper)
    {
        PlayerController taker = null;
        float bestDistance = Mathf.Infinity;

        foreach (PlayerController player in players)
        {
            if (player.data == null ||
                player.isHomeTeam != homeTeam)
                continue;

            bool isKeeper =
                player.data.preferredPosition ==
                PlayerPosition.Goalkeeper;

            if (preferGoalkeeper && isKeeper)
                return player;

            if (!preferGoalkeeper && isKeeper)
                continue;

            float distance = Vector2.Distance(
                player.transform.position,
                position);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                taker = player;
            }
        }

        return taker;
    }

    PlayerController FindRestartReceiver(
        bool homeTeam,
        PlayerController taker,
        Vector2 position)
    {
        PlayerController receiver = null;
        float bestScore = float.NegativeInfinity;

        foreach (PlayerController teammate in players)
        {
            if (teammate == taker ||
                teammate.data == null ||
                teammate.isHomeTeam != homeTeam)
                continue;

            if (teammate.data.preferredPosition ==
                PlayerPosition.Goalkeeper)
                continue;

            Vector2 target =
                teammate.transform.position;

            float distance =
                Vector2.Distance(position, target);

            if (distance < 0.4f)
                continue;

            if (Mathf.Abs(target.x) >=
                goalLineX - 0.2f ||
                Mathf.Abs(target.y) >=
                touchLineY - 0.2f)
                continue;

            float openness = 5f;

            foreach (PlayerController opponent in players)
            {
                if (opponent.isHomeTeam == homeTeam)
                    continue;

                openness = Mathf.Min(
                    openness,
                    Vector2.Distance(
                        target,
                        opponent.transform.position));
            }

            float score =
                openness * 1.2f - distance * 0.5f;

            if (score > bestScore)
            {
                bestScore = score;
                receiver = teammate;
            }
        }

        return receiver;
    }

    string GetTeamName(bool homeTeam)
    {
        return homeTeam
            ? "Edmonton FC"
            : "Calgary FC";
    }
}
