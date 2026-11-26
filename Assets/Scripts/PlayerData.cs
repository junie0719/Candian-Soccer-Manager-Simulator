using UnityEngine;

public enum PlayerPosition
{
    Goalkeeper,
    Defender,
    Midfielder,
    Forward
}

[CreateAssetMenu(
    fileName = "NewPlayer",
    menuName = "Football Manager/Player")]
public class PlayerData : ScriptableObject
{
    [Header("Personal Information")]
    public string playerName;
    public int age = 23;
    public int shirtNumber;
    public PlayerPosition preferredPosition;

    [Header("Technical Attributes")]
    [Range(1, 100)] public int passing = 70;
    [Range(1, 100)] public int shooting = 70;
    [Range(1, 100)] public int defending = 70;
    [Range(1, 100)] public int dribbling = 70;
    [Range(1, 100)] public int goalkeeping = 20;

    [Header("Physical Attributes")]
    [Range(1, 100)] public int speed = 70;
    [Range(1, 100)] public int stamina = 80;
    [Range(1, 100)] public int strength = 70;

    [Header("Mental Attributes")]
    [Range(1, 100)] public int positioning = 70;
    [Range(1, 100)] public int decisionMaking = 70;

    [Header("Career Information")]
    public int marketValue = 100000;
    [Range(1, 100)] public int potential = 85;
}