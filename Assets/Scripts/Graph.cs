using UnityEngine;
public class Graph : MonoBehaviour {
    public enum TransitionMode { Cycle, Random }

    [SerializeField]
    private Transform pointPrefab;
    [SerializeField] [Range(10, 200)]
    private int resolution = 10;
    [SerializeField]
    private FunctionLibrary.FunctionName function;
    [SerializeField]
    private TransitionMode transitionMode;
    [SerializeField] [Min(0f)]
    private float functionDuration = 1f, transitionDuration = 1f;

    private float duration;
    private Transform[] points;
    private FunctionLibrary.FunctionName transitionFunction;
    private bool transitioning;

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
        this.duration += Time.deltaTime;
        if (this.transitioning) {
            if (this.duration >= this.transitionDuration) {
                this.duration -= this.transitionDuration;
                this.transitioning = false;
            }
        }
        else if (this.duration >= this.functionDuration) {
            this.duration -= this.functionDuration;
            this.transitioning = true;
            this.transitionFunction = this.function;
            this.PickNextFunction();
        }

        if (this.transitioning) {
            this.UpdateFunctionTransition();
        }
        else {
            this.UpdateFunction();
        }
    }

    private void PickNextFunction() {
        this.function = this.transitionMode == TransitionMode.Cycle ? FunctionLibrary.GetNextFunctionName(this.function) : FunctionLibrary.GetRandomFunctionNameOtherThan(this.function);
    }

    private void UpdateFunction() {
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

    private void UpdateFunctionTransition() {
        FunctionLibrary.Function from = FunctionLibrary.GetFunction(this.transitionFunction), to = FunctionLibrary.GetFunction(this.function);
        float progress = this.duration / this.transitionDuration;
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
            this.points[i].localPosition = FunctionLibrary.Morph(u, v, time, from, to, progress);
        }
    }
}