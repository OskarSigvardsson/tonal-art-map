using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GK {
	public class BlueNoise {

		System.Random generator;

		List<Vector2> samples;
		List<int> activeSamples;
		List<int> grid;

		float radius;
		float gridSize;
		int gridDims;
		int rejectionLimit = 30;

		public BlueNoise() 
			: this(new System.Random()) { }

		public BlueNoise(System.Random generator) {
			this.generator = generator;

			samples = new List<Vector2>();
			grid = new List<int>();

			Clear();
		}

		public void Clear() {
			radius = 0.25f * Mathf.Sqrt(2);
			gridDims = Mathf.CeilToInt(Mathf.Sqrt(2) / radius);
			gridSize = 1.0f / gridDims;

			activeSamples = null;

			grid.Clear();
			samples.Clear();

			var firstSample = new Vector2(
					(float)generator.NextDouble(), 
					(float)generator.NextDouble());

			samples.Add(firstSample);

			var gridCount = gridDims * gridDims;

			if (grid.Capacity < gridCount) {
				grid.Capacity = gridCount;
			}

			grid.Clear();

			for (int i = 0; i < gridCount; i++) {
				grid.Add(-1);
			}

			int xi, yi;
			grid[GetGridIndex(firstSample, out xi, out yi)] = 0;
		}

		public Vector2 GetSample() {
			//Debug.Break();
			//Debug.Log(radius);

			if (activeSamples == null) {
				// First invocation. Instead of having a "firstRun" bool or
				// something, check if activeSamples exist yet
				activeSamples = new List<int>();
				activeSamples.Add(0);
				return samples[0];
			}

			if (activeSamples.Count == 0) {
				Reinitialize();
			}

			Debug.Assert(activeSamples.Count > 0);

			var rSqr = radius * radius;

			do {
				var asi = generator.Next() % activeSamples.Count;
				var activeSample = samples[activeSamples[asi]];

				//Debug.Log("Selecting active sample " + activeSample);

				for (int i = 0; i < rejectionLimit; i++) {
					int xi, yi;
					var newSample = GenerateSample(activeSample);
					var newSampleGridIndex = GetGridIndex(newSample, out xi, out yi);

					var success = true;

					for (int y = yi - 2; y <= yi + 2; y++) {
						if (y < 0) continue;
						if (y >= gridDims) break;

						for (int x = xi - 2; x <= xi + 2; x++) {
							if (x < 0) continue;
							if (x >= gridDims) break;

							var gridSampleIndex = grid[y * gridDims + x];

							if (gridSampleIndex < 0) continue;

							var gridSample = samples[gridSampleIndex];

							if ((newSample - gridSample).sqrMagnitude < rSqr) {
								//Debug.Log("Too close to " + gridSample);
								success = false;

								break;
							}
						}

						if (!success) break;
					}

					if (success) {
						var sampleIndex = samples.Count; samples.Add(newSample);
						activeSamples.Add(sampleIndex);
						//Debug.Log("Adding sample " + samples[sampleIndex]);
						grid[newSampleGridIndex] = sampleIndex;

						return newSample;
					}
				}

				// If we get here, there was no success, so remove the active
				// sample
				//Debug.Log("Removing active sample " + samples[activeSamples[asi]]);
				activeSamples.RemoveAt(asi);
			} while (activeSamples.Count > 0);

			// If we get here, there are no active samples left, so try running
			// the method again (at which point it will reinitialize).
			return GetSample();
		}

		Vector2 GenerateSample(Vector2 reference) {
			var rMin = radius;
			var rMax = 2.0f * radius;

			var rMinSqr = rMin * rMin;
			var rMaxSqr = rMax * rMax;

			float x, y;

			do {
				var r = Mathf.Sqrt((float)((rMaxSqr - rMinSqr) * generator.NextDouble() + rMinSqr));
				var angle = (float)(2.0f * Mathf.PI * generator.NextDouble());

				x = reference.x + r * Mathf.Cos(angle);
				y = reference.y + r * Mathf.Sin(angle);
				  
			} while (x < 0.0f || x >= 1.0f || y < 0.0f || y >= 1.0f);

			return new Vector2(x, y);
		}

		void Reinitialize() {
			//Debug.Log("Reinitializing");
			Debug.Assert(activeSamples.Count == 0);

			radius /= 2.0f;
			gridDims = Mathf.CeilToInt(Mathf.Sqrt(2) / radius);
			gridSize = 1.0f / gridDims;
			
			var gridCount = gridDims * gridDims;

			if (grid.Capacity < gridCount) {
				grid.Capacity = gridCount;
			}

			grid.Clear();

			for (int i = 0; i < gridCount; i++) {
				grid.Add(-1);
			}

			Debug.Assert(grid.Count == gridDims * gridDims);

			int xi, yi;

			for (int i = 0; i < samples.Count; i++) {
				grid[GetGridIndex(samples[i], out xi, out yi)] = i;
				activeSamples.Add(i);
			}
		}

		int GetGridIndex(Vector2 point, out int gridX, out int gridY) {
			gridX = Mathf.FloorToInt(point.x / gridSize);
			gridY = Mathf.FloorToInt(point.y / gridSize);

			//Debug.Log("Point " + point + " dims " + gridDims);
			//Debug.Log("x,y " + gridX + ", " + gridY);
			//Debug.Break();
			return gridY * gridDims + gridX;
		}
	}
}
