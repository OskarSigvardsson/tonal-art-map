using UnityEngine;
using System.Collections;

namespace GK {
	public class TonalArtMapGenerator {

		struct Stroke {
			public Vector2 Position;
			public float Length;
			public bool Horizontal;

			public override string ToString() {
				return string.Format("Stroke(({0}, {1}), {2}, {3})",
					Position.x,
					Position.y,
					Length,
					Horizontal);
			}
		}

		int potSize;
		int size;
		Texture strokeTex;
		int toneLevels;
		int mipLevels;
		float minTone;
		float maxTone;
		float height;
		System.Random generator;
		BlueNoise noiseGen;
		Shader blitShader;
		Material bltMat;
		Texture2D toneCalculator;
		Texture2D[] toneCalculators;

		public RenderTexture[,] Textures { get; private set; }

		// TODO this is a nightmare, i should remove all of these
		// things from the constructor and have the user set them
		// manually
		public TonalArtMapGenerator(
				int potSize,
				Texture strokeTex,
				Shader blitShader,
				int toneLevels,
				int mipLevels,
				float minTone,
				float maxTone,
				float height,
				System.Random generator)
		{
			this.potSize = potSize;
			this.size = 1 << potSize;
			this.strokeTex = strokeTex;
			this.toneLevels = toneLevels;
			this.mipLevels = mipLevels;
			this.minTone = minTone;
			this.maxTone = maxTone;
			this.generator = generator;
			this.noiseGen = new BlueNoise(generator);
			this.blitShader = blitShader;
			this.height = height;

			Textures = new RenderTexture[toneLevels, mipLevels];

			toneCalculator = new Texture2D(1, 1, TextureFormat.ARGB32, false);
			toneCalculators = new Texture2D[mipLevels];

			for (int mip = 0; mip < mipLevels; mip++) {
				var mipSize = size >> mip;
				toneCalculators[mip] = new Texture2D(mipSize, mipSize, TextureFormat.ARGB32, true);
			}

			var oldRt = RenderTexture.active;
			
			for (int tone = 0; tone < toneLevels; tone++) {
				for (int mip = 0; mip < mipLevels; mip++) {
					var mipSize = size >> mip;

					Textures[tone, mip] = new RenderTexture(mipSize, mipSize, 0, RenderTextureFormat.ARGB32);
					Textures[tone, mip].name = string.Format("Tone {0} Mip {1} Size {2}", tone, mip, mipSize);
					Textures[tone, mip].useMipMap = false;

					RenderTexture.active = Textures[tone, mip];

					GL.Clear(true, true, Color.white);
				}
			}

			RenderTexture.active = oldRt;
		}

		public Texture2DArray GetTextureArray() {
			var array = new Texture2DArray(size, size, toneLevels, TextureFormat.RGB24, true);
			var readingTextures = new Texture2D[mipLevels];

			for (int mip = 0; mip < mipLevels; mip++) {
				var mipSize = size >> mip;
				readingTextures[mip] = new Texture2D(mipSize, mipSize, TextureFormat.RGB24, false);
			}

			var oldRt = RenderTexture.active;

			for (int tone = 0; tone < toneLevels; tone++) {
				for (int mip = 0; mip < mipLevels; mip++) {
					RenderTexture.active = Textures[tone, mip];

					var mipSize = size >> mip;

					readingTextures[mip].ReadPixels(new Rect(0, 0, mipSize, mipSize), 0, 0, false);
					var pixels = readingTextures[mip].GetPixels();
					array.SetPixels(pixels, tone, mip);
				}
			}

			array.Apply(false, true);

			array.filterMode = FilterMode.Trilinear;

			RenderTexture.active = oldRt;

			return array;
		}

		public IEnumerable DrawStrokes() {
			var toneRange = maxTone - minTone;
			for (int tone = 0; tone < toneLevels; tone++) {
				var toneValue = minTone + tone * (toneRange / (toneLevels - 1));

				for (int mip = mipLevels - 1; mip >= 0; mip--) {
					while (CalculateTone(tone, mip) < toneValue) {
						var stroke = GenerateStroke(toneValue);

						ApplyStroke(stroke, tone, mip);

						yield return null;
					}
				}
			}
		}

