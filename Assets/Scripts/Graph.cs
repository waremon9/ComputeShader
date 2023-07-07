using UnityEngine;
public class Graph : MonoBehaviour {
    [SerializeField]
    private Transform pointPrefab;
    [SerializeField] [Range(10, 100)]
    private int resolution = 10;
    [SerializeField]
    private FunctionLibrary.FunctionName function;

    private Transform[] points;

    private void Awake() {
        float step = 2f / this.resolution;
        Vector3 scale = Vector3.one * step;

        this.points = new Transform[this.resolution * this.resolution];

        for (int i = 0; i < this.points.Length; i++) {
            Transform point = this.points[i] = Instantiate(this.pointPrefab);

            point.localScale = scale;
            point.SetParent(this.transform, false);
        }
    }

    private void Update() {
        FunctionLibrary.Function f = FunctionLibrary.GetFunction(this.function);
        float time = Time.time;
        float step = 2f / this.resolution;
        float v = 0.5f * step - 1f;
        for (int i = 0, x = 0, z = 0; i < this.points.Length; i++, x++) {
            if (x == this.resolution) {
                x = 0;
                z += 1;
                v = (z + 0.5f) * step - 1f;
            }
            float u = (x + 0.5f) * step - 1f;
            this.points[i].localPosition = f(u, v, time);
        }
    }
}