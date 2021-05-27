using UnityEngine;

[RequireComponent(typeof(Controller2D))]
public class Player2D : MonoBehaviour
{
    public HomingButterfly homingButterfly;

    public PlayerParameters defaultParameters;

    private PlayerParameters Parameters { get { return defaultParameters; } }

    private float timeToWallUnstick;

    private Vector2 homingInitialPosition;
    private Vector2 homingDirection;
    private float homingTargetDistance = 0;

    private float gravity;
    private float maxJumpVelocity;
    private float minJumpVelocity;
    private float _jumpIn;
    private int _airJumps;

    private float _jumpInputDownBuffer;
    private bool jumpButtonIsPressed;
    private float velocityYAux;

    private Vector3 velocity;
    public Vector3 Velocity
    {
        get { return velocity; }
    }
    private float velocityXSmoothing;
    private float velocityYSmoothing;

    private bool constantVelocity;

    private Controller2D controller;
    public Controller2D.CollisionInfo Collisions
    {
        get { return controller.collisions; }
    }

    private Vector2 directionalInput;
    private bool wallSliding;
    private float wallSlidingTime = 0;
    private int wallDirX;
    private bool Climb { get; set; }

    private SpriteRenderer _renderer;

    private bool isFacingRight;

    private bool canTakeDamage = true;
    private int currentLifePoints = 0;
    private Vector3 checkpoint;

    /// <summary>
    /// Chequea si el personaje puede saltar.
    /// </summary>
    public bool CanJump
    {
        get
        {
            // Por defecto devuelve false
            bool result = false;

            // Si puede saltar sobre el suelo...
            if ((Parameters.JumpRestrictions & JumpBehavior.CanJumpOnGround) != 0)
                // y esta sobre el suelo retorna true.
                result = controller.collisions.below;

            // Si puede saltar en el aire...
            if (!result && (Parameters.JumpRestrictions & JumpBehavior.CanJumpOnAir) != 0)
                // Si el cooldown es menor a 0, esta en el aire y tiene saltos disponibles retorna true.
                result = _jumpIn <= 0 && !controller.collisions.below && (_airJumps < Parameters.maxAirJumps || Parameters.maxAirJumps == 0);

            // Si puede hacer WallJump...
            if (!result && (Parameters.JumpRestrictions & JumpBehavior.CanJumpOnWall) != 0)
                // y esta deslizandose en una pared retorna true.
                result = wallSliding;

            return result;
        }
    }

