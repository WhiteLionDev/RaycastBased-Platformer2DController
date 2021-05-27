using UnityEngine;

/// <summary>
/// Clase para dotar a un objeto de la posibilidad de moverse por los ejes x e y detectando coliciones en base a los parametros de movimiento recibidos.
/// </summary>
public class Controller2D : RaycastController {

    [Tooltip("Maximo valor en grados sobre el cual el personaje sera capas de caminar")]
	public float maxSlopeAngle = 60;

    // Variable para guardar toda la informacion referente a las coliciones del personaje.
    public CollisionInfo collisions;
    // Variable para guardar los valores de input.
	private Vector2 playerInput;

    /// <summary>
    /// Metodo para inicializar componentes.
    /// </summary>
	public override void Start() {
        // Llama al metodo Start de RaycastController
		base.Start();
        // Setea la direccion en la que apunta el objeto en 1 por defecto (Derecha)
		collisions.faceDir = 1;
	}

    /// <summary>
    /// Metodo que se encarga de gestionar el movimiento del gameobject tanto en el eje x como en el eje y.
    /// </summary>
    /// <param name="moveAmount">Cantidad de unidades a moverse en x e y</param>
    /// <param name="standingOnPlatform">Flag para indicar si el gameobject se encuentraen una plataforma movil</param>
	public void Move(Vector2 moveAmount, bool standingOnPlatform) {
        // realiza los calculos de movimiento sin valores de input.
		Move (moveAmount, Vector2.zero, standingOnPlatform);
	}

    /// <summary>
    /// Metodo que se encarga de gestionar el movimiento del personaje tanto en el eje x como en el eje y.
    /// </summary>
    /// <param name="moveAmount">Cantidad de unidades a moverse en x e y</param>
    /// <param name="input">Input del usuario en x e y</param>
    /// <param name="standingOnPlatform">Flag para indicar si el gameobject se encuentraen una plataforma movil</param>
	public void Move(Vector2 moveAmount, Vector2 input, bool standingOnPlatform = false) {
        // Setea las posiciones de las que partiran los raycasts para detectar coliciones.
		UpdateRaycastOrigins ();

        // Resetea la informacion de coliciones guardada previamente.
		collisions.Reset ();
        // Guarda el vector de movimiento original antes de que pueda ser modificado por la deteccion de coliciones.
		collisions.moveAmountOld = moveAmount;
        // Asigna el input recibido a la variable de mayor alcance dentro de la clase.
		playerInput = input;

        // Si viene un valor de movimiento negativo en el eje y...
		if (moveAmount.y < 0) {
            // Verifica si se esta bajando por una pendiente.
			DescendSlope(ref moveAmount);
		}

        // Si hay movimiento en el eje x...
		if (moveAmount.x != 0) {
            // Usa el valor para setear la direccion en la que apunta el objeto.
			collisions.faceDir = (int)Mathf.Sign(moveAmount.x);
        }

        // Chequea coliciones en el eje horizontal, si es necesario modifica moveAmount.
        HorizontalCollisions(ref moveAmount);

        // Si hay movimiento en el eje y...
        if (moveAmount.y != 0) {
            // Chequea coliciones en el eje vertical, si es necesario modifica moveAmount.
            VerticalCollisions(ref moveAmount);
        }

        // Mueve al objeto por pantalla utilizando el moveAmount resultante de los chequeos de colliciones.
        transform.Translate (moveAmount);

        // Si esta sobre una plataforma movil...
		if (standingOnPlatform) {
            // Activa flag de colicion contra un suelo. 
			collisions.below = true;
		}
	}

