using UnityEngine;
public class Fractal : MonoBehaviour {
    private static readonly int matricesId = Shader.PropertyToID("_Matrices");
    private static MaterialPropertyBlock propertyBlock;

    private static readonly Vector3[] directions = {
        Vector3.up,
        Vector3.right,
        Vector3.left,
        Vector3.forward,
        Vector3.back
    };
    private static readonly Quaternion[] rotations = {
        Quaternion.identity,
        Quaternion.Euler(0f, 0f, -90f),
        Quaternion.Euler(0f, 0f, 90f),
        Quaternion.Euler(90f, 0f, 0f),
        Quaternion.Euler(-90f, 0f, 0f)
    };

    [SerializeField] [Range(1, 12)]
    private int depth = 4;
    [SerializeField]
    private Mesh mesh;
    [SerializeField]
    private Material material;
    private Matrix4x4[][] matrices;
    private ComputeBuffer[] matricesBuffers;

    private FractalPart[][] parts;

    private void Update() {
        float spinAngleDelta = 22.5f * Time.deltaTime;

        FractalPart rootPart = this.parts[0][0];
        rootPart.spinAngle += spinAngleDelta;
        rootPart.worldRotation = this.transform.rotation * (rootPart.rotation * Quaternion.Euler(0f, rootPart.spinAngle, 0f));
        rootPart.worldPosition = this.transform.position;

        this.parts[0][0] = rootPart;
        float objectScale = transform.lossyScale.x;
        this.matrices[0][0] = Matrix4x4.TRS(rootPart.worldPosition, rootPart.worldRotation, objectScale*Vector3.one);

        float scale = objectScale;
        for (int li = 1; li < this.parts.Length; li++) {
            scale *= 0.5f;
            FractalPart[] parentParts = this.parts[li - 1];
            FractalPart[] levelParts = this.parts[li];
            Matrix4x4[] levelMatrices = this.matrices[li];

            for (int fpi = 0; fpi < levelParts.Length; fpi++) {
                FractalPart parent = parentParts[fpi / 5];
                FractalPart part = levelParts[fpi];

                part.spinAngle += spinAngleDelta;

                part.worldRotation = parent.worldRotation * (part.rotation * Quaternion.Euler(0f, part.spinAngle, 0f));
                part.worldPosition = parent.worldPosition + parent.worldRotation * (1.5f * scale * part.direction);

                levelParts[fpi] = part;
                levelMatrices[fpi] = Matrix4x4.TRS(part.worldPosition, part.worldRotation, scale * Vector3.one);
            }
        }

        Bounds bounds = new(rootPart.worldPosition, 3f * objectScale * Vector3.one);
        for (int i = 0; i < this.matricesBuffers.Length; i++) {
            ComputeBuffer buffer = this.matricesBuffers[i];
            buffer.SetData(this.matrices[i]);
            propertyBlock.SetBuffer(matricesId, buffer);
            Graphics.DrawMeshInstancedProcedural(this.mesh, 0, this.material, bounds, buffer.count, propertyBlock);
        }
    }

    private void OnEnable() {
        this.parts = new FractalPart[this.depth][];
        this.matrices = new Matrix4x4[this.depth][];
        this.matricesBuffers = new ComputeBuffer[this.depth];
        int stride = 16 * 4;

        for (int i = 0, length = 1; i < this.parts.Length; i++, length *= directions.Length) {
            this.parts[i] = new FractalPart[length];
            this.matrices[i] = new Matrix4x4[length];
            this.matricesBuffers[i] = new ComputeBuffer(length, stride);
        }

        this.parts[0][0] = this.CreatePart(0);

        for (int li = 1; li < this.parts.Length; li++) {
            FractalPart[] levelParts = this.parts[li];

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
        public Vector3 direction, worldPosition;
        public Quaternion rotation, worldRotation;
        public float spinAngle;
    }
}