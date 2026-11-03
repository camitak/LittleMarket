using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class LayoutVisualUtility
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private static readonly int ColorId = Shader.PropertyToID("_Color");

    public static Material CreateTransparentMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            Debug.LogError("Could not find a shader for Layout Mode visuals.");

            return null;
        }

        Material material = new(shader) { name = materialName };

        ConfigureTransparency(material);

        SetMaterialColor(material, color);

        return material;
    }

    public static void SetMaterialColor(Material material, Color color)
    {
        if (material == null)
            return;

        if (material.HasProperty(BaseColorId))
        {
            material.SetColor(BaseColorId, color);
        }

        if (material.HasProperty(ColorId))
        {
            material.SetColor(ColorId, color);
        }
    }

    public static Mesh CreateUnitQuad()
    {
        Mesh mesh = new() { name = "LayoutVisualUnitQuad" };

        mesh.vertices = new[]
        {
            new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, 0.5f),
            new Vector3(-0.5f, 0f, 0.5f)
        };

        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };

        mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };

        mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };

        mesh.RecalculateBounds();

        return mesh;
    }

    public static Mesh CreateGridMesh(IReadOnlyList<Bounds> floorBounds,
        float gridSize,
        float lineWidth,
        float surfaceOffset)
    {
        Mesh mesh = new() { name = "LayoutFloorGrid" };

        mesh.indexFormat = IndexFormat.UInt32;

        List<Vector3> vertices = new();

        List<int> triangles = new();

        float safeGridSize = Mathf.Max(0.1f, gridSize);

        float safeLineWidth = Mathf.Max(0.001f, lineWidth);

        if (floorBounds != null)
        {
            foreach (Bounds bounds in floorBounds)
            {
                AddGridForBounds(bounds, safeGridSize, safeLineWidth, surfaceOffset, vertices, triangles);
            }
        }

        mesh.SetVertices(vertices);

        mesh.SetTriangles(triangles, 0);

        mesh.RecalculateBounds();

        return mesh;
    }

    private static void AddGridForBounds(Bounds bounds,
        float gridSize,
        float lineWidth,
        float surfaceOffset,
        List<Vector3> vertices,
        List<int> triangles)
    {
        float y = bounds.max.y + surfaceOffset;

        int firstX = Mathf.CeilToInt(bounds.min.x / gridSize);

        int lastX = Mathf.FloorToInt(bounds.max.x / gridSize);

        for (int i = firstX; i <= lastX; i++)
        {
            float x = i * gridSize;

            AddQuad(x - lineWidth * 0.5f, x + lineWidth * 0.5f, bounds.min.z, bounds.max.z, y, vertices, triangles);
        }

        int firstZ = Mathf.CeilToInt(bounds.min.z / gridSize);

        int lastZ = Mathf.FloorToInt(bounds.max.z / gridSize);

        for (int i = firstZ; i <= lastZ; i++)
        {
            float z = i * gridSize;

            AddQuad(bounds.min.x, bounds.max.x, z - lineWidth * 0.5f, z + lineWidth * 0.5f, y, vertices, triangles);
        }
    }

    private static void AddQuad(float minX,
        float maxX,
        float minZ,
        float maxZ,
        float y,
        List<Vector3> vertices,
        List<int> triangles)
    {
        int start = vertices.Count;

        vertices.Add(new Vector3(minX, y, minZ));

        vertices.Add(new Vector3(maxX, y, minZ));

        vertices.Add(new Vector3(maxX, y, maxZ));

        vertices.Add(new Vector3(minX, y, maxZ));

        triangles.Add(start);
        triangles.Add(start + 2);
        triangles.Add(start + 1);

        triangles.Add(start);
        triangles.Add(start + 3);
        triangles.Add(start + 2);
    }

    private static void ConfigureTransparency(Material material)
    {
        if (material == null)
            return;

        material.SetOverrideTag("RenderType", "Transparent");

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat("_Blend", 0f);
        }

        if (material.HasProperty("_SrcBlend"))
        {
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        }

        if (material.HasProperty("_DstBlend"))
        {
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        }

        if (material.HasProperty("_ZWrite"))
        {
            material.SetFloat("_ZWrite", 0f);
        }

        if (material.HasProperty("_Cull"))
        {
            material.SetFloat("_Cull", (float)CullMode.Off);
        }

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        material.renderQueue = (int)RenderQueue.Transparent;
    }
}