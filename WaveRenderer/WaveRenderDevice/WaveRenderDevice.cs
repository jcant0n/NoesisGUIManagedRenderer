﻿using System;
using WaveEngine.Common.Graphics;
using WaveEngine.Mathematics;
using WaveEngine.Platform;
using VisualTests.Runners.Common;
using Buffer = WaveEngine.Common.Graphics.Buffer;
using NoesisManagedRenderer;
using System.Threading.Tasks;

namespace WaveRenderer.WaveRenderDevice
{
    public class WaveRenderDevice : ManagedRenderDevice
    {
        GraphicsPipelineState[] graphicPipelineStates = new GraphicsPipelineState[Enum.GetNames(typeof(NoesisShader.Enum)).Length];

        ResourceSet[] resourceSets = new ResourceSet[Enum.GetNames(typeof(NoesisShader.Enum)).Length];

        public CommandBuffer commandBuffer;

        public GraphicsContext GraphicsContext { get; }

        //Buffers
        Buffer vertexBuffer;
        Buffer indexBuffer;
        Buffer vertexCB;
        Buffer pixelCB;
        Buffer effectCB;
        Buffer texDimensionsCB;

        uint vertexCBHash;
        uint pixelCBHash;
        uint effectCBHash;
        uint texDimensionsCBHash;

        MappedResource vertexBufferWritableResource;
        MappedResource indexBufferWritableResource;

        public WaveRenderDevice(GraphicsContext graphicsContext)
            : base(new NoesisDeviceCaps(), flippedTextures: false)
        {
            this.GraphicsContext = graphicsContext;

            CreateBuffers();
        }

        private void CreateBuffers()
        {
            vertexCBHash = 0;
            pixelCBHash = 0;
            effectCBHash = 0;
            texDimensionsCBHash = 0;

            var bufferDescription = new BufferDescription(DYNAMIC_VB_SIZE, BufferFlags.VertexBuffer, ResourceUsage.Dynamic, ResourceCpuAccess.Write);
            vertexBuffer = this.GraphicsContext.Factory.CreateBuffer(ref bufferDescription);

            bufferDescription = new BufferDescription(DYNAMIC_IB_SIZE, BufferFlags.IndexBuffer, ResourceUsage.Dynamic, ResourceCpuAccess.Write);
            indexBuffer = this.GraphicsContext.Factory.CreateBuffer(ref bufferDescription);

            bufferDescription = new BufferDescription(16 * sizeof(float), BufferFlags.ConstantBuffer, ResourceUsage.Default);
            vertexCB = this.GraphicsContext.Factory.CreateBuffer(ref bufferDescription);

            bufferDescription = new BufferDescription(12 * sizeof(float), BufferFlags.ConstantBuffer, ResourceUsage.Default);
            pixelCB = this.GraphicsContext.Factory.CreateBuffer(ref bufferDescription);

            bufferDescription = new BufferDescription(16 * sizeof(float), BufferFlags.ConstantBuffer, ResourceUsage.Default);
            effectCB = this.GraphicsContext.Factory.CreateBuffer(ref bufferDescription);

            bufferDescription = new BufferDescription(4 * sizeof(float), BufferFlags.ConstantBuffer, ResourceUsage.Default);
            texDimensionsCB = this.GraphicsContext.Factory.CreateBuffer(ref bufferDescription);
        }

        public async Task InitializeAsync(AssetsDirectory assetsDirectory, FrameBuffer frameBuffer)
        {
            for (int i = 0; i < NoesisShader.Formats.Length; ++i)
            {
                await this.InitGraphicsPipelineStateAsync(i, assetsDirectory, frameBuffer);
            }
        }

