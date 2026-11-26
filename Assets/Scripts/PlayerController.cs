using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Player Profile")]
    public PlayerData data;

    [Header("Team Information")]
    public bool isHomeTeam;

    [Header("Match Information")]
    public float currentStamina;
    public bool hasBall;

    [Header("Movement")]
    public Vector2 homePosition;
    public Vector2 targetPosition;
    public float moveSpeed;
    public Vector2 movementVelocity;

    [Header("AI Information")]
    public string currentAction = "Holding Position";

    public void Initialize(PlayerData playerData, bool home)
    {
        data = playerData;
        isHomeTeam = home;

        currentStamina = data.stamina;
        hasBall = false;

        homePosition = transform.position;
        targetPosition = homePosition;
        movementVelocity = Vector2.zero;

        moveSpeed = Mathf.Lerp(
            0.4f, 1.2f, data.speed / 100f);

        gameObject.name = data.shirtNumber
            + " - " + data.playerName;
    }

    void Update()
    {
        if (data == null || Time.deltaTime <= 0f)
            return;

        MovePlayer();
    }

    void MovePlayer()
    {
        Vector2 previousPosition = transform.position;

        Vector2 newPosition = Vector2.MoveTowards(
            previousPosition,
            targetPosition,
            moveSpeed * Time.deltaTime);

        transform.position = newPosition;

        movementVelocity =
            (newPosition - previousPosition) /
            Time.deltaTime;
    }

    public Vector2 PredictPosition(float seconds)
    {
        Vector2 predicted = Vector2.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Mathf.Max(0f, seconds));

        predicted.x = Mathf.Clamp(predicted.x, -3.05f, 3.05f);
        predicted.y = Mathf.Clamp(predicted.y, -1.7f, 1.7f);

        return predicted;
    }

    public void SetTarget(Vector2 position, string action)
    {
        position.x = Mathf.Clamp(position.x, -3.05f, 3.05f);
        position.y = Mathf.Clamp(position.y, -1.7f, 1.7f);

        targetPosition = position;
        currentAction = action;
    }
}