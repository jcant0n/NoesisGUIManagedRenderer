﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct Shader
{
    // List of shaders to be implemented by the device with expected vertex format
    //
    //  Name       Format                   Size (bytes)      Semantic
    //  ---------------------------------------------------------------------------------
    //  Pos        R32G32_FLOAT             8                 Position (x,y)
    //  Color      R8G8B8A8_UNORM           4                 Color (rgba)
    //  Tex0       R32G32_FLOAT             8                 Texture (u,v)
    //  Tex1       R32G32_FLOAT             8                 Texture (u,v)
    //  Tex2       R16G16B16A16_UNORM       8                 Rect (x0,y0, x1,y1)
    //  Coverage   R32_FLOAT                4                 Coverage (x)
    //
    public enum Enum
    {
        RGBA,                       // Pos
        Mask,                       // Pos

        Path_Solid,                 // Pos | Color
        Path_Linear,                // Pos | Tex0
        Path_Radial,                // Pos | Tex0
        Path_Pattern,               // Pos | Tex0

        PathAA_Solid,               // Pos | Color | Coverage
        PathAA_Linear,              // Pos | Tex0  | Coverage
        PathAA_Radial,              // Pos | Tex0  | Coverage
        PathAA_Pattern,             // Pos | Tex0  | Coverage

        SDF_Solid,                  // Pos | Color | Tex1
        SDF_Linear,                 // Pos | Tex0  | Tex1
        SDF_Radial,                 // Pos | Tex0  | Tex1
        SDF_Pattern,                // Pos | Tex0  | Tex1

        SDF_LCD_Solid,              // Pos | Color | Tex1
        SDF_LCD_Linear,             // Pos | Tex0  | Tex1
        SDF_LCD_Radial,             // Pos | Tex0  | Tex1
        SDF_LCD_Pattern,            // Pos | Tex0  | Tex1

        Image_Opacity_Solid,        // Pos | Color | Tex1
        Image_Opacity_Linear,       // Pos | Tex0  | Tex1
        Image_Opacity_Radial,       // Pos | Tex0  | Tex1
        Image_Opacity_Pattern,      // Pos | Tex0  | Tex1

        Image_Shadow35V,            // Pos | Color | Tex1 | Tex2
        Image_Shadow63V,            // Pos | Color | Tex1 | Tex2
        Image_Shadow127V,           // Pos | Color | Tex1 | Tex2

        Image_Shadow35H_Solid,      // Pos | Color | Tex1 | Tex2
        Image_Shadow35H_Linear,     // Pos | Tex0  | Tex1 | Tex2
        Image_Shadow35H_Radial,     // Pos | Tex0  | Tex1 | Tex2
        Image_Shadow35H_Pattern,    // Pos | Tex0  | Tex1 | Tex2

        Image_Shadow63H_Solid,      // Pos | Color | Tex1 | Tex2
        Image_Shadow63H_Linear,     // Pos | Tex0  | Tex1 | Tex2
        Image_Shadow63H_Radial,     // Pos | Tex0  | Tex1 | Tex2
        Image_Shadow63H_Pattern,    // Pos | Tex0  | Tex1 | Tex2

        Image_Shadow127H_Solid,     // Pos | Color | Tex1 | Tex2
        Image_Shadow127H_Linear,    // Pos | Tex0  | Tex1 | Tex2
        Image_Shadow127H_Radial,    // Pos | Tex0  | Tex1 | Tex2
        Image_Shadow127H_Pattern,   // Pos | Tex0  | Tex1 | Tex2

        Image_Blur35V,              // Pos | Color | Tex1 | Tex2
        Image_Blur63V,              // Pos | Color | Tex1 | Tex2
        Image_Blur127V,             // Pos | Color | Tex1 | Tex2

        Image_Blur35H_Solid,        // Pos | Color | Tex1 | Tex2
        Image_Blur35H_Linear,       // Pos | Tex0  | Tex1 | Tex2
        Image_Blur35H_Radial,       // Pos | Tex0  | Tex1 | Tex2
        Image_Blur35H_Pattern,      // Pos | Tex0  | Tex1 | Tex2

        Image_Blur63H_Solid,        // Pos | Color | Tex1 | Tex2
        Image_Blur63H_Linear,       // Pos | Tex0  | Tex1 | Tex2
        Image_Blur63H_Radial,       // Pos | Tex0  | Tex1 | Tex2
        Image_Blur63H_Pattern,      // Pos | Tex0  | Tex1 | Tex2

        Image_Blur127H_Solid,       // Pos | Color | Tex1 | Tex2
        Image_Blur127H_Linear,      // Pos | Tex0  | Tex1 | Tex2
        Image_Blur127H_Radial,      // Pos | Tex0  | Tex1 | Tex2
        Image_Blur127H_Pattern,     // Pos | Tex0  | Tex1 | Tex2

        Count
    };

    

    byte v;
}