        private async Task InitGraphicsPipelineStateAsync(int shader, AssetsDirectory assetsDirectory, FrameBuffer frameBuffer)
        {
            InputLayouts vertexLayouts = new InputLayouts();
            LayoutDescription layoutDescription = new LayoutDescription();
            vertexLayouts.Add(layoutDescription);

            int format = NoesisShader.Formats[shader];

            string vertexShaderPath = "";

            if ((format & NoesisShader.Pos) != 0)
            {
                layoutDescription.Add(new ElementDescription(ElementFormat.Float2, ElementSemanticType.Position));
                vertexShaderPath += "Pos";
            }
            if ((format & NoesisShader.Color) != 0)
            {
                layoutDescription.Add(new ElementDescription(ElementFormat.UByte4Normalized, ElementSemanticType.Color));
                vertexShaderPath += "Color";
            }
            if ((format & NoesisShader.Tex0) != 0)
            {
                layoutDescription.Add(new ElementDescription(ElementFormat.Float2, ElementSemanticType.TexCoord, 0));
                vertexShaderPath += "Tex0";
            }
            if ((format & NoesisShader.Tex1) != 0)
            {
                layoutDescription.Add(new ElementDescription(ElementFormat.Float2, ElementSemanticType.TexCoord, 1));
                vertexShaderPath += "Tex1";
            }
            if ((format & NoesisShader.Tex2) != 0)
            {
                layoutDescription.Add(new ElementDescription(ElementFormat.Float2, ElementSemanticType.TexCoord, 2));
                vertexShaderPath += "Tex2";
            }
            if ((format & NoesisShader.Coverage) != 0)
            {
                layoutDescription.Add(new ElementDescription(ElementFormat.Float, ElementSemanticType.TexCoord, 3));
                vertexShaderPath += "Coverage";
            }
            if ((format & NoesisShader.SDF) != 0)
            {
                vertexShaderPath += "_SDF";
            }
            vertexShaderPath += "_VS";

            string pixelShaderPath = ((NoesisShader.Enum)shader).ToString() + "_FS";
            string vsEntryPoint = "main";
            string psEntryPoint = "main";
            if (shader != 6 && shader != 7 && shader != 8 && shader != 10)
            {
                //TODO: Include this shaders
                vertexShaderPath = "HLSLVertex";
                pixelShaderPath = "HLSLVertex";
                vsEntryPoint = "VS";
                psEntryPoint = "PS";
            }

            var vertexShaderDescription = await assetsDirectory.ReadAndCompileShader(this.GraphicsContext, vertexShaderPath, "VertexShader", ShaderStages.Vertex, vsEntryPoint);
            var pixelShaderDescription = await assetsDirectory.ReadAndCompileShader(this.GraphicsContext, pixelShaderPath, "FragmentShader", ShaderStages.Pixel, psEntryPoint);
            var vertexShader = this.GraphicsContext.Factory.CreateShader(ref vertexShaderDescription);
            var pixelShader = this.GraphicsContext.Factory.CreateShader(ref pixelShaderDescription);

            var resourceLayoutDescription = new ResourceLayoutDescription(
                new LayoutElementDescription(0, ResourceType.ConstantBuffer, ShaderStages.Vertex),
                new LayoutElementDescription(1, ResourceType.ConstantBuffer, ShaderStages.Vertex),
                new LayoutElementDescription(0, ResourceType.ConstantBuffer, ShaderStages.Pixel)
            );

            var resourceLayout = this.GraphicsContext.Factory.CreateResourceLayout(ref resourceLayoutDescription);

            var resourceSetDescription = new ResourceSetDescription(
                resourceLayout, vertexCB, texDimensionsCB, pixelCB
            );

            resourceSets[shader] = this.GraphicsContext.Factory.CreateResourceSet(ref resourceSetDescription);

            var pipelineDescription = new GraphicsPipelineDescription()
            {
                PrimitiveTopology = PrimitiveTopology.TriangleList,
                InputLayouts = vertexLayouts,
                ResourceLayouts = new[] { resourceLayout },
                Shaders = new GraphicsShaderStateDescription()
                {
                    VertexShader = vertexShader,
                    PixelShader = pixelShader,
                },
                RenderStates = new RenderStateDescription()
                {
                    RasterizerState = RasterizerStates.None,
                    BlendState = BlendStates.AlphaBlend,
                    DepthStencilState = DepthStencilStates.None,
                },
                Outputs = frameBuffer.OutputDescription,
            };

            graphicPipelineStates[shader] = this.GraphicsContext.Factory.CreateGraphicsPipeline(ref pipelineDescription);
        }

        unsafe protected override void DrawBatch(ref NoesisBatch batch)
        {
            this.SetShaders(batch);
            this.SetBuffers(batch);

            //TODO: SetRenderState

            this.SetTextures(batch);

            //Draw
            commandBuffer.DrawIndexed(batch.numIndices, batch.startIndex);
        }

