
using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviour
{
    [Header("Player Profile")]
    public PlayerData data;

    [Header("Team Information")]
    public bool isHomeTeam;

    [Header("Visual References")]
    public TMP_Text shirtNumberText;

    [Header("Tactical Information")]
    public PlayerPosition assignedPosition;
    public int formationSlot = -1;

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

        assignedPosition = data.preferredPosition;
        formationSlot = -1;

        currentStamina = data.stamina;
        hasBall = false;

        homePosition = transform.position;
        targetPosition = homePosition;

        movementVelocity = Vector2.zero;

        moveSpeed = Mathf.Lerp(
            0.4f,
            1.2f,
            data.speed / 100f);

        gameObject.name =
            data.shirtNumber + " - " + data.playerName;

        UpdateShirtNumber();
    }

    public void UpdateShirtNumber()
    {
        if (shirtNumberText == null)
        {
            Transform numberObject =
                transform.Find("ShirtNumber");

            if (numberObject != null)
            {
                shirtNumberText =
                    numberObject.GetComponent<TMP_Text>();
            }
        }

        if (shirtNumberText != null && data != null)
        {
            shirtNumberText.text =
                data.shirtNumber.ToString();

            Renderer textRenderer =
                shirtNumberText.GetComponent<Renderer>();

            SpriteRenderer playerRenderer =
                GetComponent<SpriteRenderer>();

            if (textRenderer != null &&
                playerRenderer != null)
            {
                textRenderer.sortingLayerID =
                    playerRenderer.sortingLayerID;

                textRenderer.sortingOrder =
                    playerRenderer.sortingOrder + 1;
            }
        }
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

        predicted.x = Mathf.Clamp(
            predicted.x, -3.05f, 3.05f);

        predicted.y = Mathf.Clamp(
            predicted.y, -1.7f, 1.7f);

        return predicted;
    }

    public void SetTarget(Vector2 position, string action)
    {
        position.x = Mathf.Clamp(
            position.x, -3.05f, 3.05f);

        position.y = Mathf.Clamp(
            position.y, -1.7f, 1.7f);

        targetPosition = position;
        currentAction = action;
    }

    public void SetTacticalPosition(
        PlayerPosition newPosition,
        Vector2 newHomePosition,
        int newSlot)
    {
        assignedPosition = newPosition;
        formationSlot = newSlot;

        newHomePosition.x = Mathf.Clamp(
            newHomePosition.x, -3.05f, 3.05f);

        newHomePosition.y = Mathf.Clamp(
            newHomePosition.y, -1.7f, 1.7f);

        homePosition = newHomePosition;

        SetTarget(
            homePosition,
            "Adjusting Tactical Position");
    }

    public bool IsPlayingOutOfPosition()
    {
        if (data == null)
            return false;

        return assignedPosition !=
            data.preferredPosition;
    }
}
