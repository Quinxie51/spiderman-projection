using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpiderProjection.Runtime
{
    [CreateAssetMenu(menuName = "Spider Projection/Projection Layout", fileName = "ProjectionLayout")]
    public sealed class ProjectionLayout : ScriptableObject
    {
        public string layoutName = "Example Wall - Replace With Measured Values";
        public Vector2Int outputResolution = new Vector2Int(1920, 1080);
        public int displayIndex;
        public bool fullScreen = true;
        public float orthographicSize = 5.4f;
        public Color clearColor = Color.black;
        public List<SurfaceLayout> surfaces = new List<SurfaceLayout>();

        [Serializable]
        public sealed class SurfaceLayout
        {
            public string id;
            public MappedSurfaceKind kind;
            public Rect normalizedRect;
            public bool landable;
            public bool oneWay;
            public bool webAttachable;
            public string visibleSprite;
        }

        public Vector2 NormalizedToWorld(Vector2 normalized, float aspect)
        {
            return ProjectionCoordinates.NormalizedToWorld(normalized, orthographicSize, aspect);
        }
    }

    public static class ProjectionCoordinates
    {
        public static Vector2 NormalizedToWorld(Vector2 normalized, float orthographicSize, float aspect)
        {
            float height = orthographicSize * 2f;
            float width = height * aspect;
            return new Vector2(
                (normalized.x - 0.5f) * width,
                (normalized.y - 0.5f) * height);
        }

        public static Rect NormalizedRectToWorld(Rect normalized, float orthographicSize, float aspect)
        {
            Vector2 min = NormalizedToWorld(normalized.min, orthographicSize, aspect);
            Vector2 max = NormalizedToWorld(normalized.max, orthographicSize, aspect);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