		Stroke GenerateStroke(float toneValue) {
			return new Stroke {
				Position	= noiseGen.GetSample(),
				Length		= (float)(generator.NextDouble() * 0.5f + 0.25f),
				Horizontal	= toneValue < 0.5f * (maxTone - minTone),
			};
		}

		void ApplyStroke(Stroke stroke, int toneLevel, int mipLevel) {
			for (int tone = toneLevel; tone < toneLevels; tone++) {
				for (int mip = mipLevel; mip >= 0; mip--) {
					DrawStroke(Textures[tone, mip], height * (1 << mip), stroke);
				}
			}
		}

		float CalculateTone(int tone, int mip) {
			var oldRt = RenderTexture.active;
			RenderTexture.active = Textures[tone, mip];

			var mipSize = size >> mip;
			Debug.Assert(Textures[tone, mip].width == mipSize);
			Debug.Assert(Textures[tone, mip].height == mipSize);
			Debug.Assert(toneCalculators[mip].width == mipSize);
			Debug.Assert(toneCalculators[mip].height == mipSize);

			toneCalculators[mip].ReadPixels(new Rect(0, 0, mipSize, mipSize), 0, 0, true);

			RenderTexture.active = oldRt;

			var col = toneCalculators[mip].GetPixels(potSize - mip);

			Debug.Assert(col.Length == 1);

			var val = col[0].r * 0.2126f + col[0].g * 0.7152f + col[0].b * 0.0722f;
			val = Mathf.Pow(val, 2.2f);
			return 1 - val;
		}

		// TODO update to add proper rotation, not just horizontal
		void DrawStroke(RenderTexture destination, float height, Stroke stroke)
		{

			if (bltMat == null) {
				bltMat = new Material(blitShader);
			}

			bltMat.mainTexture = strokeTex;

			var oldRt = RenderTexture.active;
			RenderTexture.active = destination;

			GL.PushMatrix();
			GL.LoadOrtho();
			bltMat.SetPass(0);
			GL.Begin(GL.TRIANGLES);

			if (!stroke.Horizontal) {
				var vert = 
					Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0.0f)) * 
					Matrix4x4.Rotate(Quaternion.Euler(new Vector3(0.0f, 0.0f, 90.0f))) * 
					Matrix4x4.Translate(new Vector3(-0.5f, -0.5f, 0.0f));

				GL.MultMatrix(vert);
			}
				

			var x0 = stroke.Position.x;
			var x1 = stroke.Position.x + stroke.Length;
			var y0 = stroke.Position.y - 0.5f * height;
			var y1 = stroke.Position.y + 0.5f * height;

			DrawQuad(x0, y0, x1, y1);

			x0 -= 1.0f;
			x1 -= 1.0f;

			DrawQuad(x0, y0, x1, y1);

			GL.End();
			GL.PopMatrix();
			GL.Flush();

			RenderTexture.active = oldRt;

			bltMat.mainTexture = null;
		}

		void DrawQuad(float x0, float y0, float x1, float y1) {
			GL.TexCoord2(0.0f, 0.0f);
			GL.Vertex3(x0, y0, 0.0f);

			GL.TexCoord2(1.0f, 0.0f);
			GL.Vertex3(x1, y0, 0.0f);

			GL.TexCoord2(1.0f, 1.0f);
			GL.Vertex3(x1, y1, 0.0f);

			GL.TexCoord2(0.0f, 0.0f);
			GL.Vertex3(x0, y0, 0.0f);

			GL.TexCoord2(1.0f, 1.0f);
			GL.Vertex3(x1, y1, 0.0f);

			GL.TexCoord2(0.0f, 1.0f);
			GL.Vertex3(x0, y1, 0.0f);
		}
	}
}
