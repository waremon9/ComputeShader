using UnityEngine;
public class GPUGraph : MonoBehaviour {
    public enum TransitionMode { None, Cycle, Random }

    private const int maxResolution = 1000;

    private static readonly int positionsId = Shader.PropertyToID("_Positions");
    private static readonly int resolutionId = Shader.PropertyToID("_Resolution");
    private static readonly int stepId = Shader.PropertyToID("_Step");
    private static readonly int timeId = Shader.PropertyToID("_Time");
    private static readonly int transitionProgressId = Shader.PropertyToID("_TransitionProgress");

    [SerializeField]
    private ComputeShader computeShader;
    [SerializeField]
    private Material material;
    [SerializeField]
    private Mesh mesh;
    [SerializeField] [Range(10, maxResolution)]
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
        this.positionsBuffer = new ComputeBuffer(maxResolution * maxResolution, 3 * 4);
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

        if (this.transitioning) {
            this.computeShader.SetFloat(transitionProgressId, Mathf.SmoothStep(0f, 1f, this.duration / this.transitionDuration));
        }

        int kernelIndex = (int)this.function + (int)(this.transitioning ? this.transitionFunction : this.function) * FunctionLibrary.FunctionCount;
        this.computeShader.SetBuffer(kernelIndex, positionsId, this.positionsBuffer);

        int groups = Mathf.CeilToInt(this.resolution / 8f);
        this.computeShader.Dispatch(kernelIndex, groups, groups, 1);

        this.material.SetBuffer(positionsId, this.positionsBuffer);
        this.material.SetFloat(stepId, step);

        Bounds bounds = new(Vector3.zero, Vector3.one * (2f + 2f / this.resolution));
        Graphics.DrawMeshInstancedProcedural(this.mesh, 0, this.material, bounds, this.resolution * this.resolution);
    }

    private void PickNextFunction() {
        switch (this.transitionMode) {
            case TransitionMode.None:
                break;
            case TransitionMode.Cycle:
                this.function = FunctionLibrary.GetNextFunctionName(this.function);
                break;
            case TransitionMode.Random:
                this.function = FunctionLibrary.GetRandomFunctionNameOtherThan(this.function);
                break;
        }
    }
}