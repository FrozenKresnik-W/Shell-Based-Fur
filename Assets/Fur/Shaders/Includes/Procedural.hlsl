#ifndef _PROCEDURAL_
#define _PROCEDURAL_

uint _VertexBufferStride;
ByteAddressBuffer _VertexBuffer;

uint _TexcoordFormatSize;
uint _TexcoordBufferStride;
uint _TexcoordBufferOffset;
ByteAddressBuffer _TexcoordBuffer;

uint _ColorFormatSize;
uint _ColorBufferStride;
uint _ColorBufferOffset;
ByteAddressBuffer _ColorBuffer;

float3 GetVertexData_Position(uint vid)
{
    uint vidx = vid * _VertexBufferStride;
    float3 data = asfloat(_VertexBuffer.Load3(vidx));
    return data;
}

float3 GetVertexData_Normal(uint vid)
{
    uint vidx = vid * _VertexBufferStride;
    float3 data = asfloat(_VertexBuffer.Load3(vidx + 12)); //offset by float3 (position) in front, so 3*4bytes = 12
    return data;
}

float4 GetVertexData_Tangent(uint vid)
{
    uint vidx = vid * _VertexBufferStride;
    float4 data = asfloat(_VertexBuffer.Load4(vidx + 24)); //offset by float3 (position) + float3 (normal) in front, so 12 + 3*4bytes = 24
    return data;
}

real2 GetVertexData_TexCoord0(uint vid)
{
    real2 data = 0;
    uint vidx = vid * _TexcoordBufferStride + _TexcoordBufferOffset;

    //Vertex data maybe compressed on mobile device
    //so we decompress the buffer data here if format size is 16
    if (_TexcoordFormatSize == 16)
    {
        uint raw = _TexcoordBuffer.Load(vidx);
        uint high = (raw & 0xFFFF0000) >> 16;
        uint low = raw & 0x0000FFFF;
        data = real2(f16tof32(low), f16tof32(high));
    }
    else
    {
        uint2 raw = _TexcoordBuffer.Load2(vidx);
        data = asfloat(raw);
    }

    return data;
}

half4 GetVertexData_Color(uint vid)
{
    half4 color = half4(0, 0, 0, 1);
    uint vidx = vid * _ColorBufferStride + _ColorBufferOffset;

    if (_ColorFormatSize > 0)
    {
        uint raw = _ColorBuffer.Load(vidx);
        uint4 data = uint4(raw & 0x000000FF, (raw & 0x0000FF00) >> 8, (raw & 0x00FF0000) >> 16, (raw & 0xFF000000) >> 24);
        color = data * 0.003921h;//color / 255,0
    }
    return color;
}

#endif