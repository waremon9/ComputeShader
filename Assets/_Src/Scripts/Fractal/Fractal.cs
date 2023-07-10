using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;

public class Fractal : MonoBehaviour {
    private static readonly int matricesId = Shader.PropertyToID("_Matrices");
    private static MaterialPropertyBlock propertyBlock;

    private static readonly float3[] directions = {
        up(),
        right(),
        left(),
        forward(),
        back()
    };

    private static readonly quaternion[] rotations = {
        quaternion.identity,
        quaternion.RotateZ(-0.5f * PI),
        quaternion.RotateZ(0.5f * PI),
        quaternion.RotateX(0.5f * PI),
        quaternion.RotateX(-0.5f * PI)
    };

    [SerializeField] [Range(1, 8)]
    private int depth = 4;
    [SerializeField]
    private Mesh mesh;
    [SerializeField]
    private Material material;

    private NativeArray<float3x4>[] matrices;
    private ComputeBuffer[] matricesBuffers;
    private NativeArray<FractalPart>[] parts;

    private void Update() {
        float spinAngleDelta = 0.125f * PI * Time.deltaTime;

        FractalPart rootPart = this.parts[0][0];
        rootPart.spinAngle += spinAngleDelta;
        rootPart.worldRotation = mul(this.transform.rotation, mul(rootPart.rotation, quaternion.RotateY(rootPart.spinAngle)));
        rootPart.worldPosition = this.transform.position;
        this.parts[0][0] = rootPart;
        float objectScale = this.transform.lossyScale.x;
        float3x3 r = float3x3(rootPart.worldRotation) * objectScale;
        this.matrices[0][0] = float3x4(r.c0, r.c1, r.c2, rootPart.worldPosition);

        float scale = objectScale;
        JobHandle jobHandle = default;
        for (int li = 1; li < this.parts.Length; li++) {
            scale *= 0.5f;
            jobHandle = new UpdateFractalLevelJob {
                spinAngleDelta = spinAngleDelta,
                scale = scale,
                parents = this.parts[li - 1],
                parts = this.parts[li],
                matrices = this.matrices[li]
            }.ScheduleParallel(this.parts[li].Length, 5, jobHandle);
        }
        jobHandle.Complete();

        Bounds bounds = new(rootPart.worldPosition, 3f * objectScale * Vector3.one);
        for (int i = 0; i < this.matricesBuffers.Length; i++) {
            ComputeBuffer buffer = this.matricesBuffers[i];
            buffer.SetData(this.matrices[i]);
            propertyBlock.SetBuffer(matricesId, buffer);
            Graphics.DrawMeshInstancedProcedural(this.mesh, 0, this.material, bounds, buffer.count, propertyBlock);
        }
    }

    private void OnEnable() {
        this.parts = new NativeArray<FractalPart>[this.depth];
        this.matrices = new NativeArray<float3x4>[this.depth];
        this.matricesBuffers = new ComputeBuffer[this.depth];
        int stride = 12 * 4;

        for (int i = 0, length = 1; i < this.parts.Length; i++, length *= directions.Length) {
            this.parts[i] = new NativeArray<FractalPart>(length, Allocator.Persistent);
            this.matrices[i] = new NativeArray<float3x4>(length, Allocator.Persistent);
            this.matricesBuffers[i] = new ComputeBuffer(length, stride);
        }

        this.parts[0][0] = this.CreatePart(0);

        for (int li = 1; li < this.parts.Length; li++) {
            NativeArray<FractalPart> levelParts = this.parts[li];

            for (int fpi = 0; fpi < levelParts.Length; fpi += 5) {
                for (int ci = 0; ci < 5; ci++) {
                    levelParts[fpi + ci] = this.CreatePart(ci);
                }
            }
        }

        propertyBlock ??= new MaterialPropertyBlock();
    }

    private void OnDisable() {
        for (int i = 0; i < this.matricesBuffers.Length; i++) {
            this.matricesBuffers[i].Release();
            this.parts[i].Dispose();
            this.matrices[i].Dispose();
        }
        this.parts = null;
        this.matrices = null;
        this.matricesBuffers = null;
    }

    private void OnValidate() {
        if (this.parts != null && this.enabled) {
            this.OnDisable();
            this.OnEnable();
        }
    }

    private FractalPart CreatePart(int childIndex) {
        return new FractalPart {
            direction = directions[childIndex],
            rotation = rotations[childIndex]
        };
    }

    private struct FractalPart {
        public float3 direction, worldPosition;
        public quaternion rotation, worldRotation;
        public float spinAngle;
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    private struct UpdateFractalLevelJob : IJobFor {
        public float spinAngleDelta;
        public float scale;
        [ReadOnly]
        public NativeArray<FractalPart> parents;
        public NativeArray<FractalPart> parts;
        [WriteOnly]
        public NativeArray<float3x4> matrices;

        public void Execute(int i) {
            FractalPart parent = this.parents[i / 5];
            FractalPart part = this.parts[i];

            part.spinAngle += this.spinAngleDelta;

            part.worldRotation = mul(parent.worldRotation, mul(part.rotation, quaternion.RotateY(part.spinAngle)));
            part.worldPosition = parent.worldPosition + mul(parent.worldRotation, 1.5f * this.scale * part.direction);
            this.parts[i] = part;

            float3x3 r = float3x3(part.worldRotation) * this.scale;
            this.matrices[i] = float3x4(r.c0, r.c1, r.c2, part.worldPosition);
        }
    }
}