    /// <summary>
    /// Metodo que chequea si el objeto puede moverse la cantidad requerida por -moveAmount- sin colicionar en el eje horizontal.
    /// En caso de detectar una collision, restringe el movimiento en el eje horizontal.
    /// </summary>
    /// <param name="moveAmount">Cantidad de unidades a moverse en x e y</param>
	void HorizontalCollisions(ref Vector2 moveAmount) {
        
        // Obtiene la direccion del movimiento y calcula el largo de los raycasts.
		float directionX = collisions.faceDir;
		float rayLength = Mathf.Abs (moveAmount.x) + skinWidth;

        // Si el largo es menor a el minimo, lo ajusta.
		if (Mathf.Abs(moveAmount.x) < skinWidth) {
			rayLength = 2 * skinWidth;
		}

        // Se dispara cada uno de los raycast horizontales y evalua sus coliciones si las hay.
        for (int i = 0; i < horizontalRayCount; i ++) {
            // Dispara el raycast.
			Vector2 rayOrigin = (directionX == -1)?raycastOrigins.bottomLeft:raycastOrigins.bottomRight;
			rayOrigin += Vector2.up * (horizontalRaySpacing * i);
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            // Dibuja el raycast.
			Debug.DrawRay(rayOrigin, Vector2.right * (directionX * rayLength),Color.red);

            // Se evalua si el raycast golpea algo.
			if (hit) {

				if (hit.distance == 0) {
					continue;
				}

                // Obtiene el angulo de inclinacion del objeto que golpeo.
				float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                // Si se esta frente o sobre una pendiente...
				if (i == 0 && slopeAngle <= maxSlopeAngle) {
                    // Si estaba bajando una pendiente y se topo con otra pendiente de subida.
                    if (collisions.descendingSlope)
                    {
                        // Cancela el decenso por la primer pendiente para comenzar el ascenso por la segunda.
                        collisions.descendingSlope = false;
                        moveAmount = collisions.moveAmountOld;
                    }

                    float distanceToSlopeStart = 0;
                    // Si acaba de entrar en contacto con la pendiente...
                    if (slopeAngle != collisions.slopeAngleOld)
                    {
                        // Mueve al personaje un poco hacia atras para que no se superponga con la pendiente al ascender por ella.
                        distanceToSlopeStart = hit.distance - skinWidth;
                        moveAmount.x -= distanceToSlopeStart * directionX;
                    }

                    // Realiza los calculos necesarios para subir por la pendiente.
                    ClimbSlope(ref moveAmount, slopeAngle, hit.normal);
                    moveAmount.x += distanceToSlopeStart * directionX;
                }

                // Si no esta subiendo por una pendiente o el raycast choco contra un muro
				if (!collisions.climbingSlope || slopeAngle > maxSlopeAngle) {
                    // Ajusta el movimiento en x para que el personaje solo pueda recorrer la distancia que lo separa del muro, sin sobreponerse a este.
					moveAmount.x = (hit.distance - skinWidth) * directionX;
					rayLength = hit.distance;

                    // Si estaba subiendo por una pendiente y se topo con un muro...
					if (collisions.climbingSlope) {
                        // Recalcula el movimiento en y para que no halla superpocicion.
						moveAmount.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x);
					}

                    // Guarda la collicion.
					collisions.left = directionX == -1;
					collisions.right = directionX == 1;
				}
			}
		}
	}

    /// <summary>
    /// Metodo que chequea si el objeto puede moverse la cantidad requerida por -moveAmount- sin colicionar en el eje vertical.
    /// En caso de detectar una collision, restringe el movimiento en el eje vertical.
    /// </summary>
    /// <param name="moveAmount">Cantidad de unidades a moverse en x e y</param>
	void VerticalCollisions(ref Vector2 moveAmount) {
        // Obtiene la direccion del movimiento y calcula el largo de los raycasts.
        float directionY = Mathf.Sign (moveAmount.y);
		float rayLength = Mathf.Abs (moveAmount.y) + skinWidth;

        // Se dispara cada uno de los raycast verticales y evalua sus coliciones si las hay.
        for (int i = 0; i < verticalRayCount; i ++) {

            // Dispara el raycast.
			Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
			rayOrigin += Vector2.right * (verticalRaySpacing * i + moveAmount.x);
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);

            // Se dibuja el raycast.
			Debug.DrawRay(rayOrigin, Vector2.up * (directionY * rayLength), Color.red);

            // Se evalua si el raycast golpea algo.
            if (hit) {
                // Si coliciona contra una One way platform...
                if (hit.collider.tag == Tags.THROUGH)
                {
                    // Si el personaje esta saltando desde debajo de la plataforma...
                    if (directionY == 1 || hit.distance == 0)
                    {
                        // Continua por el siguiente raycast ignorando la colicion en este para que pueda atravesar la plataforma desde debajo.
                        continue;
                    }
                    // Si esta saltando hacia debajo de la plataforma...
                    if (collisions.fallingThroughPlatform)
                    {
                        // Continua por el siguiente raycast ignorando la colicion en este para que pueda atravesar la plataforma hacia abajo.
                        continue;
                    }
                    // Si quiere atravesar la plataforma hacia abajo...
                    if (playerInput.y == -1)
                    {
                        // Activa el flag para que se ignoren las coliciones mietras esta atravezando la plataforma.
                        collisions.fallingThroughPlatform = true;
                        Invoke("ResetFallingThroughPlatform", .5f);
                        // Luego continua por el siguiente raycast ignorando la colicion en este.
                        continue;
                    }
                }

                // Ajusta el movimiento en y para que el personaje solo pueda recorrer la distancia que lo separa del muro, sin sobreponerse a este.
                moveAmount.y = (hit.distance - skinWidth) * directionY;
				rayLength = hit.distance;

                // Si estaba subiendo por una pendiente y se topo con un muro...
                if (collisions.climbingSlope) {
                    // Recalcula el movimiento en x para que no halla superpocicion.
                    moveAmount.x = moveAmount.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(moveAmount.x);
				}

                // Guarda la collicion.
                collisions.below = directionY == -1;
				collisions.above = directionY == 1;
			}
		}

        // Si esta subiendo por una pendiente...
		if (collisions.climbingSlope) {
            // Se dispara un raycast horizontal hacia la direccion del movimiento.
			float directionX = Mathf.Sign(moveAmount.x);
			rayLength = Mathf.Abs(moveAmount.x) + skinWidth;
			Vector2 rayOrigin = ((directionX == -1)?raycastOrigins.bottomLeft:raycastOrigins.bottomRight) + Vector2.up * moveAmount.y;
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin,Vector2.right * directionX,rayLength,collisionMask);

            // Si golpea contra algo...
			if (hit) {
                // Evalua si golpea contra una pendiente distinta a la que esta subiendo actualmente.
				float slopeAngle = Vector2.Angle(hit.normal,Vector2.up);
				if (slopeAngle != collisions.slopeAngle) {
                    // Si es asi, ajusta el movimiento en x para que el personaje solo pueda recorrer la distancia que lo separa del muro o pendiente contra el que choco el raycast.
                    moveAmount.x = (hit.distance - skinWidth) * directionX;
					collisions.slopeAngle = slopeAngle;
					collisions.slopeNormal = hit.normal;
				}
			}
		}
	}

    /// <summary>
    /// Metodo para manejar el ascenso por pendientes.
    /// </summary>
    /// <param name="moveAmount">Cantidad de unidades a moverse en x e y</param>
    /// <param name="slopeAngle">Angulo de inclinacion de la pendiente</param>
    /// <param name="slopeNormal">Normal de la pendiente</param>
	void ClimbSlope(ref Vector2 moveAmount, float slopeAngle, Vector2 slopeNormal) {
        // Obtiene valores para calcular el ascenso.
        float moveDistance = Mathf.Abs (moveAmount.x);
		float climbmoveAmountY = Mathf.Sin (slopeAngle * Mathf.Deg2Rad) * moveDistance;

		if (moveAmount.y <= climbmoveAmountY) {
            // Si se esta quieto sobre la pendiente...
            if(collisions.moveAmountOld.x == 0 || playerInput.x == 0)
            {
                // Anula el movimiento en y.
                moveAmount.y = 0;
            }
            // Si se esta subiendo por la pendiente...
            else
            {
                // Calcula el movimiento necesario para el ascenso.
                moveAmount.y = climbmoveAmountY;
                moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
            }

            // Se guardan datos de colicion y de la pendiente.
            collisions.below = true;
			collisions.climbingSlope = true;
			collisions.slopeAngle = slopeAngle;
			collisions.slopeNormal = slopeNormal;
		}
	}

    /// <summary>
    /// Metodo para manejar el decenso por pendientes.
    /// </summary>
    /// <param name="moveAmount">Cantidad de unidades a moverse en x e y</param>
    /// <param name="slopeAngle">Angulo de inclinacion de la pendiente</param>
    /// <param name="slopeNormal">Normal de la pendiente</param>
	void DescendSlope(ref Vector2 moveAmount) {

        // Dispara dos pequeños raycast verticales, uno a cada lado del personaje y apuntando hacia abajo.
		RaycastHit2D maxSlopeHitLeft = Physics2D.Raycast (raycastOrigins.bottomLeft, Vector2.down, Mathf.Abs (moveAmount.y) + skinWidth, collisionMask);
		RaycastHit2D maxSlopeHitRight = Physics2D.Raycast (raycastOrigins.bottomRight, Vector2.down, Mathf.Abs (moveAmount.y) + skinWidth, collisionMask);

        // Si solo uno de los raycast coliciona con algo...
        if (maxSlopeHitLeft ^ maxSlopeHitRight) {
            // Evalua si esta sobre una pendiente demasiado inclinada y es necesario deslizarse por ella.
			SlideDownMaxSlope (maxSlopeHitLeft, ref moveAmount);
			SlideDownMaxSlope (maxSlopeHitRight, ref moveAmount);
		}

        // Si no se esta deslizando por una pendiente...
		if (!collisions.slidingDownMaxSlope) {
            // Dispara otro raycast hacia abajo desde detras del personaje.
			float directionX = Mathf.Sign (moveAmount.x);
			Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
			RaycastHit2D hit = Physics2D.Raycast (rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);

            // Si golpea algo...
            if (hit) {
				float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                // Si golpea contra una pendiente y su inclinacion es menor a la maxima permitida...
				if (slopeAngle != 0 && slopeAngle <= maxSlopeAngle) {
                    // Si esta decendiendo por la pendiente...
					if (Mathf.Sign(hit.normal.x) == directionX) {
						if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x)) {
                            // Calcula el movimiento necesario para descender por la pendiente de manera correcta.
							float moveDistance = Mathf.Abs (moveAmount.x);
							float descendmoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
                            moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
                            moveAmount.y -= descendmoveAmountY;

                            // Se guardan datos de colicion y de la pendiente.
                            collisions.slopeAngle = slopeAngle;
							collisions.descendingSlope = true;
							collisions.below = true;
							collisions.slopeNormal = hit.normal;
						}
					}
				}
			}
		}
	}

    /// <summary>
    /// Metodo para manejar el deslizamiento por pendientes.
    /// </summary>
    /// <param name="hit">Pendiente sobre la que se deslizara</param>
    /// <param name="moveAmount">Cantidad de unidades a moverse en x e y</param>
	void SlideDownMaxSlope(RaycastHit2D hit, ref Vector2 moveAmount) {

        // Evalua el parametro recibido.
		if (hit) {
            // Si la inclinacion es mayor a la maxima permitida...
			float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
			if (slopeAngle > maxSlopeAngle) {
                // Calcula el movimiento para deslizarse por la pendiente.
				moveAmount.x = Mathf.Sign(hit.normal.x) * (Mathf.Abs(moveAmount.y) - hit.distance) / Mathf.Tan(slopeAngle * Mathf.Deg2Rad);

                // Se guardan datos de colicion y de la pendiente.
				collisions.slopeAngle = slopeAngle;
				collisions.slidingDownMaxSlope = true;
				collisions.slopeNormal = hit.normal;
			}
		}

	}

    /// <summary>
    /// Resetea el flag para bajar atravezando una "One way platform".
    /// </summary>
    void ResetFallingThroughPlatform() {
        // Resetea el flag para que vuelva a manejar coliciones en el eje y.
        collisions.fallingThroughPlatform = false;
	}

    /// <summary>
    /// Estructura para guardar toda la informacion referente a las coliciones del objeto.
    /// </summary>
	public struct CollisionInfo {
        // Flags de coliciones.
		public bool above, below;
		public bool left, right;

        // Flags de movimiento sobre pendientes.
		public bool climbingSlope;
		public bool descendingSlope;
		public bool slidingDownMaxSlope;
        
        // Datos sobre pendientes.
        public float slopeAngle, slopeAngleOld;
		public Vector2 slopeNormal;
		public Vector2 moveAmountOld;

        // Direccion en la que se mueve el personaje.
		public int faceDir;

        // Flag para atravezar one way platforms.
		public bool fallingThroughPlatform;

        /// <summary>
        /// Resetea los datos de las coliciones y pendientes
        /// </summary>
		public void Reset() {
			above = below = false;
			left = right = false;
			climbingSlope = false;
			descendingSlope = false;
			slidingDownMaxSlope = false;
            slopeNormal = Vector2.zero;
			slopeAngleOld = slopeAngle;
			slopeAngle = 0;
		}
	}

}
