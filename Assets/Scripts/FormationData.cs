
using UnityEngine;
using System.Collections.Generic;

public enum FormationType
{
    Formation433,
    Formation442,
    Formation4231,
    Formation352,
    Formation532,
    Formation343
}

public static class FormationData
{
    public static string GetName(FormationType formation)
    {
        switch (formation)
        {
            case FormationType.Formation433:
                return "4-3-3";

            case FormationType.Formation442:
                return "4-4-2";

            case FormationType.Formation4231:
                return "4-2-3-1";

            case FormationType.Formation352:
                return "3-5-2";

            case FormationType.Formation532:
                return "5-3-2";

            case FormationType.Formation343:
                return "3-4-3";

            default:
                return "4-3-3";
        }
    }

    public static Vector2[] GetPositions(
        FormationType formation)
    {
        List<Vector2> positions =
            new List<Vector2>();

        // Goalkeeper
        positions.Add(new Vector2(-2.8f, 0f));

        switch (formation)
        {
            case FormationType.Formation433:

                AddLine(positions, 4, -2.1f, 1.3f);
                AddLine(positions, 3, -1.2f, 1.0f);
                AddLine(positions, 3, -0.4f, 1.2f);

                break;

            case FormationType.Formation442:

                AddLine(positions, 4, -2.1f, 1.3f);
                AddLine(positions, 4, -1.2f, 1.3f);
                AddLine(positions, 2, -0.4f, 0.55f);

                break;

            case FormationType.Formation4231:

                AddLine(positions, 4, -2.1f, 1.3f);
                AddLine(positions, 2, -1.55f, 0.55f);
                AddLine(positions, 3, -0.9f, 1.2f);
                AddLine(positions, 1, -0.3f, 0f);

                break;

            case FormationType.Formation352:

                AddLine(positions, 3, -2.1f, 1.0f);
                AddLine(positions, 5, -1.2f, 1.45f);
                AddLine(positions, 2, -0.4f, 0.55f);

                break;

            case FormationType.Formation532:

                AddLine(positions, 5, -2.1f, 1.45f);
                AddLine(positions, 3, -1.2f, 1.0f);
                AddLine(positions, 2, -0.4f, 0.55f);

                break;

            case FormationType.Formation343:

                AddLine(positions, 3, -2.1f, 1.0f);
                AddLine(positions, 4, -1.2f, 1.3f);
                AddLine(positions, 3, -0.4f, 1.2f);

                break;
        }

        return positions.ToArray();
    }

    public static PlayerPosition[] GetRoles(
        FormationType formation)
    {
        List<PlayerPosition> roles =
            new List<PlayerPosition>();

        roles.Add(PlayerPosition.Goalkeeper);

        switch (formation)
        {
            case FormationType.Formation433:
                AddRoles(roles, 4, 3, 3);
                break;

            case FormationType.Formation442:
                AddRoles(roles, 4, 4, 2);
                break;

            case FormationType.Formation4231:
                AddRoles(roles, 4, 5, 1);
                break;

            case FormationType.Formation352:
                AddRoles(roles, 3, 5, 2);
                break;

            case FormationType.Formation532:
                AddRoles(roles, 5, 3, 2);
                break;

            case FormationType.Formation343:
                AddRoles(roles, 3, 4, 3);
                break;
        }

        return roles.ToArray();
    }

    private static void AddLine(
        List<Vector2> positions,
        int count,
        float x,
        float width)
    {
        if (count == 1)
        {
            positions.Add(new Vector2(x, 0f));
            return;
        }

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / (count - 1);

            float y = Mathf.Lerp(
                width, -width, t);

            positions.Add(new Vector2(x, y));
        }
    }

    private static void AddRoles(
        List<PlayerPosition> roles,
        int defenders,
        int midfielders,
        int forwards)
    {
        for (int i = 0; i < defenders; i++)
            roles.Add(PlayerPosition.Defender);

        for (int i = 0; i < midfielders; i++)
            roles.Add(PlayerPosition.Midfielder);

        for (int i = 0; i < forwards; i++)
            roles.Add(PlayerPosition.Forward);
    }
}
