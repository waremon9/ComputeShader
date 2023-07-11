using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class WallManager : MonoBehaviour {
    [SerializeField]
    private float attackPower;
    [SerializeField]
    private int carveSize;
    [SerializeField]
    private bool reduceDamageWithDistance;
    
    private Camera _camera;
    private List<CubeWall> _cubeWallsList;
    private Plane _plane = new(Vector3.back, Vector3.zero);
    private Ray _ray;

    private void Awake() {
        this._cubeWallsList = this.GetComponentsInChildren<CubeWall>().ToList();
        this._camera = Camera.main;

        CubeWall.OnWallFullyDestroyed += this.OnCubeWallFullyDestroyed;
    }

    private void Update() {
        if (Input.GetMouseButton(0)) {
            this.MouseClick();
        }
    }

    private void OnDestroy() {
        CubeWall.OnWallFullyDestroyed -= this.OnCubeWallFullyDestroyed;
    }

    private void OnCubeWallFullyDestroyed(CubeWall wall) {
        this._cubeWallsList.Remove(wall);
    }

    private void MouseClick() {
        this._ray = this._camera.ScreenPointToRay(Input.mousePosition);
        if (this._plane.Raycast(this._ray, out float enter) == false) {
            return;
        }
        Vector3 hitPoint = this._ray.GetPoint(enter);

        foreach (CubeWall cubeWall in this._cubeWallsList) {
            cubeWall.ImpactAtWorldPosition(hitPoint, this.attackPower*Time.deltaTime, this.carveSize, this.reduceDamageWithDistance);
        }
    }
}