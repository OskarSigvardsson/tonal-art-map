using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GK {
	public class ImageBlit : MonoBehaviour {

		public Shader Shader;
		public Texture Source;
		public RenderTexture Destination;

		Material mat;
		float lastBlit = 0.0f;
		
		void Start() {
			var oldRt = RenderTexture.active;
			RenderTexture.active = Destination;
			GL.Clear(true, true, Color.white);
			RenderTexture.active = oldRt;


		}

		void Update() {
			if (Time.time - lastBlit > 1.0f) {
				Blit();
				lastBlit = Time.time;
			}
		}

		void Blit() {
			if (mat == null) {
				mat = new Material(Shader);
				mat.mainTexture = Source;
			}

			var oldRt = RenderTexture.active;
			RenderTexture.active = Destination;

			GL.PushMatrix();
			mat.SetPass(0);
			GL.LoadOrtho();
			GL.MultMatrix(Matrix4x4.Translate(new Vector3(0.5f * Random.value - 0.25f, 0.5f * Random.value - 0.25f, 0.0f)));
			GL.Begin(GL.TRIANGLES);

			GL.TexCoord2(0.0f, 0.0f);
			GL.Vertex3(0.2f, 0.49f, 0.0f);

			GL.TexCoord2(1.0f, 0.0f);
			GL.Vertex3(0.8f, 0.49f, 0.0f);

			GL.TexCoord2(1.0f, 1.0f);
			GL.Vertex3(0.8f, 0.51f, 0.0f);

			GL.TexCoord2(0.0f, 0.0f);
			GL.Vertex3(0.2f, 0.49f, 0.0f);

			GL.TexCoord2(1.0f, 1.0f);
			GL.Vertex3(0.8f, 0.51f, 0.0f);

			GL.TexCoord2(0.0f, 1.0f);
			GL.Vertex3(0.2f, 0.51f, 0.0f);

			GL.End();
			GL.PopMatrix();
			GL.Flush();

			RenderTexture.active = oldRt;
		}
	}
}
