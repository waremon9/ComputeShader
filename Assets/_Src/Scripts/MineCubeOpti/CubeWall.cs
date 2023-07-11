using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
public class CubeWall : MonoBehaviour {
    private static readonly int positionsId = Shader.PropertyToID("_Positions");
    private static readonly int scaleId = Shader.PropertyToID("_Scale");
    private static readonly int color1Id = Shader.PropertyToID("_Color1");
    private static readonly int color2Id = Shader.PropertyToID("_Color2");

    [SerializeField]
    private Mesh mesh;
    [SerializeField]
    private Material material;
    [SerializeField]
    private Vector2Int dimensions = new(10, 20);
    public int totalCube;
    [SerializeField] [Range(0, 1.5f)]
    private float cubeScale = 1;
    [SerializeField]
    private Color color1 = Color.white;
    [SerializeField]
    private Color color2 = Color.white;
    [SerializeField]
    private float cubesHealth;

    public List<Vector2Int> toDelete;
    private List<CubeData> _aliveCube;
    private Bounds _bounds;
    private CubeData[,] _cubeDataArray2d;
    private List<Vector3> _positionList;
    private ComputeBuffer _positionsBuffer;
    private MaterialPropertyBlock _propertyBlock;
    private bool _updateBuffer;

    private void Update() {
        if (Input.GetKey(KeyCode.Space)) {
            this.RandomBreakCube();
        }
        if (Input.GetKey(KeyCode.N)) {
            this.RandomBreakCube(10);
        }
        if (Input.GetKey(KeyCode.B)) {
            foreach (Vector2Int vec in this.toDelete) {
                this.BreakCubeAt(vec.x, vec.y);
            }
        }

        if (this._updateBuffer) {
            this._updateBuffer = false;
            this.UpdateBuffer();
        }
        if (this._positionList == null || this._positionList.Count <= 0) {
            return;
        }
        Graphics.DrawMeshInstancedProcedural(this.mesh, 0, this.material, this._bounds, this._positionList.Count, this._propertyBlock);
    }

    private void OnEnable() {
        this._propertyBlock ??= new MaterialPropertyBlock();
        this._bounds = this.GetBounds();

        int size = this.dimensions.x * this.dimensions.y;
        this._positionsBuffer = new ComputeBuffer(size, 3 * sizeof(float));
        this._positionList = new List<Vector3>(size);
        this._aliveCube = new List<CubeData>(size);
        this._cubeDataArray2d = new CubeData[this.dimensions.x, this.dimensions.y];

        float offset = this.dimensions.x * .5f;
        for (int y = 0; y < this.dimensions.y; y++) {
            for (int x = 0; x < this.dimensions.x; x++) {
                CubeData cube = new() {
                    alive = true,
                    coordinate = new Vector2Int(x, y),
                    worldPosition = this.transform.position + new Vector3(x - offset, y) * this.cubeScale,
                    health = this.cubesHealth
                };

                this._cubeDataArray2d[x, y] = cube;
                this._aliveCube.Add(cube);
            }
        }

        this.UpdateBuffer();

        this._propertyBlock.SetBuffer(positionsId, this._positionsBuffer);
        this._propertyBlock.SetFloat(scaleId, this.cubeScale);
        this._propertyBlock.SetColor(color1Id, this.color1);
        this._propertyBlock.SetColor(color2Id, this.color2);
    }

    private void OnDisable() {
        this._positionsBuffer.Dispose();
        this._positionList = null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Bounds b = this.GetBounds();
        Gizmos.DrawWireCube(b.center, b.size);

        this.totalCube = this.dimensions.x * this.dimensions.y;
    }
#endif

    public static event Action<CubeWall> OnWallFullyDestroyed;

    private Bounds GetBounds() {
        return new Bounds(this.transform.position + new Vector3(-.5f * this.cubeScale, this.dimensions.y * .5f * this.cubeScale - .5f * this.cubeScale), new Vector3(this.dimensions.x, this.dimensions.y, 1) * this.cubeScale);
    }

