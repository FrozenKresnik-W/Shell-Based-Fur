using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Aperture.Fur.Runtime
{
    /// <summary>
    /// 使用Graphics.DrawProcedural将蒙皮后的顶点数据直接渲染出来
    /// 如果真机运行时出现了毛发无故消失的问题，可能是从Renderer中获取的VertexBuffer失效了
    /// 可以尝试在触发VertexBuffer失效的地方调用ReleaseVertexBuffer()重新获取新的VertexBuffer
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    public class SkinnedShellBasedFur : MonoBehaviour, IQuantifiableOverdraw
    {
        // ShellBased毛发材质
        public Material m_Material;

        private SkinnedMeshRenderer m_Renderer;
        private MaterialPropertyBlock m_MaterialPropertyBlock;

        // 从Mesh中获取的顶点索引
        private GraphicsBuffer m_IndexBuffer;

        // 从SkinnedMeshRenderer中获取的计算完蒙皮后的顶点数据
        private GraphicsBuffer m_RendererVertexBuffer;

        // 从Mesh中获取的UV数据
        private GraphicsBuffer m_TexcoordBuffer;
        // 从Mesh中获取的Color
        private GraphicsBuffer m_ColorBuffer;

        // 根据系统信息判断是否支持此效果
        private bool m_IsSupport;

        private void Awake()
        {
            m_IsSupport = SystemInfo.supportsInstancing && SystemInfo.supportsComputeShaders && SystemInfo.maxComputeBufferInputsVertex >= 3;

            m_Renderer = GetComponent<SkinnedMeshRenderer>();
            m_MaterialPropertyBlock = new MaterialPropertyBlock();
        }

        private void Start()
        {
            if (m_IsSupport)
            {
                m_Renderer.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                m_Renderer.sharedMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            }
        }

        private void OnEnable()
        {
            if (m_IsSupport)
            {
                RenderPipelineManager.beginFrameRendering += OnBeginFrameRendering;
                RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
#if UNITY_EDITOR
                RenderPipelineManager.endFrameRendering += OnEndFrameRendering;
#endif
                FurLayerBalancer.Register(this);
            }
        }

        private void OnDisable()
        {
            if (m_IsSupport)
            {
                FurLayerBalancer.Unregister(this);

                RenderPipelineManager.beginFrameRendering -= OnBeginFrameRendering;
                RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
#if UNITY_EDITOR
                RenderPipelineManager.endFrameRendering -= OnEndFrameRendering;
#endif
            }
            ReleaseBuffers();
        }

        private void ReleaseBuffers()
        {
            m_MaterialPropertyBlock?.Clear();

            m_IndexBuffer?.Dispose();
            m_IndexBuffer = null;

            m_TexcoordBuffer?.Dispose();
            m_TexcoordBuffer = null;

            m_ColorBuffer?.Dispose();
            m_ColorBuffer = null;

            ReleaseVertexBuffer();
        }

        public void ReleaseVertexBuffer()
        {
            m_RendererVertexBuffer?.Dispose();
            m_RendererVertexBuffer = null;
        }

        private void OnBeginFrameRendering(ScriptableRenderContext renderContext, Camera[] cameras)
        {
            if (m_Renderer.isVisible)
            {
                UpdateBuffers();
                UpdateMaterialPropertyBlock();
            }
        }

        private void OnBeginCameraRendering(ScriptableRenderContext renderContext, Camera camera)
        {
            if (m_Renderer.isVisible)
            {
                Render(camera);
            }
        }


#if UNITY_EDITOR
        /// <summary>
        /// 编辑器下每帧渲染完成就释放VertexBuffer
        /// </summary>
        /// <param name="renderContext"></param>
        /// <param name="cameras"></param>
        private void OnEndFrameRendering(ScriptableRenderContext renderContext, Camera[] cameras)
        {
            ReleaseVertexBuffer();
        }
#endif

        private void UpdateBuffers()
        {
            BindRendererVertexBuffer();

            Mesh mesh = m_Renderer.sharedMesh;
            if (mesh != null)
            {
                // Index Buffer
                if (m_IndexBuffer == null)
                {
                    m_IndexBuffer = mesh.GetIndexBuffer();
                }

                if (mesh.HasVertexAttribute(VertexAttribute.Color))
                {
                    //Color
                    BindMeshVertexBuffer(mesh, VertexAttribute.Color, ref m_ColorBuffer, ShaderProperties.COLOR_BUFFER_NAME, ShaderProperties.COLOR_BUFFER_STRIDE, ShaderProperties.COLOR_BUFFER_OFFSET, ShaderProperties.COLOR_BUFFER_FORMATSIZE);
                }

                if (mesh.HasVertexAttribute(VertexAttribute.TexCoord0))
                {
                    // UV
                    BindMeshVertexBuffer(mesh, VertexAttribute.TexCoord0, ref m_TexcoordBuffer, ShaderProperties.TEXCOORD_BUFFER_NAME, ShaderProperties.TEXCOORD_BUFFER_STRIDE, ShaderProperties.TEXCOORD_BUFFER_OFFSET, ShaderProperties.TEXCOORD_BUFFER_FORMATSIZE);
                }
            }
        }

        private void BindRendererVertexBuffer()
        {
            //编辑器下任何SkinnedMeshRenderer的改动，甚至修改Shader触发编译都会导致VertexBuffer失效
            //这对材质调试带来了极大的不便，但又没有找到一个方法可以提前判断
            //所以编辑器模式下每帧获取新的VertexBuffer,注意这会导致每帧都产生GCAlloc
#if !UNITY_EDITOR
        if (m_RendererVertexBuffer == null)
#endif
            {
                m_RendererVertexBuffer = m_Renderer.GetVertexBuffer();
                m_MaterialPropertyBlock.SetBuffer(ShaderProperties.VERTEX_BUFFER_NAME, m_RendererVertexBuffer);
            }

            if (m_RendererVertexBuffer != null)
            {
                m_MaterialPropertyBlock.SetInteger(ShaderProperties.VERTEX_BUFFER_STRIDE, m_RendererVertexBuffer.stride);
            }
            else
            {
                m_MaterialPropertyBlock.SetInteger(ShaderProperties.VERTEX_BUFFER_STRIDE, 0);
            }
        }

        private void BindMeshVertexBuffer(Mesh mesh, VertexAttribute vertexAttribute, ref GraphicsBuffer buffer, int bufferNameID, int strideNameID, int offsetNameID, int sizeNameID)
        {
            if (buffer == null)
            {
                int vertexAttributeStream = mesh.GetVertexAttributeStream(vertexAttribute);
                if (vertexAttributeStream != -1)
                {
                    buffer = mesh.GetVertexBuffer(vertexAttributeStream);
                    m_MaterialPropertyBlock.SetBuffer(bufferNameID, buffer);
                }

                if (buffer != null)
                {
                    VertexAttributeFormat format = mesh.GetVertexAttributeFormat(vertexAttribute);
                    int formatSize = GetFormatSize(format);
                    m_MaterialPropertyBlock.SetInteger(sizeNameID, formatSize);

                    int vertexAttributeOffset = mesh.GetVertexAttributeOffset(vertexAttribute);
                    m_MaterialPropertyBlock.SetInteger(offsetNameID, vertexAttributeOffset);

                    m_MaterialPropertyBlock.SetInteger(strideNameID, buffer.stride);
                }
                else
                {
                    m_MaterialPropertyBlock.SetInteger(sizeNameID, 0);
                    m_MaterialPropertyBlock.SetInteger(offsetNameID, 0);
                    m_MaterialPropertyBlock.SetInteger(strideNameID, 0);
                }
            }
        }

        private int GetFormatSize(VertexAttributeFormat format)
        {
            switch (format)
            {
                case VertexAttributeFormat.Float32:
                case VertexAttributeFormat.UInt32:
                case VertexAttributeFormat.SInt32:
                    return 32;
                case VertexAttributeFormat.Float16:
                case VertexAttributeFormat.UNorm16:
                case VertexAttributeFormat.SNorm16:
                case VertexAttributeFormat.UInt16:
                case VertexAttributeFormat.SInt16:
                    return 16;
                case VertexAttributeFormat.UNorm8:
                case VertexAttributeFormat.SNorm8:
                case VertexAttributeFormat.UInt8:
                case VertexAttributeFormat.SInt8:
                    return 8;
                default:
                    return 0;
            }
        }

        private void UpdateMaterialPropertyBlock()
        {
            if (m_Renderer.rootBone != null)
            {
                Matrix4x4 localToWorldMatrix = Matrix4x4.identity;
                localToWorldMatrix.SetTRS(m_Renderer.rootBone.localToWorldMatrix.GetPosition(), m_Renderer.rootBone.localToWorldMatrix.rotation, Vector3.one);
                m_MaterialPropertyBlock.SetMatrix(ShaderProperties.MATRIX_OBJECT_TO_WORLD, localToWorldMatrix);
                m_MaterialPropertyBlock.SetMatrix(ShaderProperties.MATRIX_WORLD_TO_OBJECT, Matrix4x4.Inverse(localToWorldMatrix));
            }
            else
            {
                m_MaterialPropertyBlock.SetMatrix(ShaderProperties.MATRIX_OBJECT_TO_WORLD, Matrix4x4.identity);
                m_MaterialPropertyBlock.SetMatrix(ShaderProperties.MATRIX_WORLD_TO_OBJECT, Matrix4x4.identity);
            }
        }

        private void Render(Camera camera)
        {
            if (m_Material != null && m_IndexBuffer != null && m_RendererVertexBuffer != null && m_TexcoordBuffer != null)
            {
                int layerCount = FurLayerBalancer.GetBalancedLayerCount(camera, this);
                if(layerCount > 0)
                {
                    m_MaterialPropertyBlock.SetFloat(ShaderProperties.LAYER_COUNT, layerCount);
                    Graphics.DrawProcedural(m_Material, m_Renderer.bounds, MeshTopology.Triangles, m_IndexBuffer, m_IndexBuffer.count, layerCount, camera, m_MaterialPropertyBlock, ShadowCastingMode.Off, m_Renderer.receiveShadows, gameObject.layer);
                }
            }
        }

        public float GetRelativeHeight(Camera camera)
        {
            if(m_Renderer.isVisible)
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

