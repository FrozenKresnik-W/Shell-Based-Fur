using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Aperture.Fur.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class StaticShellBasedFur : MonoBehaviour, IQuantifiableOverdraw
    {
        // ShellBased毛发材质
        public Material m_Material;

        private MeshFilter m_MeshFilter;
        private MeshRenderer m_Renderer;
        private MaterialPropertyBlock m_MaterialPropertyBlock;

        // 根据系统信息判断是否支持此效果
        private bool m_IsSupport;

        private void Awake()
        {
            m_IsSupport = SystemInfo.supportsInstancing && SystemInfo.supportsComputeShaders;

            m_MeshFilter = GetComponent<MeshFilter>();
            m_Renderer = GetComponent<MeshRenderer>();
            m_MaterialPropertyBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            if (m_IsSupport)
            {
                RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;

                FurLayerBalancer.Register(this);
            }
        }

        private void OnDisable()
        {
            if (m_IsSupport)
            {
                FurLayerBalancer.Unregister(this);

                RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            }
        }

        private void OnBeginCameraRendering(ScriptableRenderContext renderContext, Camera camera)
        {
            if (m_Renderer.isVisible)
            {
                Render(camera);
            }
        }

        private void LateUpdate()
        {
            if (m_Renderer.isVisible)
            {
                m_MaterialPropertyBlock.SetMatrix(ShaderProperties.MATRIX_OBJECT_TO_WORLD, m_Renderer.localToWorldMatrix);
                m_MaterialPropertyBlock.SetMatrix(ShaderProperties.MATRIX_WORLD_TO_OBJECT, m_Renderer.worldToLocalMatrix);
            }
        }

        private void Render(Camera camera)
        {
            int layerCount = FurLayerBalancer.GetBalancedLayerCount(camera, this);
            if(layerCount > 0)
            {
                m_MaterialPropertyBlock.SetFloat(ShaderProperties.LAYER_COUNT, layerCount);

                Mesh mesh = m_MeshFilter.sharedMesh;
                if (m_Material != null && mesh != null)
                {
                    for (int i = 0; i < mesh.subMeshCount; i++)
                    {
                        Graphics.DrawMeshInstancedProcedural(mesh, i, m_Material, m_Renderer.bounds, layerCount,
                            m_MaterialPropertyBlock, ShadowCastingMode.Off, m_Renderer.receiveShadows,
                            gameObject.layer, camera, m_Renderer.lightProbeUsage);
                    }
                }
            }
        }

        //计算相对高度
        public float GetRelativeHeight(Camera camera)
        {
            if (m_Renderer.isVisible)
            {
                float distance = (m_Renderer.bounds.center - camera.transform.position).magnitude;
                float size = Mathf.Max(Mathf.Max(m_Renderer.bounds.size.x, m_Renderer.bounds.size.y), m_Renderer.bounds.size.z);

                return IQuantifiableOverdraw.DistanceToRelativeHeight(camera, distance, size);
            }
            return 0.0f;
        }

        public float GetFurLength()
        {
            if (m_Material != null && m_Material.HasProperty(ShaderProperties.FUR_LENGTH))
            {
                return m_Material.GetFloat(ShaderProperties.FUR_LENGTH);
            }
            return 0.0f;
        }
    }
}