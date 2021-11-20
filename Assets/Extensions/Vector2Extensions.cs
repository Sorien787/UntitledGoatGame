using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Vector2Extensions
{
    public static Vector2 ToVector2(this Vector3 vec3)
    {
        return new Vector2 { x = vec3.x, y = vec3.y };
    }

    public static Vector2 Vec2Clamp(Vector2 ToClamp, in Vector2 Minimum, in Vector2 Maximum)
    {
        ToClamp.x = Mathf.Clamp(ToClamp.x, Minimum.x, Maximum.x);
        ToClamp.y = Mathf.Clamp(ToClamp.y, Minimum.y, Maximum.y);
        return ToClamp;
    }

    public static Vector2 Vec2Min(Vector2 ToTakeMin, Vector2 Minimum)
    {
        ToTakeMin.x = Mathf.Min(ToTakeMin.x, Minimum.x);
        ToTakeMin.y = Mathf.Min(ToTakeMin.y, Minimum.y);
        return ToTakeMin;
    }

    public static Vector2 Vec2Max(Vector2 ToTakeMax, Vector2 Maximum)
    {
        ToTakeMax.x = Mathf.Max(ToTakeMax.x, Maximum.x);
        ToTakeMax.y = Mathf.Max(ToTakeMax.y, Maximum.y);
        return ToTakeMax;
    }
}
