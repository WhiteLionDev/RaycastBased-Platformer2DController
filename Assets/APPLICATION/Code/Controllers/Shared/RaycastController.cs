using UnityEngine;

[RequireComponent (typeof (BoxCollider2D))]
public class RaycastController : MonoBehaviour {

    // Variable para definir contra que layer se detecta colicion.
	public LayerMask collisionMask;
    // Distancia entre raycasts.
    public float dstBetweenRays = 0.25f;
    // Ancho de la "piel" del personaje (offset para castear los raycasts).
    public float skinWidth = .015f;

    // Contadores de raycasts, se calculan en base a la distancia que debe haber entre raycasts.
	[HideInInspector]
	public int horizontalRayCount;
	[HideInInspector]
	public int verticalRayCount;

    // Espacio calculado de distancia real entre los raycasts.
	[HideInInspector]
	public float horizontalRaySpacing;
	[HideInInspector]
	public float verticalRaySpacing;

	[HideInInspector]
	public BoxCollider2D myCollider;
	public RaycastOrigins raycastOrigins;

	public virtual void Awake() {
        // Obtiene la referencia del BoxCollider2D del objeto.
		myCollider = GetComponent<BoxCollider2D> ();
	}

	public virtual void Start() {
        // Se calculan los puntos desde los cuales se comenzaran a emitir raycasts.
        CalculateRaySpacing();
	}

    /// <summary>
    /// Metodo que calcula los puntos desde los cuales se comenzaran a emitir raycasts.
    /// </summary>
	public void UpdateRaycastOrigins() {
        // Obtiene los limites del collider y los reduce por el doble del skin (offset) para comenzar a castear los raycast desde dentro del objeto.
        Bounds bounds = myCollider.bounds;
		bounds.Expand (skinWidth * -2);

        // Calcula los puntos desde los cuales se comenzaran a emitir raycasts.
        raycastOrigins.bottomLeft.x = raycastOrigins.topLeft.x = bounds.min.x;
        raycastOrigins.bottomLeft.y = raycastOrigins.bottomRight.y = bounds.min.y;
        raycastOrigins.topRight.x = raycastOrigins.bottomRight.x = bounds.max.x;
        raycastOrigins.topRight.y = raycastOrigins.topLeft.y = bounds.max.y;
    }
	
    /// <summary>
    /// Metodo que calcula la cantidad de raycast a emitir y la separacion entre ellos.
    /// </summary>
	public void CalculateRaySpacing() {
        // Obtiene los limites del collider y los reduce por el doble del skin (offset) para comenzar a castear los raycast desde dentro del objeto.
        Bounds bounds = myCollider.bounds;
		bounds.Expand (skinWidth * -2);

        // Calcula la cantidad de raycasts en base al tamaño de los limites del collider y la separacion que debe haber entre raycasts.
        horizontalRayCount = Mathf.RoundToInt(bounds.size.y / dstBetweenRays);
        verticalRayCount = Mathf.RoundToInt(bounds.size.x / dstBetweenRays);

        // Calcula la separacion real que habra entre cada raycast en base al tamaño de los limites del collider y la cantidad de raycasts a emitir.
        horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
		verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
	}
	
    /// <summary>
    /// Estructura para guardar los puntos desde los cuales se comenzaran a emitir raycasts desde el GameObject.
    /// </summary>
	public struct RaycastOrigins {
		public Vector2 topLeft, topRight;
		public Vector2 bottomLeft, bottomRight;
	}
}
