using System.Collections.Generic;
using UnityEngine;
using System;

[RequireComponent(typeof(FoodSourceComponent))]
public class BushComponent : MonoBehaviour, IFoodSourceSizeListener
{
    [Header("Animation and Generation params")]
    [SerializeField] private float m_TimeForFlowerAnim;
    [SerializeField] private uint m_TargetNumOfFlowers;
    [SerializeField] private float m_MaxAngleFromUp;
    [SerializeField] private float m_FlowerSizeRandom;
    [SerializeField] private float m_FlowerScalar;

    [Header("Object references")]
    [SerializeField] private Mesh m_BerryMesh;
    [SerializeField] private Material m_BerryMaterial;
    [SerializeField] private Transform m_Transform;


    private class Flower 
    {
        public float m_SizeScalar;
        public float m_CurrentSize;
        public float m_SizeChangeVelocity;
        public Vector3 m_flowerPos;
        public Quaternion m_flowerRotation;
    }

    private float m_CurrentFoodSize = 0.0f;
    private readonly List<Flower> m_Flowers = new List<Flower>();
    private Matrix4x4[] m_Matrices;

    static void AddMeshAndTransformDataForUse(in List<Mesh> meshes, in List<Transform> transforms, GameObject gameObjectToAdd) 
    {

        MeshFilter meshFilter = gameObjectToAdd.GetComponent<MeshFilter>();
        if (!meshFilter)
            return;
        transforms.Add(gameObjectToAdd.transform);
        meshes.Add(meshFilter.mesh);
    }

	private struct Triangle 
    {
        public Vector3 cornerA;
        public Vector3 cornerB;
        public Vector3 cornerC;

        public float CalculateArea() 
        {
            float ang = Vector3.Angle(cornerC - cornerA, cornerB - cornerA);
            return 0.5f * (cornerC - cornerA).magnitude * (cornerB - cornerA).magnitude * Mathf.Sin(Mathf.Deg2Rad * ang);
        }

        public Vector3 GetRandomPosOnTri()
        {
            Vector3 one = cornerB - cornerA;
            Vector3 two = cornerC - cornerA;
            float randA = UnityEngine.Random.Range(0.0f, 1.0f);
            float randB = UnityEngine.Random.Range(0.0f, 1.0f);
            if (randA + randB > 1.0f)
            {
                randA = 1 - randA;
                randB = 1 - randB;
            }
            Vector3 transformed = randA * one + randB * two;
            return cornerA + transformed;
        }

        public Vector3 GetNorm()
        {
            return Vector3.Normalize(Vector3.Cross(cornerB - cornerA, cornerC - cornerA));
        }
    }

    void ForEachValidTriInMesh(in Mesh mesh, in Transform transform, Action<Triangle> func)
    {
        Triangle triangle = new Triangle();
        for (int j = 0; j < mesh.triangles.Length - 2; j += 3)
        {
            triangle.cornerA = transform.rotation * Vector3.Scale(transform.localScale, mesh.vertices[mesh.triangles[j]]) + transform.position;
            triangle.cornerB = transform.rotation * Vector3.Scale(transform.localScale, mesh.vertices[mesh.triangles[j + 1]]) + transform.position;
            triangle.cornerC = transform.rotation * Vector3.Scale(transform.localScale, mesh.vertices[mesh.triangles[j + 2]]) + transform.position;

            Vector3 spawnUp = triangle.GetNorm();
            float angFromUp = Vector3.Angle(spawnUp, Vector3.up);

            if (angFromUp > m_MaxAngleFromUp)
                continue;

            func(triangle);
        }
    }

	private void Awake()
	{
        m_BerryMesh = GetComponentInChildren<MeshFilter>().sharedMesh;
        GetComponent<FoodSourceComponent>().AddListener(this);

        List<Mesh> meshes = new List<Mesh>();
        List<Transform> transforms = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++) 
        {
            AddMeshAndTransformDataForUse(meshes, transforms, transform.GetChild(i).gameObject);
        }

        AddMeshAndTransformDataForUse(meshes, transforms, gameObject);

        float validArea = 0.0f;
        int triNum = 0;

        for (int i = 0; i < meshes.Count; i++) 
        {
            ForEachValidTriInMesh(meshes[i], transforms[i], (Triangle tri) =>
            {
                triNum++;
                validArea += tri.CalculateArea();
            });
        }

        float areaPerTri = validArea / triNum;
        float currentArea = 0;

        for (int i = 0; i < meshes.Count; i++) 
        {
            ForEachValidTriInMesh(meshes[i], transforms[i], (Triangle tri) =>
            {
                currentArea += tri.CalculateArea();
                while (currentArea > areaPerTri) 
                {
                    currentArea -= areaPerTri;

                    Vector3 spawnPos = tri.GetRandomPosOnTri();
                    Quaternion spawnQuat = Quaternion.LookRotation(tri.GetNorm(), Vector3.up);

                    Flower flower = new Flower
                    {
                        m_flowerPos = spawnPos,
                        m_flowerRotation = UnityEngine.Random.rotation,
                        m_CurrentSize = 1.0f,
                        m_SizeScalar = UnityEngine.Random.Range(1 - m_FlowerSizeRandom, 1 + m_FlowerSizeRandom),
                        m_SizeChangeVelocity = 0.0f
                    };
                    m_Flowers.Add(flower);
                }
            });
        }
        m_Matrices = new Matrix4x4[m_Flowers.Count];
        for (int i = 0; i < m_Matrices.Length; i++) 
        {
            Flower flower = m_Flowers[i];
            m_Matrices[i] = Matrix4x4.TRS(flower.m_flowerPos, flower.m_flowerRotation, flower.m_CurrentSize * flower.m_SizeScalar * Vector3.one);
        }
    }

	public void OnSetFoodSize(float foodSize)
	{
        if (m_CurrentFoodSize != foodSize) 
        {
            m_CurrentFoodSize = foodSize;
            m_bUpdateSizes = true;
        }
    }

    private bool m_bUpdateSizes = true;

    void Update()
    {
        bool isReadable = m_BerryMesh.isReadable;

        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] { Vector3.zero, Vector3.forward, Vector3.right};
        mesh.triangles = new int[] { 0, 1, 2 };
        mesh.normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up };
        Graphics.DrawMeshInstanced(mesh, 0, m_BerryMaterial, m_Matrices);

        if (!m_bUpdateSizes)
            return;

        bool allFlowersAtTarget = true;
		for (int i = 0; i < m_Flowers.Count; i++)
		{
			Flower currentFlower = m_Flowers[i];
			float target = (float)i / m_Flowers.Count <= m_CurrentFoodSize ? 1.0f : 0.0f;

			if (Mathf.Abs(m_Flowers[i].m_CurrentSize - target) < 0.001f)
				continue;
			allFlowersAtTarget = false;
			currentFlower.m_CurrentSize = Mathf.SmoothDamp(currentFlower.m_CurrentSize, target, ref currentFlower.m_SizeChangeVelocity, m_TimeForFlowerAnim);
			m_Matrices[i] = Matrix4x4.TRS(currentFlower.m_flowerPos, currentFlower.m_flowerRotation, currentFlower.m_CurrentSize * m_FlowerScalar * Vector3.one * currentFlower.m_SizeScalar);
		}

		if (allFlowersAtTarget)
            m_bUpdateSizes = false;
    }
}
