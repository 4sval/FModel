using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace FModel.Chic.ModelViewer
{
    public class Text : Volume
    {
        Vector2[] texCoords = new Vector2[]
        {
            new Vector2(0, 1),
            new Vector2(1, 1),
            new Vector2(1, 0),
            new Vector2(0, 0)
        };
        Vector3[] colorData = new Vector3[4];
        Vector3[] verts = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(10, 0, 0),
            new Vector3(10, 0, 10),
            new Vector3(0, 0, 10)
        };
        int[] inds = new int[]
        {
            0, 1, 2,
            2, 3, 0
        };

        public override int VertCount => verts.Length;
        public override int ColorDataCount => colorData.Length;
        public override int TextureCoordsCount => texCoords.Length;
        public override int IndiceCount => inds.Length;

        public override string Shader => "textured";
        public override bool WorldSpace => false;

        readonly SKPaint TextPaint = new SKPaint()
        {
            FilterQuality = SKFilterQuality.High,
            Typeface = SKTypeface.Default,
            TextSize = 120
        };
        int texId;

        public void SetText(string text)
        {
            var bmp = new SKBitmap((int)TextPaint.MeasureText(text) + 1, (int)TextPaint.FontSpacing + 1, SKColorType.Rgb888x, SKAlphaType.Opaque);
            using (var c = new SKCanvas(bmp))
            {
                c.Clear(new SKColor(255, 255, 255));
                c.DrawText(text, 0, TextPaint.FontSpacing - TextPaint.FontMetrics.Descent, TextPaint);
            }
            UploadTexture(bmp);
        }

        void UploadTexture(SKBitmap bmp)
        {
            verts[1] = new Vector3(bmp.Width / 40f, 0, 0);
            verts[2] = new Vector3(bmp.Width / 40f, 0, bmp.Height / 40f);
            verts[3] = new Vector3(0, 0, bmp.Height / 40f);

            GL.BindTexture(TextureTarget.Texture2D, texId);
            using (bmp)
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp.Width, bmp.Height, 0,
                     PixelFormat.Rgba, PixelType.UnsignedByte, bmp.GetPixels());
        }

        public Text(Action<string, int> textureAction)
        {
            texId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texId);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            textureAction("textimg", texId);
            TextureID = "textimg";

            CalculateNormals();

            Rotation = new Vector3((float)Math.PI / 2, 0, 0);
        }

        public override void CalculateModelMatrix()
        {
            ModelMatrix = Matrix4.CreateScale(Scale) * Matrix4.CreateRotationX(Rotation.X) * Matrix4.CreateRotationY(Rotation.Y) * Matrix4.CreateRotationZ(Rotation.Z) * Matrix4.CreateTranslation(Position);
        }

        public override Vector3[] GetColorData()
        {
            return colorData;
        }

        public override int[] GetIndices(int offset = 0)
        {
            int[] indices = new int[inds.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = inds[i] + offset;
            }
            return indices;
        }

        public override Vector2[] GetTextureCoords()
        {
            return texCoords;
        }

        public override Vector3[] GetVerts()
        {
            return verts;
        }
    }
}
