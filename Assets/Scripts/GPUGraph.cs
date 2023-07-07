using UnityEngine;
public class GPUGraph : MonoBehaviour {
    public enum TransitionMode { Cycle, Random }
    private static readonly int positionsId = Shader.PropertyToID("_Positions");
    private static readonly int resolutionId = Shader.PropertyToID("_Resolution");
    private static readonly int stepId = Shader.PropertyToID("_Step");
    private static readonly int timeId = Shader.PropertyToID("_Time");

    [SerializeField]
    private ComputeShader computeShader;
    [SerializeField]
    private Material material;
    [SerializeField]
    private Mesh mesh;
    [SerializeField] [Range(10, 1000)]
    private int resolution = 10;
    [SerializeField]
    private FunctionLibrary.FunctionName function;
    [SerializeField]
    private TransitionMode transitionMode;
    [SerializeField] [Min(0f)]
    private float functionDuration = 1f, transitionDuration = 1f;

    private float duration;

    private ComputeBuffer positionsBuffer;
    private FunctionLibrary.FunctionName transitionFunction;
    private bool transitioning;

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

        this.UpdateFunctionOnGPU();
    }

    private void OnEnable() {
        this.positionsBuffer = new ComputeBuffer(this.resolution * this.resolution, 12);
    }

    private void OnDisable() {
        this.positionsBuffer.Release();
        this.positionsBuffer = null;
    }

    private void UpdateFunctionOnGPU() {
        float step = 2f / this.resolution;
        this.computeShader.SetInt(resolutionId, this.resolution);
        this.computeShader.SetFloat(stepId, step);
        this.computeShader.SetFloat(timeId, Time.time);

        this.computeShader.SetBuffer(0, positionsId, this.positionsBuffer);

        int groups = Mathf.CeilToInt(this.resolution / 8f);
        this.computeShader.Dispatch(0, groups, groups, 1);

        this.material.SetBuffer(positionsId, this.positionsBuffer);
        this.material.SetFloat(stepId, step);

        Bounds bounds = new(Vector3.zero, Vector3.one * (2f + 2f / this.resolution));
        Graphics.DrawMeshInstancedProcedural(this.mesh, 0, this.material, bounds, this.positionsBuffer.count);
    }

    private void PickNextFunction() {
        this.function = this.transitionMode == TransitionMode.Cycle ? FunctionLibrary.GetNextFunctionName(this.function) : FunctionLibrary.GetRandomFunctionNameOtherThan(this.function);
    }
}