        private unsafe void SetBuffers(NoesisBatch batch)
        {
            //Set Index Buffer
            commandBuffer.SetIndexBuffer(indexBuffer);

            //Set Vertex Buffer
            commandBuffer.SetVertexBuffer(0, vertexBuffer, batch.vertexOffset);

            // Vertex Constants
            if (vertexCBHash != batch.projMtxHash)
            {
                Matrix4x4 prjMtx = Matrix4x4.Transpose(*(Matrix4x4*)batch.projMtx);
                commandBuffer.UpdateBufferData(this.vertexCB, ref prjMtx);
                vertexCBHash = batch.projMtxHash;
            }

            // Pixel Constants
            if (batch.rgba != IntPtr.Zero || batch.radialGrad != IntPtr.Zero || batch.opacity != IntPtr.Zero)
            {
                uint hash = batch.rgbaHash ^ batch.radialGradHash ^ batch.opacityHash;
                if (pixelCBHash != hash)
                {
                    float[] pixelData = new float[12];
                    int idx = 0;

                    if (batch.rgba != IntPtr.Zero)
                    {
                        for (int i = 0; i < 4; ++i)
                        {
                            pixelData[idx++] = ((float*)batch.rgba)[i];
                        }
                    }

                    if (batch.radialGrad != IntPtr.Zero)
                    {
                        for (int i = 0; i < 8; ++i)
                        {
                            pixelData[idx++] = ((float*)batch.radialGrad)[i];
                        }
                    }

                    if (batch.opacity != IntPtr.Zero)
                    {
                        pixelData[idx++] = ((float*)batch.opacity)[0];
                    }

                    commandBuffer.UpdateBufferData(this.pixelCB, pixelData);
                    pixelCBHash = hash;
                }
            }

            // Texture dimensions
            if (batch.Glyphs != null || batch.Image != null)
            {
                var texture = batch.Glyphs ?? batch.Image;
                uint hash = texture.Width << 16 | texture.Height;
                if (texDimensionsCBHash != hash)
                {
                    Vector4 data = new Vector4(texture.Width, texture.Height, 1f / texture.Width, 1f / texture.Height);
                    commandBuffer.UpdateBufferData(this.texDimensionsCB, ref data);
                    texDimensionsCBHash = hash;
                }
            }

            //Effects
            if (batch.effectParamsSize != 0)
            {
                if (effectCBHash != batch.effectParamsHash)
                {
                    float[] effectData = new float[16];
                    for (int i = 0; i < batch.effectParamsSize; ++i)
                    {
                        effectData[i] = ((float*)batch.effectParams)[i];
                    }
                    commandBuffer.UpdateBufferData(this.effectCB, effectData);
                    effectCBHash = batch.effectParamsHash;
                }
            }
        }

        private void SetShaders(NoesisBatch batch)
        {
            commandBuffer.SetGraphicsPipelineState(graphicPipelineStates[batch.shader.v]);
            commandBuffer.SetResourceSet(resourceSets[batch.shader.v]);
        }

        private unsafe void SetTextures(NoesisBatch batch)
        {
            this.SetTexture((WaveTexture)batch.Pattern, 0, batch.patternSampler);
            this.SetTexture((WaveTexture)batch.Ramps, 1, batch.rampsSampler);
            this.SetTexture((WaveTexture)batch.Image, 2, batch.imageSampler);
            this.SetTexture((WaveTexture)batch.Glyphs, 3, batch.glyphsSampler);
            this.SetTexture((WaveTexture)batch.Shadow, 4, batch.shadowSampler);
        }

        private void SetTexture(WaveTexture texture, uint slot, byte sampler)
        {
            if (texture != null)
            {
                if (texture.resourceSet == null)
                {
                    texture.SetResourceSet(slot, sampler);
                }

                commandBuffer.SetResourceSet(texture.resourceSet);
            }
        }

        unsafe protected override IntPtr MapVertices(UInt32 bytes)
        {
            vertexBufferWritableResource = this.GraphicsContext.MapMemory(vertexBuffer, MapMode.Write);
            return vertexBufferWritableResource.Data;
        }

        unsafe protected override void UnmapVertices()
        {
            this.GraphicsContext.UnmapMemory(vertexBuffer);
        }

        unsafe protected override IntPtr MapIndices(uint bytes)
        {
            indexBufferWritableResource = this.GraphicsContext.MapMemory(indexBuffer, MapMode.Write);
            return indexBufferWritableResource.Data;
        }

        unsafe protected override void UnmapIndices()
        {
            this.GraphicsContext.UnmapMemory(indexBuffer);
        }

        protected override void BeginRender(bool offscreen)
        {
        }

        protected override void EndRender()
        {
        }

        protected override ManagedTexture CreateTexture(uint width, uint height, uint numLevels, ref NoesisTextureFormat format)
        {
            return WaveTexture.Create(this.GraphicsContext, width, height, numLevels, ref format, null);
        }

        protected override ManagedRenderTarget CreateRenderTarget(uint width, uint height, uint sampleCount)
        {
            return new WaveRenderTarget(this.GraphicsContext, width, height, sampleCount);
        }

        protected override ManagedRenderTarget CloneRenderTarget(ManagedRenderTarget surface)
        {
            throw new NotImplementedException();
        }

        protected override void SetRenderTarget(ManagedRenderTarget surface)
        {
            throw new NotImplementedException();
        }

        protected override void BeginTile(ref NoesisTile tile, uint surfaceWidth, uint surfaceHeight)
        {
            throw new NotImplementedException();
        }

        protected override void EndTile()
        {
            throw new NotImplementedException();
        }

        protected override void ResolveRenderTarget(ManagedRenderTarget surface, NoesisTile[] tiles)
        {
            throw new NotImplementedException();
        }
    }
}
