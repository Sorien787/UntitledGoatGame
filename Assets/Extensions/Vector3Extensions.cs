
using UnityEngine;

public static class Vector3Extensions
{
	public static Vector3 Modulo(this Vector3 vector3, int divisor)
	{
		return new Vector3(vector3.x % divisor, vector3.y % divisor, vector3.z % divisor);
	}

	public static Vector4 ToVector4(this Vector3 vector3, int w)
	{
		return new Vector4(vector3.x, vector3.y, vector3.z, w);
	}
}