    private void DestroyWall() {
        this._positionList = null;
        OnWallFullyDestroyed?.Invoke(this);
        Destroy(this.gameObject);
    }

    private void UpdateBuffer() {
        if (this._aliveCube.Count == 0) {
            this.DestroyWall();
            return;
        }

        this._positionList.Clear();
        this._aliveCube.Clear();

        for (int x = 0; x < this._cubeDataArray2d.GetLength(0); x++) {
            for (int y = 0; y < this._cubeDataArray2d.GetLength(1); y++) {
                CubeData cd = this._cubeDataArray2d[x, y];
                if (cd.alive) {
                    this._positionList.Add(cd.worldPosition);
                    this._aliveCube.Add(cd);
                }
            }
        }

        this._positionsBuffer.SetData(this._positionList);
    }

    private void RandomBreakCube(int amount = 1) {
        if (this._positionList.Count <= amount) {
            this.DestroyWall();
            return;
        }

        for (int i = 0; i < amount; i++) {
            int randomIndex = Random.Range(0, this._aliveCube.Count);
            CubeData cube = this._aliveCube[randomIndex];
            this._aliveCube.RemoveAt(randomIndex);

            cube = this._cubeDataArray2d[cube.coordinate.x, cube.coordinate.y];
            cube.alive = false;
            this._cubeDataArray2d[cube.coordinate.x, cube.coordinate.y] = cube;
        }

        this._updateBuffer = true;
    }

    private void BreakCubeAt(int x, int y) {
        CubeData cube = this._cubeDataArray2d[x, y];
        cube.alive = false;
        this._updateBuffer = true;
        this._cubeDataArray2d[cube.coordinate.x, cube.coordinate.y] = cube;
    }

    private void DamageCubeAt(int x, int y, float damage) {
        CubeData cube = this._cubeDataArray2d[x, y];
        cube.health -= damage;
        if (cube.health <= 0) {
            cube.alive = false;
            this._updateBuffer = true;
        }
        this._cubeDataArray2d[cube.coordinate.x, cube.coordinate.y] = cube;
    }

    public void ImpactAtWorldPosition(Vector3 point, float damage, int size = 0, bool reduceDamageWithDistance = false) {
        Bounds b = this._bounds;
        b.extents += Vector3.one * (size * this.cubeScale);
        if (b.Contains(point) == false) {
            return;
        }

        Vector3 localPoint = this.transform.InverseTransformPoint(point);
        Vector2Int wallCoordinates = this.FromLocalPointToCoordinates(localPoint);

        // To get all coordinates in a circle, we get all in a square then filter with distance to center
        for (int x = -size; x <= size; ++x) {
            for (int y = -size; y <= size; ++y) {
                if (x * x + y * y > size * size) {
                    continue;
                }
                if (wallCoordinates.x + x < 0 || wallCoordinates.x + x >= this.dimensions.x || wallCoordinates.y + y < 0 || wallCoordinates.y + y >= this.dimensions.y) {
                    continue;
                }

                float finalDamage = reduceDamageWithDistance ? Mathf.Lerp(0, damage, 1-(float)(x * x + y * y) / (size * size)) : damage;
                this.DamageCubeAt(wallCoordinates.x + x, wallCoordinates.y + y, finalDamage);
            }
        }
    }

    private Vector2Int FromLocalPointToCoordinates(Vector3 point) {
        float half = .5f * this.cubeScale;
        point.x += this.dimensions.x * half;
        return new Vector2Int(Mathf.RoundToInt(point.x / this.cubeScale), Mathf.RoundToInt(point.y / this.cubeScale));
    }

    private struct CubeData {
        public bool alive;
        public Vector2Int coordinate;
        public Vector3 worldPosition;
        public float health;
    }
}