    /// <summary>
    /// Metodo en el que se obtienen las referencias a los componentes necesarios.
    /// </summary>
    void Awake()
    {
        // Se obtiene la referencia al componente Controller2D del personaje
        controller = GetComponent<Controller2D>();
        _renderer = GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>
    /// Metodo para inicializar los parametros necesarios.
    /// </summary>
    void Start()
    {
        // Calvula la gravedad en base a los PlayerParameters.
        CalculateGravity();

        // Setea la direccion en la que esta mirando el personaje.
        isFacingRight = transform.localScale.x > 0;

        // Guarda la posicion del checkpoint para haccer respawn.
        checkpoint = this.transform.position;

        // Setea los puntos de vida del personaje.
        currentLifePoints = Parameters.lifePoints;
    }

    void Update()
    {
        // Calcula la velocidad a la que se movera el personaje.
        CalculateVelocity();

        // Maneja el deslizamiento por paredes.
        HandleWallSliding();

        // Si recibe un valor de input en el eje x se voltea al personaje hacia la direccion en la que se intenta avanzar.
        if (directionalInput.x > 0 && !isFacingRight)
            Flip();
        else if (directionalInput.x < 0 && isFacingRight)
            Flip();

        // Mueve al personaje en base a la velocidad previamente calculada y al input recibido.
        controller.Move(velocity * Time.deltaTime, directionalInput);

        // Si esta en colición con el suelo o el techo...
        if (controller.collisions.above || controller.collisions.below)
        {
            // Si se encuentra sobre una pendiente demasiado inclinada...
            if (controller.collisions.slidingDownMaxSlope)
            {
                // Se calcula velocidad de deslizamiento por la pendiente.
                velocity.y += controller.collisions.slopeNormal.y * -gravity * Time.deltaTime;
            }
            else
            {
                // Se setea en 0 la velocidad en y.
                velocityYSmoothing = 0;
                velocity.y = 0;
            }
        }

        // Si se posa en el suelo o un muro resetea los indicadores de movimientos aereos.
        if (controller.collisions.below || wallSliding) {
            // Resetea el contador de saltos en el aire.
            _airJumps = 0;
        }

        // Evalua si segun el buffer se debe realizar un salto.
        if (_jumpInputDownBuffer > 0)
        {
            // Intenta realizar el salto nuevamente.
            OnJumpInputDown(false);

            // Si se salto gracias al buffer, pero no se mantuvo presionado el boton de salto...
            if (!jumpButtonIsPressed && _jumpInputDownBuffer <= 0)
            {
                // Realiza el salto con la altura minima.
                OnJumpInputUp();
            }
        }

        // Se resta tiempo a las variables de cooldowns y buffers.
        SubstractCooldownAndBuffersTime(Time.deltaTime);

    }

    /// <summary>
    /// Metodo que voltea al personaje invirtiendo su escala local en el eje x.
    /// </summary>
    private void Flip()
    {
        // Voltea al personaje invirtiendo su escala en x
        transform.localScale = new Vector3(transform.localScale.x * -1, transform.localScale.y, transform.localScale.z);
        isFacingRight = !isFacingRight;
    }

    /// <summary>
    /// Metodo que guarda los valores recibidos del input de direccion.
    /// </summary>
    /// <param name="input">Valores de entrada</param>
    public void SetDirectionalInput(Vector2 input)
    {
        // Guarda el input recibido en la variable local
        directionalInput = input;
    }

    /// <summary>
    /// Metodo que se encarga de ejecutar un salto, ya sea sobre suelo pared o aire.
    /// </summary>
    /// <param name="buttonJustPressed">Flag para saber si el metodo fue invocado por orden directa del usuario</param>
    public void OnJumpInputDown(bool buttonJustPressed = true)
    {
        if (buttonJustPressed)
            jumpButtonIsPressed = true;

        // Si puede saltar...
        if (CanJump)
        {
            // Si se esta deslizando por una pared...
            if (wallSliding)
            {
                // Si se intenta escalar la pared...
                if (wallDirX == directionalInput.x)
                {
                    // Salta hacia la pared, escalandola.
                    velocity.x = -wallDirX * Parameters.wallJumpClimb.x;
                    velocity.y = Parameters.wallJumpClimb.y;
                }
                // Si se intenta saltar fuera de la pared...
                else if (directionalInput.x == 0)
                {
                    // Salta fuera de la pared.
                    velocity.x = -wallDirX * Parameters.wallJumpOff.x;
                    velocity.y = Parameters.wallJumpOff.y;
                }
                // Si solo se salta desde la pared...
                else
                {
                    // Salta desde la pared hacie el otro lado.
                    velocity.x = -wallDirX * Parameters.wallLeap.x;
                    velocity.y = Parameters.wallLeap.y;
                }

            }
            // Si no se esta deslizando por una pared...
            else
            {
                // Si se esta deslizando por una pendiente...
                if (controller.collisions.slidingDownMaxSlope)
                {
                    // Solo se puede saltar hacia la direccion en la que se esta deslizando
                    if (directionalInput.x != -Mathf.Sign(controller.collisions.slopeNormal.x))
                    {
                        // Salta hacia arriva y hacia la direccion en que se esta deslizando y se setea el cooldown del salto
                        velocity.y = maxJumpVelocity * controller.collisions.slopeNormal.y;
                        velocity.x = maxJumpVelocity * controller.collisions.slopeNormal.x;
                        _jumpIn = Parameters.jumpFrequency;
                    }
                }
                // Si esta en el aire...
                else if (!controller.collisions.below)
                {
                    // Salta en el aire.
                    velocity.y = maxJumpVelocity;
                    _jumpIn = Parameters.jumpFrequency;
                    _airJumps += 1;
                }
                // Si esta en el suelo
                else
                {
                    // Salta hacia arriva y se setea el cooldown del salto
                    velocity.y = maxJumpVelocity;
                    _jumpIn = Parameters.jumpFrequency;
                }
            }
            // Ya que realizo el salto, setea en 0 el buffer.
            _jumpInputDownBuffer = 0;
        }
        else if (buttonJustPressed)
        {
            // Ya que no se pudo saltar, activa el buffer.
            _jumpInputDownBuffer = Parameters.jumpInputBuffer;
        }
    }

    /// <summary>
    /// Flag para limitar la altura de un salto en cuanto se deja de presionar el input.
    /// </summary>
    public void OnJumpInputUp()
    {
        jumpButtonIsPressed = false;

        // Si el personaje esta saltando...
        if (velocity.y > 0)
        {
            // Si la distancia que se llego a saltar supera al salto minimo...
            if ((maxJumpVelocity - velocity.y) > minJumpVelocity)
            {
                // Setea la velocidad en y a cero, para terminar el salto.
                velocity.y = 0;
            }
            else
            {
                // Si no, setea la velocidad en y con la distancia que le falta saltar para cubrir el salto minimo.
                velocity.y = minJumpVelocity - (maxJumpVelocity - velocity.y);
            }
        }
    }

    public void OnHomingInput()
    {
        if (homingButterfly.traveling)
        {
            homingInitialPosition = transform.position;
            homingTargetDistance = Vector2.Distance(transform.position, homingButterfly.transform.position) * Parameters.homingOffset;

            if (homingTargetDistance < Parameters.homingMinDistance)
                homingTargetDistance = Parameters.homingMinDistance;

            homingDirection = (homingButterfly.transform.position - transform.position).normalized;

            if ((homingDirection.x > 0f && homingDirection.x < 0.1f) || (homingDirection.x < 0f && homingDirection.x > -0.1f))
                homingDirection.x = 0;

            if ((homingDirection.y > 0f && homingDirection.y < 0.1f) || (homingDirection.y < 0f && homingDirection.y > -0.1f))
                homingDirection.y = 0;

            velocity.x = homingDirection.x * Parameters.homingSpeed;
            velocity.y = homingDirection.y * Parameters.homingSpeed;

            homingButterfly.Return(transform.position, Parameters.homingButterflyBaseSpeed);

            constantVelocity = true;
        }
        else
        {
            Vector3 butterflyTarget;

            butterflyTarget = transform.position + (((isFacingRight) ? Vector3.right : Vector3.left) * Parameters.homingButterflyMaxDistance);

            homingButterfly.Move(butterflyTarget, transform.position, Parameters.homingButterflyBaseSpeed, Parameters.homingButterflyReturningSpeedFactor);
        }
    }

    /// <summary>
    /// Metodo para manejar el deslizamiento por muros.
    /// </summary>
    void HandleWallSliding()
    {
        wallDirX = (controller.collisions.left) ? -1 : 1;
        wallSliding = false;
        if ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below)
        {

            wallSliding = true;

            if (velocity.y < 0)
            {
                if (!Climb)
                {
                    wallSlidingTime += Time.deltaTime;

                    velocity.y = -Parameters.wallSlideSpeed.Evaluate(wallSlidingTime) * Mathf.Abs(gravity);
                }
                else
                {
                    velocityYSmoothing = 0;
                    velocity.y = 0;
                }

                if (timeToWallUnstick > 0)
                {
                    velocityXSmoothing = 0;
                    velocity.x = 0;

                    if (directionalInput.x != wallDirX && directionalInput.x != 0)
                    {
                        timeToWallUnstick -= Time.deltaTime;
                    }
                    else
                    {
                        timeToWallUnstick = Parameters.wallStickTime;
                    }
                }
                else
                {
                    timeToWallUnstick = Parameters.wallStickTime;
                }
            }
            else
            {
                wallSlidingTime = 0;
            }
        }
        else
        {
            wallSlidingTime = 0;
        }
    }

