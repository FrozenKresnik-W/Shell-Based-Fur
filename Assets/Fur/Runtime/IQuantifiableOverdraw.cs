using UnityEngine;

namespace Aperture.Fur.Runtime
{
    /// <summary>
    /// 可量化Overdraw
    /// </summary>
    public interface IQuantifiableOverdraw
    {
        float GetRelativeHeight(Camera camera);

        float GetFurLength();

        public static float DistanceToRelativeHeight(Camera camera, float distance, float size)
        {
            if (camera.orthographic)
                return size * 0.5F / camera.orthographicSize;

            var halfAngle = Mathf.Tan(Mathf.Deg2Rad * camera.fieldOfView * 0.5F);
            var relativeHeight = size * 0.5F / (distance * halfAngle);
            return relativeHeight;
        }

        public float GetExpectedLayerCount(Camera camera, float relativeHeight, float quality)
        {
            float furLengthOS = GetFurLength();
            float expectedLayerCount = furLengthOS * relativeHeight * camera.pixelHeight * quality;

            return expectedLayerCount;
        }
    }
}
