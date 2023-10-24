using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Aperture.Fur.Runtime
{
    /// <summary>
    /// 毛发对象层数均衡器
    /// 用期望层数和最大Overdraw控制毛发层数
    /// </summary>
    public static class FurLayerBalancer
    {
        /// <summary>
        /// 毛发质量(预期层数的系数)
        /// </summary>
        private static float s_FurQuality = 0.4f;

        /// <summary>
        /// 用于毛发物体的总预估Overdraw限制
        /// </summary>
        private static float s_MaxOverdraw = 5.0f;

        /// <summary>
        /// 每个相机下统计的总预估Overdraw
        /// </summary>
        private static Dictionary<Camera, float> s_Camera2EstimatedOverdraw = new Dictionary<Camera, float>();

        /// <summary>
        /// 需要处理的对象
        /// </summary>
        private static List<IQuantifiableOverdraw> s_QuantifiableOverdrawObjects = new List<IQuantifiableOverdraw>();

        static FurLayerBalancer()
        {
            s_Camera2EstimatedOverdraw.Clear();
            s_QuantifiableOverdrawObjects.Clear();

            RenderPipelineManager.beginFrameRendering += OnBeginFrameRendering;
        }

        private static void OnBeginFrameRendering(ScriptableRenderContext scriptableRenderContext, Camera[] cameras)
        {
            s_Camera2EstimatedOverdraw.Clear();

            for (int i = 0; i < cameras.Length; i++)
            {
                CalcEstimatedOverdraw(cameras[i]);
            }
        }

        /// <summary>
        /// 设置毛发的预期质量，如果需要的话
        /// </summary>
        /// <param name="quality"></param>
        public static void SetFurQuality(FurQuality quality)
        {
            s_FurQuality = 0.2f + 0.1f * (int)quality;
        }

        /// <summary>
        /// 按全屏幕像素数总数作为单位的Overdraw量化，即一屏幕像素就是 Overdraw = 1.0
        /// </summary>
        /// <param name="overdraw"></param>
        public static void SetMaxOverdraw(float overdraw)
        {
            s_MaxOverdraw = overdraw;
        }

        /// <summary>
        /// 计算指定摄像机当前帧所有毛发物体累积的估计屏幕占比
        /// </summary>
        /// <param name="camera"></param>
        private static void CalcEstimatedOverdraw(Camera camera)
        {
            float overdraw = 0.0f;

            for(int i = 0; i < s_QuantifiableOverdrawObjects.Count; i++)
            {
                IQuantifiableOverdraw quantifiableOverdraw = s_QuantifiableOverdrawObjects[i];
                float relativeHeight = quantifiableOverdraw.GetRelativeHeight(camera);
                float relativeWidth = relativeHeight / camera.aspect;
                //预期层数
                float expectedLayerCount = quantifiableOverdraw.GetExpectedLayerCount(camera, relativeHeight, s_FurQuality);
                //屏占比
                //float furLength = quantifiableOverdraw.GetFurLength();
                //for(int j = 0; j < expectedLayerCount; j++)
                //{
                //    float weight = (j + 1) / expectedLayerCount;
                //    float screenOccupancy = Mathf.Min(1.0f, relativeHeight * (1.0f + furLength * 2)) * Mathf.Min(1.0f, relativeWidth * (1.0f + furLength * 2));
                //    overdraw += screenOccupancy * expectedLayerCount;
                //}
                float screenOccupancy = Mathf.Min(1.0f, relativeHeight) * Mathf.Min(1.0f, relativeWidth);
                overdraw += screenOccupancy * expectedLayerCount;
            }
            s_Camera2EstimatedOverdraw[camera] = overdraw;
        }

        /// <summary>
        /// 获取平衡后的毛发层数
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="quantifiableOverdraw"></param>
        /// <returns></returns>
        public static int GetBalancedLayerCount(Camera camera, IQuantifiableOverdraw quantifiableOverdraw)
        {
            if(s_Camera2EstimatedOverdraw.TryGetValue(camera, out float currentOverdraw))
            {
                float relativeHeight = quantifiableOverdraw.GetRelativeHeight(camera);
                float expectedLayerCount = quantifiableOverdraw.GetExpectedLayerCount(camera, relativeHeight, s_FurQuality);

                float balancedRate = Mathf.Min(1.0f, currentOverdraw > 0 ? (s_MaxOverdraw / currentOverdraw) : 1.0f);
                return Mathf.FloorToInt(balancedRate * expectedLayerCount);
            }
            return 0;
        }

        public static void Register(IQuantifiableOverdraw quantifiableOverdraw)
        {
            s_QuantifiableOverdrawObjects.Add(quantifiableOverdraw);
        }

        public static void Unregister(IQuantifiableOverdraw quantifiableOverdraw)
        {
            s_QuantifiableOverdrawObjects.Remove(quantifiableOverdraw);
        }
    }
}
