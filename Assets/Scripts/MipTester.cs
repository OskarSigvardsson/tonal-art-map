using UnityEngine;

namespace GK {
	public class MipTester : MonoBehaviour {

		public int MipLevels = 9;
		public Renderer Renderer;

		void Start() {
			var size = 1 << MipLevels;
			var tex = new Texture2D(size, size, TextureFormat.RGB24, true);

			var hue = 0.0f;
			var sat = 0.5f;
			var val = 0.5f;

			var pixels = new Color[size * size];

			for (int i = MipLevels; i >= 0; i--) {
				var col = Color.HSVToRGB(hue, sat, val);
				var mipSize = 1 << (MipLevels - i);

				Debug.Log("Miplevel " + i + " size " + mipSize + " hue " + hue);

				pixels = tex.GetPixels(i);

				for (int j = 0; j < pixels.Length; j++) {
					pixels[j] = new Color(hue, hue, hue);
				}

				tex.SetPixels(pixels, i);
				tex.Apply(false);
				hue += 1.0f / MipLevels;
			}

			tex.Apply(false);


			Renderer.material.mainTexture = tex;
		}
	}
}
