using UnityEngine;

namespace Aperture.Fur.Runtime
{
    public class ShaderProperties
    {
        public static readonly int VERTEX_BUFFER_NAME = Shader.PropertyToID("_VertexBuffer");
        public static readonly int VERTEX_BUFFER_STRIDE = Shader.PropertyToID("_VertexBufferStride");
        public static readonly int TEXCOORD_BUFFER_FORMATSIZE = Shader.PropertyToID("_TexcoordFormatSize");
        public static readonly int TEXCOORD_BUFFER_NAME = Shader.PropertyToID("_TexcoordBuffer");
        public static readonly int TEXCOORD_BUFFER_OFFSET = Shader.PropertyToID("_TexcoordBufferOffset");
        public static readonly int TEXCOORD_BUFFER_STRIDE = Shader.PropertyToID("_TexcoordBufferStride");
        public static readonly int COLOR_BUFFER_FORMATSIZE = Shader.PropertyToID("_ColorFormatSize");
        public static readonly int COLOR_BUFFER_NAME = Shader.PropertyToID("_ColorBuffer");
        public static readonly int COLOR_BUFFER_OFFSET = Shader.PropertyToID("_ColorBufferOffset");
        public static readonly int COLOR_BUFFER_STRIDE = Shader.PropertyToID("_ColorBufferStride");
        public static readonly int MATRIX_OBJECT_TO_WORLD = Shader.PropertyToID("_ObjectToWorld");
        public static readonly int MATRIX_WORLD_TO_OBJECT = Shader.PropertyToID("_WorldToObject");
        public static readonly int LAYER_COUNT = Shader.PropertyToID("_LayerCount");
        public static readonly int FUR_LENGTH = Shader.PropertyToID("_FurLength");
    }
}
