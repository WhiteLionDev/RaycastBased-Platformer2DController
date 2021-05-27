using UnityEngine;

public class HomingButterfly : MonoBehaviour
{
    private float baseSpeed = 15f;
    private float returningSpeedFactor = 0.5f;
    public Transform target;
    public Transform targetDefaultSpot;

    private Vector3 targetOriginalPosition;

    [HideInInspector]
    public bool traveling = false;
    private bool returning = false;

    void Update()
    {
        if (!traveling)
            target.position = targetDefaultSpot.position;

        if (target.position != transform.position)
        {
            float speed = baseSpeed;

            if (returning)
                speed *= returningSpeedFactor;

            transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

            if (traveling)
            {
                if (Vector2.Distance(transform.position, target.position) < 0.1f)
                {
                    if (!returning)
                    {
                        target.position = targetOriginalPosition;
                        returning = true;
                    }
                    else
                    {
                        target.position = targetDefaultSpot.position;
                        transform.position = targetDefaultSpot.position;
                        returning = false;
                        traveling = false;
                    }
                }
            }
        }
    }

    public void Move(Vector3 newPosition, Vector3 playerPosition, float baseSpeed, float returningSpeedFactor)
    {
        if (!traveling)
        {
            this.baseSpeed = baseSpeed;
            this.returningSpeedFactor = returningSpeedFactor;
            transform.position = playerPosition;
            targetOriginalPosition = playerPosition;
            target.position = newPosition;
            traveling = true;
        }
    }

    public void Return(Vector3 newPosition, float speed)
    {
        if (traveling)
        {
            this.baseSpeed = speed;
            this.returningSpeedFactor = 1;
            target.position = newPosition;
            returning = true;
        }
    }
}
