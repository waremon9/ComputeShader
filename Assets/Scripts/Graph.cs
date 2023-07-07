using UnityEngine;
public class Graph : MonoBehaviour {
    [SerializeField]
    private Transform pointPrefab;
    [SerializeField] [Range(10, 100)]
    private int resolution = 10;

    private Transform[] points;

    private void Awake() {
        Vector3 position = Vector3.zero;
        float step = 2f / this.resolution;
        Vector3 scale = Vector3.one * step;

        this.points = new Transform[this.resolution];

        for (int i = 0; i < this.points.Length; i++) {
            Transform point = this.points[i] = Instantiate(this.pointPrefab);

            position.x = (i + 0.5f) * step - 1f;

            point.localPosition = position;
            point.localScale = scale;

            point.SetParent(this.transform, false);
        }
    }

    private void Update() {
        float time = Time.time;
        for (int i = 0; i < this.points.Length; i++) {
            Transform point = this.points[i];
            Vector3 position = point.localPosition;
            position.y = FunctionLibrary.MultiWave(position.x, time);
            ;
            point.localPosition = position;
        }
    }
}