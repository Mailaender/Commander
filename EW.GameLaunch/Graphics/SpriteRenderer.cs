﻿using System;
using System.Drawing;
using EW.OpenGLES.Graphics;
using EW.OpenGLES;
namespace EW.Graphics
{
    /// <summary>
    /// Sprite renderer.
    /// 精灵渲染器
    /// </summary>
    public class SpriteRenderer:Renderer.IBatchRenderer
    {
        readonly Renderer renderer;

        readonly Action renderAction;

        readonly IShader shader;

        readonly Vertex[] vertices;

        Sheet currentSheet;

        BlendMode currentBlend = BlendMode.Alpha;

        int nv = 0;

        public SpriteRenderer(Renderer renderer,IShader shader)
        {
            this.renderer = renderer;
            this.shader = shader;
            vertices = new Vertex[renderer.TempBufferSize];
            renderAction = () => renderer.DrawBatch(vertices, nv, PrimitiveType.TriangleList);
        }


        void SetRenderStateForSprite(Sprite s){

            currentBlend = s.BlendMode;
            currentSheet = s.Sheet;

        }
        public void DrawVertexBuffer(IVertexBuffer<Vertex> buffer,int start,int length,PrimitiveType type,Sheet sheet,BlendMode blendMode)
        {
            shader.SetTexture("DiffuseTexture", sheet.GetTexture());
            renderer.Device.SetBlendMode(blendMode);
            shader.Render(() => renderer.DrawBatch(buffer, start, length, type));
            renderer.Device.SetBlendMode(BlendMode.None);
        }


        public void DrawSprite(Sprite s,Vector3 location,PaletteReference pal){
            
            DrawSprite(s,location,pal.TextureIndex,s.Size);
        }

        public void DrawSprite(Sprite s,Vector3 location,PaletteReference pal,Vector3 size){

            DrawSprite(s,location,pal.TextureIndex,size);
        }

        public void DrawSprite(Sprite s, Vector3 location, float paletteTextureIndex,Vector3 size){

            SetRenderStateForSprite(s);
            Util.FastCreateQuad(vertices, location + s.FractionalOffset*size,s,paletteTextureIndex,nv,size);
            nv += 6;
        }



        public void SetPalette(ITexture palette)
        {
            //effect.Parameters["Palette"].SetValue(palette);
            shader.SetTexture("Palette", palette);
        }

        public void SetViewportParams(Size screen,float depthScale,float depthOffset,float zoom,Int2 scroll)
        {
            //effect.Parameters["Scroll"].SetValue(new Vector3(scroll.X, scroll.Y, scroll.Y));
            //effect.Parameters["r1"].SetValue(new Vector3(zoom * 2f / screen.Width, -zoom * 2f / screen.Height, -depthScale * zoom / screen.Height));
            //effect.Parameters["r2"].SetValue(new Vector3(-1, 1, 1 - depthOffset));
            shader.SetVec("Scroll", scroll.X, scroll.Y, scroll.Y);
            shader.SetVec("r1", zoom * 2f / screen.Width,
                    -zoom * 2f / screen.Height,
                    -depthScale * zoom / screen.Height);
            shader.SetVec("r2", -1, 1, 1 - depthOffset);

            //Texture index is sampled as a float,so convert to pixels then scale.
            //纹理索引被采样为浮点数，因此转换为像素然后缩放
            //effect.Parameters["DepthTextureScale"].SetValue(128 * depthScale * zoom / screen.Height);
            shader.SetVec("DepthTextureScale", 128 * depthScale * zoom / screen.Height);
        }

        public void SetDepthPreviewEnabled(bool enabled)
        {
            //effect.Parameters["EnableDepthPreview"].SetValue(enabled);
            shader.SetBool("EnableDepthPreview", enabled);
        }

        public void Flush()
        {
            if(nv > 0)
            {
                //effect.Parameters["DiffuseTexture"].SetValue(currentSheet.GetTexture());
                shader.SetTexture("DiffuseTexture", currentSheet.GetTexture());

                renderer.Device.SetBlendMode(currentBlend);
                shader.Render(renderAction);
                renderer.Device.SetBlendMode(BlendMode.None);
                //renderer.GraphicsDevice.BlendState = ConvertBlend(currentBlend);
                //effect.CurrentTechnique.Passes[0].Apply();
                nv = 0;
                currentSheet = null;
            }
        }




    }
}