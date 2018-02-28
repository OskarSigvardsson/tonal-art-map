using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GK {
	public class DebugTonalArtMapGenerator : MonoBehaviour {


		public Renderer TestArrayRenderer;
		public Texture Stroke;
		public Shader BlitShader;
		public int TexturePOTSize = 9;
		public int MipLevels = 5;
		public int ToneLevels = 16;

		public bool ScaleMips = true;

		Transform[,] debugQuads;

		IEnumerator Start() {
			var gen = new TonalArtMapGenerator(
				TexturePOTSize,
				Stroke,
				BlitShader,
				ToneLevels,
				MipLevels,
				0.00f,
				0.95f,
				0.00125f,
				new System.Random());


			debugQuads = new Transform[ToneLevels, MipLevels];

			for (int tone = 0; tone < ToneLevels; tone++) {
				for (int mip = 0; mip < MipLevels; mip++) {
					var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);

					quad.GetComponent<Renderer>().material.mainTexture = gen.Textures[tone, mip];
					quad.name = string.Format("Tone {0} Mip {1}", tone, mip);
					quad.transform.SetParent(transform, false);

					debugQuads[tone, mip] = quad.transform;
				}
			}

			yield return null;

			var watch = new System.Diagnostics.Stopwatch();

			watch.Start();

			foreach (var _ in gen.DrawStrokes()) {
				if (watch.Elapsed.TotalMilliseconds > 30.0) {
					yield return null;
					watch.Reset();
					watch.Start();
				}
			}

			var path = "Assets/TonalArtMapTexture.asset";
			UnityEditor.AssetDatabase.CreateAsset(gen.GetTextureArray(), path);
		}

		void Update() {
			var x = 0.0f;
			for (int tone = 0; tone < ToneLevels; tone++) {
				var y = 0.0f;
				for (int mip = 0; mip < MipLevels; mip++) {
					var quad = debugQuads[tone, mip];


					if (ScaleMips) {
						quad.position = x * Vector3.right + y * Vector3.up;
						quad.localScale = (1.0f / (1 << mip)) * Vector3.one;

						y += 1.0f / (1 << mip);
					} else {
						quad.position = x * Vector3.right + y * Vector3.up;
						quad.localScale = Vector3.one;

						y += 1.0f;
					}
				}

				x += 1.0f + 0.1f;
			}
		}
	}
}