    /// <summary>
    /// Calcula la velocidad a la que se mueve el personaje, tambien aplica fuerza de gravedad sobre este.
    /// </summary>
    void CalculateVelocity()
    {
        if (!constantVelocity)
        {
            float targetVelocityX;
            float targetVelocityY;

            // Calcula la velocidad en x a la que se deveria mover el personaje
            targetVelocityX = directionalInput.x * ((controller.collisions.below) ? Parameters.moveSpeedOnGround : Parameters.moveSpeedOnAir);

            // Setea velocidad en x aplicando aceleracion.
            velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below) ? Parameters.accelerationTimeGrounded : Parameters.accelerationTimeAirborne);

            // Calcula la velocidad en y a la que se deveria mover el personaje
            targetVelocityY = velocity.y + (gravity * Time.deltaTime);

            // Limita la velocidad de caida para que no sea excesivamente rapida.
            if (targetVelocityY < Parameters.minGravity)
                targetVelocityY = Parameters.minGravity;

            // Setea velocidad en y aplicando aceleracion.
            velocity.y = Mathf.SmoothDamp(velocity.y, targetVelocityY, ref velocityYSmoothing, gravity * Time.deltaTime);
        }
        else
        {
            if ((Vector2.Distance(homingInitialPosition, transform.position) >= homingTargetDistance) ||
                (homingDirection.x > 0 && controller.collisions.right) || (homingDirection.x < 0 && controller.collisions.left) ||
                (homingDirection.y > 0 && controller.collisions.above) || (homingDirection.y < 0 && controller.collisions.below))
            {
                constantVelocity = false;

                velocity.y = 0;
                velocity.x = 0;

                // Velocidad actual 

                float targetVelocityX;
                float targetVelocityY;

                // Calcula la velocidad en x a la que se deveria mover el personaje
                targetVelocityX = directionalInput.x * ((controller.collisions.below) ? Parameters.moveSpeedOnGround : Parameters.moveSpeedOnAir);

                // Setea velocidad en x aplicando aceleracion.
                velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below) ? Parameters.accelerationTimeGrounded : Parameters.accelerationTimeAirborne);

                if (velocity.y > 0)
                    targetVelocityY = 0;
                else
                    targetVelocityY = Parameters.minGravity;

                // Limita la velocidad de caida para que no sea excesivamente rapida.
                if (targetVelocityY < Parameters.minGravity)
                    targetVelocityY = Parameters.minGravity;

                // Setea velocidad en y aplicando aceleracion.
                velocity.y = Mathf.SmoothDamp(velocity.y, targetVelocityY, ref velocityYSmoothing, 0.2f);
            }
        }
    }

    /// <summary>
    /// Calcula la gravedad en base a los parametros de salto del personaje.
    /// </summary>
    void CalculateGravity()
    {
        gravity = -(2 * Parameters.maxJumpHeight) / Mathf.Pow(Parameters.timeToJumpApex, 2);
        maxJumpVelocity = Mathf.Abs(gravity) * Parameters.timeToJumpApex;
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * Parameters.minJumpHeight);

        Debug.Log("Gravity: " + gravity);
        Debug.Log("Max Jump Velocity: " + maxJumpVelocity);
        Debug.Log("Min Jump Velocity: " + minJumpVelocity);
    }

    private void Damage()
    {
        if (canTakeDamage)
        {
            currentLifePoints -= 1;

            if (currentLifePoints <= 0)
            {
                Respawn();
                return;
            }
            velocity.y = minJumpVelocity;
            velocity.x = isFacingRight ? minJumpVelocity * -1 : minJumpVelocity * 1;
            _renderer.color = Color.red;
            canTakeDamage = false;
            Invoke("ResetStateAfterDamage", .2f);
        }
    }

    private void Respawn()
    {
        transform.position = checkpoint;

        currentLifePoints = Parameters.lifePoints;
    }

    private void ResetStateAfterDamage()
    {
        // Resetea el estado del personaje tras recibir daño
        _renderer.color = Color.white;
        canTakeDamage = true;
    }


    /// <summary>
    /// Resta tiempo a las variables de cooldowns y buffers
    /// </summary>
    /// <param name="t">Cantidad de tiempo a restar</param>
    private void SubstractCooldownAndBuffersTime(float t)
    {
        // Resta tiempo al cooldown del salto
        _jumpIn -= t;

        // Resta tiempo al buffer de input de salto.
        _jumpInputDownBuffer -= t;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        switch (collision.gameObject.layer)
        {
            //// Recibe daño
            //case Layers.HAZARD_INT:
            //case Layers.ENEMY_INT:
            //    this.Damage();
            //    break;
            // Actualiza el checkpoint
            case Layers.CHECKPOINT_INT:
                checkpoint = collision.transform.position;
                break;
        }
    }

    //private void OnTriggerStay2D(Collider2D collision)
    //{
    //    switch (collision.gameObject.layer)
    //    {
    //        // Recibe daño
    //        case Layers.HAZARD_INT:
    //        case Layers.ENEMY_INT:
    //            this.Damage();
    //            break;
    //    }
    //}

}