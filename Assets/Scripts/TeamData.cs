
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(
    fileName = "NewTeam",
    menuName = "Football Manager/Team")]
public class TeamData : ScriptableObject
{
    [Header("Club Information")]
    public string teamName;
    public string shortName;
    public Color teamColor = Color.red;
    public Sprite clubLogo;

    [Header("Club Finances")]
    public int transferBudget = 1000000;

    [Header("Tactics")]
    public FormationType formation =
        FormationType.Formation433;

    [Header("Squad")]
    public List<PlayerData> squad =
        new List<PlayerData>();

    [Header("Starting Lineup")]
    public List<PlayerData> startingLineup =
        new List<PlayerData>();
}