// Render batch information
[StructLayout(LayoutKind.Explicit, Size = 184)]
public unsafe struct Batch
{
    // Render state
    [FieldOffset(0)] public byte shader;
    [FieldOffset(1)] public byte renderState;
    [FieldOffset(2)] public byte stencilRef;

    // Draw parameters
    [FieldOffset(4)] public UInt32 vertexOffset;
    [FieldOffset(8)] public UInt32 numVertices;
    [FieldOffset(12)] public UInt32 startIndex;
    [FieldOffset(16)] public UInt32 numIndices;

    // Textures (Unused textures are set to null)
    /*Texture pattern;
    byte patternSampler;

    Texture ramps;
    byte rampsSampler;

    Texture image;
    byte imageSampler;

    Texture glyphs;
    byte glyphsSampler;

    Texture shadow;
    byte shadowSampler;

    // Effect parameters
    IntPtr effectParams;
    UInt32 effectParamsSize;
    UInt32 effectParamsHash;

    // Shader constants (Unused constants are set to null)
    fixed IntPtr projMtx[16];
    UInt32 projMtxHash;

    const float* opacity;
    UInt32 opacityHash;

    const float (*rgba)[4];
    UInt32 rgbaHash;

    const float (*radialGrad)[8];
    UInt32 radialGradHash;*/
};


public abstract class ManagedRenderDevice
{
    private const string LIB_NOESIS = "../../../../IntegrationGLUT/Projects/windows_x86_64/x64/Debug/IntegrationGLUT.dll";

    public delegate void SetDrawBatchCallbackDelegate(ref Batch batch);
    public delegate IntPtr SetMapVerticesCallbackDelegate(UInt32 size);
    public delegate void SetUnmapVerticesCallbackDelegate();
    public delegate IntPtr SetMapIndicesCallbackDelegate(UInt32 size);
    public delegate void SetUnmapIndicesCallbackDelegate();

    [DllImport(LIB_NOESIS, CallingConvention = CallingConvention.Winapi)]
    private static extern void SetDrawBatchCallback(SetDrawBatchCallbackDelegate callback);

    [DllImport(LIB_NOESIS, CallingConvention = CallingConvention.Winapi)]
    private static extern void SetMapVerticesCallback(SetMapVerticesCallbackDelegate callback);

    [DllImport(LIB_NOESIS, CallingConvention = CallingConvention.Winapi)]
    private static extern void SetUnmapVerticesCallback(SetUnmapVerticesCallbackDelegate callback);

    [DllImport(LIB_NOESIS, CallingConvention = CallingConvention.Winapi)]
    private static extern void SetMapIndicesCallback(SetMapIndicesCallbackDelegate callback);

    [DllImport(LIB_NOESIS, CallingConvention = CallingConvention.Winapi)]
    private static extern void SetUnmapIndicesCallback(SetUnmapIndicesCallbackDelegate callback);

    public static void SetMamanagedRenderDevice(ManagedRenderDevice renderDevice)
    {
        SetDrawBatchCallback(renderDevice.DrawBatch);
        SetMapVerticesCallback(renderDevice.MapVertices);
        SetUnmapVerticesCallback(renderDevice.UnmapVertices);
        SetMapIndicesCallback(renderDevice.MapIndices);
        SetUnmapIndicesCallback(renderDevice.UnmapIndices);
    }

    public abstract void DrawBatch(ref Batch batch);
    public abstract IntPtr MapVertices(UInt32 size);
    public abstract void UnmapVertices();
    public abstract IntPtr MapIndices(UInt32 size);
    public abstract void UnmapIndices();
}

