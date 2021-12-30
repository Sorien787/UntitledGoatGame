using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class CommandBufferExtensions
{
    private static List<MeshFilter> _meshes = new List<MeshFilter>();
    private static List<SkinnedMeshRenderer> _skinnedMeshes = new List<SkinnedMeshRenderer>();
    public static void DrawAllMeshes(this CommandBuffer cmd, GameObject gameObject, Material material, int pass)
    {
        _meshes.Clear();
        _skinnedMeshes.Clear();

        gameObject.GetComponentsInChildren<MeshFilter>(_meshes);
        if (_meshes.Count == 0)
        {
            gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(_skinnedMeshes);
        }

        foreach (var meshFilter in _meshes)
        {
            // Static objects may use static batching, preventing us from accessing their default mesh
            if (!meshFilter.gameObject.isStatic)
            {
                var mesh = meshFilter.sharedMesh;
                // Render all submeshes
                for (int i = 0; i < mesh.subMeshCount; i++)
                    cmd.DrawMesh(mesh, meshFilter.transform.localToWorldMatrix, material, i, pass);
            }
        }

        foreach (var skinnedMesh in _skinnedMeshes)
        {
            var mesh = skinnedMesh.sharedMesh;
            // Render all submeshes
            for (int i = 0; i < mesh.subMeshCount; i++)
                cmd.DrawRenderer(skinnedMesh, material, i, pass);
        }
    }

    private static Mesh _fullscreenTriangle;
    public static void BlitFullscreenTriangle(this CommandBuffer cmd, Material material, int pass)
    {
        if (_fullscreenTriangle == null)
        {
            _fullscreenTriangle = new Mesh
            {
                name = "_fullScreenTriangle",
                vertices = new Vector3[] {
                new Vector3(-1f, -1f, 0f),
                new Vector3(-1f,  3f, 0f),
                new Vector3( 3f, -1f, 0f)
            },
                triangles = new int[] { 0, 1, 2 },
            };
            _fullscreenTriangle.UploadMeshData(true);
        }

        cmd.DrawMesh(_fullscreenTriangle, Matrix4x4.identity, material, 0, pass);
    }
}