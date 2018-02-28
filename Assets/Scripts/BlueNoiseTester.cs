using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace GK {
	public class BlueNoiseTester : MonoBehaviour {

		public GameObject Indicator;

		IEnumerator Start () {

			var bn = new BlueNoise();
			while (true) {
				yield return null;

				Profiler.BeginSample("GetSample");
				var newSample = bn.GetSample();
				Profiler.EndSample();

				var go = Instantiate(Indicator);

				go.transform.position = 100.0f * newSample;
			}
		}
	}
}
