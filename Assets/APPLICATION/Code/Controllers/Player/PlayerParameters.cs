using UnityEngine;
using System;

[Flags]
public enum JumpBehavior
{
    CanJumpOnGround = 1,
    CanJumpOnAir = 1 << 1,
    CanJumpOnWall = 1 << 2
}

[Serializable]
public class PlayerParameters
{
    // Movement parameters
    public float moveSpeedOnGround = 8;
    public float moveSpeedOnAir = 8;
    public float accelerationTimeAirborne = 0.2f;
    public float accelerationTimeGrounded = 0.1f;

    // Jump Parameters
    [EnumFlag]
    public JumpBehavior JumpRestrictions;
    public float jumpFrequency = 0.25f;
    public int maxAirJumps = 0;
    public float maxJumpHeight = 2;
    public float minJumpHeight = 2;
    public float timeToJumpApex = 0.25f;
    public float jumpInputBuffer = 0.1f;

    // Gravity Parameters
    public float minGravity = -10f;

    // Homing parameters
    public float homingSpeed = 25f;
    public float homingOffset = 1.25f;
    public float homingMinDistance = 5f;
    public float homingButterflyMaxDistance = 8f;
    public float homingButterflyBaseSpeed = 15f;
    public float homingButterflyReturningSpeedFactor = 0.75f;

    // WallJump parameters
    public Vector2 wallJumpClimb;
    public Vector2 wallJumpOff;
    public Vector2 wallLeap;
    public AnimationCurve wallSlideSpeed;
    public float wallStickTime = 0.25f;

    // Life Parameters
    public int lifePoints = 5;
}
