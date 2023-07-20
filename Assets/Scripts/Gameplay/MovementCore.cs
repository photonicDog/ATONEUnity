using Assets.Scripts.Debug;
using Assets.Scripts.Gameplay.Controllers;
using Assets.Scripts.Gameplay.Input;
using Assets.Scripts.Gameplay.Interfaces;
using Assets.Scripts.Gameplay.Structures;
using Assets.Scripts.Gameplay.Types.Enums;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MovementCore : MonoBehaviour
{
    private Rigidbody _pRigidbody;
    private CapsuleCollider _pCollider;
    private InputManager _pInput;
    private Transform _pCamera;
    private RaycastHit _groundRaycast;
    private DebugData _debug;

    private IEntity _entity;
    public IMovement _movement;

    [SerializeField]
    float pGravity = -9.8f;
    [SerializeField]
    float pSpeedMax = 100f;
    [SerializeField]
    float pAirWishSpeedMax = 100f;
    [SerializeField]
    float moveSpeed = 12.0f;
    [SerializeField]
    float pGroundAccelerate = 100.0f;
    [SerializeField]
    float pAirAccelerate = 1.0f;
    [SerializeField]
    float pFriction = 6.0f;
    [SerializeField]
    float pStopSpeed = 100.0f;
    [SerializeField]
    float pJumpHeight = 10.0f;
    [SerializeField]
    float pStepOffset = 1.0f;
    [SerializeField]
    float bounceMod = 1.0f;
    [SerializeField]
    Vector3 pMoveInput;
    [SerializeField]
    Vector2 pViewDir;

    [SerializeField]
    float surfaceFriction = 1.0f;
    [SerializeField]
    bool jumpPressed;
    [SerializeField]
    float surfNormalYLimit = 0.7f;
    [SerializeField]
    float walkableSlopeAngleLimit = 45f;

    uint _totalJumps;
    private uint maxCollisions = 128;
    private uint maxClipPlanes = 6;
    private uint numBumps = 1;
    private Collider[] _colliders;
    private Vector3[] _planes;
    private GameObject ground;
    private Vector3 groundNormal;
    public float groundFriction = 1.0f;
    public float airFriction = 0.4f;

    public Vector3 slideDirection;
    public float slideSpeedCurrent;
    public float slideDelay = 0.5f;
    public bool _isSliding;
    public bool _wasSliding;
    public float maxSlideSpeed = 18f;
    public float minSlideSpeed = 9f;
    public float slideSpeedMod = 1.75f;
    public float downSlideSpeedMod = 2.5f;
    public float slideFriction = 14f;

    private LayerMask _groundLayerMask;

    private Vector3 yKill = new Vector3(1, 0, 1);

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        _pRigidbody = GetComponent<Rigidbody>();
        _pCollider = GetComponent<CapsuleCollider>();
        _pInput = InputManager.Instance;
        _debug = DebugData.Instance;
        pMoveInput = new Vector3();
        pViewDir = new Vector2();
        _pCamera = Camera.main.transform;
        _entity = new PlayerEntity(new PlayerMovement());
        _movement = _entity.Movement;
        _colliders = new Collider[maxCollisions];
        _planes = new Vector3[maxClipPlanes];
        _groundLayerMask = LayerMask.GetMask("Ground");
    }

    void Update()
    {
        if(_pInput.GetAltFire())
        {
            _movement.Velocity = Vector3.zero;
            transform.position = new Vector3(-16.9454803f, 2.74504304f, 101.066254f);
            FindObjectOfType<VirtualCameraController>().transform.rotation = Quaternion.Euler(-10, 0, 0);
        }

        if(_pInput.StartedJumping())
        {
            jumpPressed = true;
        }

        if(_pInput.StoppedJumping())
        {
            jumpPressed = false;
        }

        pViewDir = _pCamera.transform.forward;
        pMoveInput = GetInputVector();

        //////////////////////
        /// MOVEMENT LOGIC ///
        //////////////////////
        
        if (!_movement.IsGrounded)
        {
            _movement.Velocity += new Vector3(0, pGravity * Time.deltaTime, 0);
        }

        _movement.IsGrounded = CheckGround();

        if (_movement.IsGrounded)
        {
            if (!_wasSliding)
            {
                slideDirection = new Vector3(_movement.Velocity.x, 0f, _movement.Velocity.z).normalized;
                slideSpeedCurrent = Mathf.Max(maxSlideSpeed, new Vector3(_movement.Velocity.x, 0f, _movement.Velocity.z).magnitude);
            }

            _isSliding = false;
            if (_movement.Velocity.magnitude > minSlideSpeed && slideDelay <= 0f && Vector3.Angle(Vector3.up, groundNormal) > 5)
            {
                if (!_wasSliding)
                {
                    slideSpeedCurrent = Mathf.Clamp(slideSpeedCurrent * slideSpeedMod, minSlideSpeed, maxSlideSpeed);

                    _isSliding = true;
                    _wasSliding = true;
                    SlideMovement();
                }
            }
            else
            {
                if (slideDelay > 0f)
                {
                    slideDelay -= Time.deltaTime;
                }

                if (_wasSliding)
                {
                    slideDelay = 0.5f;
                }

                _wasSliding = false;
            }
        }

        if (jumpPressed && _movement.IsGrounded)
        {
            _totalJumps += 1;
            _movement.Velocity = new Vector3(_movement.Velocity.x, pJumpHeight, _movement.Velocity.z);
            _movement.IsGrounded = false;
            surfaceFriction = 0f;

            _debug.JumpedThisFrame = true;
        }
        else if (_movement.IsGrounded)
        {
            _movement.Velocity = ApplyFriction(_movement.Velocity);
            _movement.WishVelocity = GetNextFrameWishDirection(pMoveInput, _groundRaycast) * moveSpeed;
            _movement.Velocity = GetNewGroundVelocity(_movement.WishDirection, _movement.WishSpeed, _movement.Velocity, _groundRaycast);
        }
        else
        {
            _wasSliding = false;
            _movement.WishVelocity = GetNextFrameWishDirection(pMoveInput, _groundRaycast, false);
            _movement.Velocity = GetNewAirVelocity(_movement.WishDirection, _movement.WishSpeed, _movement.Velocity, _groundRaycast);
            var vel = _movement.Velocity;
            TryMove(transform.position, ref vel, _pCollider, Time.deltaTime);
            _movement.Velocity = vel;
        }

        transform.position += _movement.Velocity*Time.deltaTime;

        ResolveCollisions();

        Debug.DrawLine(transform.position, _movement.Velocity + transform.position, Color.green); //velocity
        Debug.DrawLine(transform.position, _movement.WishDirection * 3 + transform.position, Color.blue); //wish dir
        Debug.DrawLine(transform.position, _pCamera.forward.normalized * 3 + transform.position, Color.red); //facing
    }

    // DEBUG
    private void LateUpdate()
    {
        _debug.CurrentVelocity = _movement.Velocity;
        _debug.CurrentSpeed = _movement.Velocity.magnitude;
        _debug.CurrentInput = pMoveInput;
        _debug.CurrentViewAngle = _pCamera.eulerAngles;
        _debug.Position = gameObject.transform.position;

        _debug.Grounded = _movement.IsGrounded;
        _debug.JumpPressed = jumpPressed;
    }

    public bool CheckGround()
    {
        var down = _pCollider.transform.position;
        down.y -= 0.15f;

        var castInfoDebug = CheckCollision(_pCollider, _pCollider.transform.position, down, _groundLayerMask);

        if (castInfoDebug.collider != null)
        {
            if (castInfoDebug.collider.tag == "debug")
            {
                Debug.Log("Touched slope");
            }
        }

        var castInfo = CheckCollision(_pCollider, _pCollider.transform.position, down, _groundLayerMask);
        if (castInfo.collider == null || Vector3.Angle(Vector3.up, castInfo.normal) > walkableSlopeAngleLimit || (jumpPressed && _movement.Velocity.y > 0f))
        {
            ground = null;
            groundNormal = Vector3.zero;

            if (_movement.Velocity.y > 0f)
            {
                surfaceFriction = airFriction;
            }

            return false;
        } else
        {
            ground = castInfo.collider.gameObject;
            groundNormal = castInfo.normal;
            surfaceFriction = groundFriction;
            Vector3.Scale(_movement.Velocity, yKill);
            return true;
        }
    }

    public Vector3 GetInputVector()
    {
        var input = _pInput.GetPlayerMovement();
        return new Vector3(input.y, 0, input.x);
        //Z is forward, X is right
    }

    private Vector3 GetNextFrameWishDirection(Vector3 moveDir, RaycastHit hit, bool yKill = true)
    {
        var vectors = GetLookAtAsVectors(_pCamera.transform);
        var forward = vectors.forward;
        var right = vectors.right;
        if (forward.y != 0 && yKill)
        {
            forward.y = 0;
            forward.Normalize();
        }
        if (right.y != 0 && yKill)
        {
            right.y = 0;
            right.Normalize();
        }

        //Debug.Log($"WishDir returned: {new Vector3(moveDir.x * forward.x + moveDir.y * right.x, 0, moveDir.x * forward.z + moveDir.y * right.z)}");

        return new Vector3(moveDir.x*forward.x + moveDir.z*right.x, moveDir.x * forward.y + moveDir.z * right.y, moveDir.x*forward.z + moveDir.z*right.z);
    }

    private Vector3 GetNewGroundVelocity(Vector3 wishDir, float wishSpeed, Vector3 prevFrameVel, RaycastHit hit)
    {

        Vector3 forwardVel = Vector3.Cross(groundNormal, Quaternion.AngleAxis(-90, Vector3.up) * new Vector3(_movement.Velocity.x, 0f, _movement.Velocity.z));

        var yVel = _movement.Velocity.y * Vector3.up;
        var newVel = Vector3.Scale(Accelerate(wishDir, wishSpeed, prevFrameVel, pGroundAccelerate), yKill);

        _movement.Velocity = Vector3.ClampMagnitude(new Vector3(newVel.x, 0, newVel.z), pSpeedMax) + yVel;

        var ySpeed = forwardVel.normalized.y * new Vector3(_movement.Velocity.x, 0, _movement.Velocity.z).magnitude;
        _movement.Velocity = new Vector3(_movement.Velocity.x, ySpeed * (wishDir.y < 0f ? 1.2f : 1.0f), _movement.Velocity.z);

        return newVel;
    }

    private Vector3 GetNewAirVelocity(Vector3 wishDir, float wishSpeed, Vector3 prevFrameVel, RaycastHit hit)
    {
        if (wishSpeed > pAirWishSpeedMax)
        {
            wishSpeed = pAirWishSpeedMax;
            Debug.Log("Wishspeed Capped");
        }

        var newVel = prevFrameVel + Accelerate(wishDir, wishSpeed, prevFrameVel, pAirAccelerate);
        newVel = newVel.magnitude > pSpeedMax ? newVel.normalized * pSpeedMax : newVel;

        return newVel;
    }

    private Vector3 Accelerate(Vector3 wishDir, float wishSpeed, Vector3 currentVel, float accelerate)
    {
        if(!_movement.IsGrounded)
        {
            wishSpeed = Mathf.Min(wishSpeed, pAirWishSpeedMax);
        }

        // project current velocity onto wish direction, then calculate following instance of accelerated speed
        var projectedSpeed = Vector3.Dot(currentVel, wishDir);
        var addSpeed = wishSpeed - projectedSpeed;
        if (addSpeed <= 0f)
        {
            return Vector3.zero;
        }

        var accelSpeed = accelerate * Time.deltaTime * wishSpeed;

        if (_movement.IsGrounded) accelSpeed *= surfaceFriction;

        if(accelSpeed > addSpeed)
        {
            accelSpeed = addSpeed;
        }

        //Debug.Log($"Accelerate returned: {prevFrameVel + (nextFrameAccel * nextFrameWishDir)}");

        return accelSpeed * wishDir;
    }

    private Vector3 ApplyFriction(Vector3 prevFrameVel)
    {
        Vector3.Scale(prevFrameVel, yKill);
        float speed = prevFrameVel.magnitude;

        if (speed < 0.0001f)
        {
            return Vector3.zero;
        }

        float drop = 0f;

        float control = speed < pStopSpeed ? pStopSpeed : speed;
        drop += control * pFriction * Time.deltaTime;

        var newspeed = speed - drop < 0 ? 0 : speed - drop;

        if (newspeed != speed)
        {
            newspeed /= speed;
        }

        return prevFrameVel * newspeed;
    }

    public AngleVectors GetLookAtAsVectors(Transform transform)
    {
        // 1. convert quarternions into euler angles (yaw, pitch, roll)
        // 2. convert euler angles into vectors
        // note: unity is weird, so Z is pitch Y is yaw and X is roll
        AngleVectors result;
        var roll = Mathf.Deg2Rad * RelativeAngle(transform.eulerAngles.x);
        var yaw = Mathf.Deg2Rad * RelativeAngle(transform.eulerAngles.y);
        var pitch = Mathf.Deg2Rad * RelativeAngle(transform.eulerAngles.z);

        var sp = Mathf.Sin(pitch);
        var cp = Mathf.Cos(pitch);
        var sy = Mathf.Sin(yaw);
        var cy = Mathf.Cos(yaw);
        var sr = Mathf.Sin(roll);
        var cr = Mathf.Cos(roll);

        //lookat vector also needs to be adapted for this
        //result.forward = new Vector3(cp * cy, -sp, cp * sy);
        //result.right = new Vector3(-1 * sr * sp * cy + (-1 * cr * -sy), -1 * sr * cp, (-1 * sr * sp * sy) + (-1 * cr * cy));
        //result.up = new Vector3(cr * sp * cy + (-sr * -sy), cr * cp, (cr * sp * sy) + (-sr * cy));
        result.forward = _pCamera.forward;
        result.right = _pCamera.right;
        result.up = _pCamera.up;

        //Debug.Log($"X: {transform.eulerAngles.x}, Y: {transform.eulerAngles.y}, Z: {transform.eulerAngles.z}"); 

        return result;
    }

    public float RelativeAngle(float angle)
    {
        return angle > 180 ? angle - 360 : angle;
    }

    public void ResolveCollisions()
    {
        var distToPoint = _pCollider.height / 2f - _pCollider.radius;
        var point1 = _pCollider.transform.position + _pCollider.center + Vector3.up * distToPoint;
        var point2 = _pCollider.transform.position + _pCollider.center + Vector3.down * distToPoint;
        var velocity = _movement.Velocity;

        int overlaps = Physics.OverlapCapsuleNonAlloc(point1, point2, _pCollider.radius, _colliders, _groundLayerMask, QueryTriggerInteraction.Ignore);
        if (overlaps == 0) return;

        var groundVel = Vector3.Scale(_movement.Velocity, yKill);
        foreach (var collider in _colliders)
        {
            if (collider != null)
            {
                Vector3 direction;
                float distance;

                if (Physics.ComputePenetration(_pCollider, _pCollider.transform.position, Quaternion.identity,
                                                collider, collider.transform.position, collider.transform.rotation, out direction, out distance))
                {
                    //check if we allow steps, if so, do it. and if that doesnt work or it isnt just move on
                    if (pStepOffset > 0f)
                    {
                        if (StepOffset(_pCollider.transform.position, velocity, groundVel))
                        {
                            return;
                        }
                    }

                    //use the penetration computation to move the player away from it and set the velocity back (vis a vis laws of motion)
                    direction.Normalize();
                    Vector3 penetrationVector = direction * distance;
                    Vector3 projectedVelocity = Vector3.Project(_movement.Velocity, -direction);
                    projectedVelocity.y = 0;
                    _pCollider.transform.position += penetrationVector;
                    velocity -= projectedVelocity;
                }
            }
        }
    }

    public bool StepOffset(Vector3 position, Vector3 velocity, Vector3 forwardVelocity)
    {
        //dont bother if there's no offset
        if (pStepOffset < 0f) return false;

        //or if they're not moving horizontally
        var forwardDirection = forwardVelocity.normalized;
        if (forwardDirection.sqrMagnitude == 0f) return false;

        var center = _pCollider.center;
        var radius = _pCollider.radius;
        var height = _pCollider.height;
        var distToPoint = height / 2f - radius;
        var point1 = center + Vector3.up * distToPoint;
        var point2 = center + Vector3.down * distToPoint;
        var contactOffset = _pCollider.contactOffset;

        //ground check
        var groundCast = CastCapsule(point1, point2, radius, position, position + Vector3.down * 0.1f,
                                     contactOffset, _groundLayerMask);
        if (groundCast.collider == null || Vector3.Angle(Vector3.up, groundCast.normal) > walkableSlopeAngleLimit)
        {
            return false;
        }

        //wall check (lower collider scale to not hit floor/ceiling)
        var wallCast = CastCapsule(point1, point2, radius, position, position + velocity,
                                   contactOffset, _groundLayerMask, 0.9f);
        if (wallCast.collider == null || Vector3.Angle(Vector3.up, wallCast.normal) <= walkableSlopeAngleLimit)
        {
            return false;
        }

        //ceiling check
        var upDistance = pStepOffset;
        var ceilingCast = CastCapsule(point1, point2, radius, position, position + Vector3.up * pStepOffset,
                                      contactOffset, _groundLayerMask);
        if (ceilingCast.collider != null)
        {
            upDistance = ceilingCast.distance;
        }

        //updistance <= 0 means the ceiling is directly above us, so dont bother stepping
        if (upDistance <= 0) return false;

        var nextStepPos = position + Vector3.up * upDistance;

        //forward movement check at next step
        float forwardMagnitude = pStepOffset;
        float forwardDistance = forwardMagnitude;
        var forwardCast = CastCapsule(point1, point2, radius, nextStepPos, nextStepPos + forwardDirection * Mathf.Max(0.2f, forwardMagnitude),
                                      contactOffset, _groundLayerMask);
        if (forwardCast.collider != null)
        {
            forwardDistance = forwardCast.distance;
        }

        nextStepPos += forwardDirection * forwardDistance;

        //step ground check
        float downDistance = upDistance;
        var downCast = CastCapsule(point1, point2, radius, nextStepPos, nextStepPos + Vector3.down * upDistance, contactOffset, _groundLayerMask);
        if (downCast.collider != null)
        {
            downDistance = downCast.distance;
        }

        float verticalDistance = Mathf.Clamp(upDistance - downDistance, 0f, pStepOffset);
        float stepAngle = Vector3.Angle(Vector3.forward, new Vector3(0f, verticalDistance, forwardDistance));
        if (stepAngle > walkableSlopeAngleLimit)
        {
            return false;
        }

        nextStepPos = position + Vector3.up * verticalDistance;

        if (position != nextStepPos && forwardDistance > 0f)
        {
            _pRigidbody.position = nextStepPos + forwardDirection * forwardDistance * Time.deltaTime;
            return true;
        }
        else
        {
            return false;
        }
    }

    public TryMoveResult TryMove(Vector3 origin, ref Vector3 velocity, CapsuleCollider collider, float deltaTime)
    {
        float d;
        var originalVel = velocity;
        var primalVel = velocity; //i love this variable name
        var newVel = new Vector3();
        var timeLeft = deltaTime;
        var allFraction = 0f;
        var numPlanes = 0;
        TryMoveResult blocked = TryMoveResult.Unblocked;

        for (int bumpcount = 0; bumpcount < numBumps; bumpcount++)
        {

            //end if stopped
            if (_movement.Velocity.magnitude == 0f)
            {
                break;
            }

            var dest = origin + velocity * timeLeft;
            var castInfo = CheckCollision(collider, origin, dest, _groundLayerMask);

            allFraction += castInfo.fraction;

            if (castInfo.fraction > 0f)
            {
                originalVel = velocity;
                numPlanes = 0;
            }

            //fraction = 1 means we made the whole journey. woo!
            if (castInfo.fraction == 1f)
            {
                break;
            }

            //if the normal is 1 its a floor. if its between 1 and 0.7 its a surf slope
            if (castInfo.normal.y > surfNormalYLimit)
            {
                blocked = TryMoveResult.BlockedByFloor;
            }

            //if the normal's y is zero its a wall or a step
            if (castInfo.normal.y == 0)
            {
                blocked = TryMoveResult.BlockedByWall;
            }

            timeLeft -= timeLeft * castInfo.fraction;

            //so this is here in case we run out of planes to clip against
            //but im going to be real i dont think this is relevant or does anything
            //clip planes are invisible walls with funny properties in the source engine (unrelated to rendering clip planes (thanks valve))
            //the distinction is not super relevant here? but any object we might run into counts as a clip plane for this
            //so im still implementing it but. now you know i feel weird about it
            if (numPlanes >= maxClipPlanes)
            {
                velocity = Vector3.zero;
                Debug.Log("Too many clip planes??? How did you get this. Please email me right now so I can study you.");
                break;
            }

            _planes[numPlanes] = castInfo.normal;
            numPlanes++;

            if (numPlanes == 1)
            {
                if (_planes[0].y > surfNormalYLimit)
                {
                    return blocked;
                } else
                {
                    ClipVelocity(originalVel, _planes[0], ref newVel, 1 + bounceMod * (1 - surfaceFriction));
                }

                velocity = newVel;
                originalVel = newVel;
            }
            else
            {
                for (int i=0; i < numPlanes; i++)
                {
                    newVel = _movement.Velocity;
                    ClipVelocity(originalVel, _planes[0], ref newVel, 1);

                    for (int j=0; j < numPlanes; j++)
                    {
                        if (j!=1)
                        {
                            if (Vector3.Dot(_movement.Velocity, _planes[j]) < 0)
                            {
                                break;
                            }
                        }
                        if (j == numPlanes)
                        {
                            break;
                        }
                    }
                    if (i == numPlanes)
                    {
                        if (numPlanes != 2)
                        {
                            _movement.Velocity = origin;
                            break;
                        }
                        var dir = Vector3.Cross(_planes[0], _planes[1]).normalized;
                        d = Vector3.Dot(dir, velocity);
                        velocity = dir * d;
                    }

                    d = Vector3.Dot(velocity, primalVel);
                    if (d <= 0f)
                    {
                        velocity = Vector3.zero;
                        break;
                    }
                }
            }

        }

        if (allFraction == 0f)
        {
            velocity = Vector3.zero;
        }
        return blocked;
    }

    public void SlideMovement()
    {
        slideDirection += new Vector3(groundNormal.x, 0f, groundNormal.z) * slideSpeedCurrent * Time.deltaTime;
        slideDirection = slideDirection.normalized;

        var slideForward = Vector3.Cross(groundNormal, Quaternion.AngleAxis(-90, Vector3.up) * slideDirection);

        slideSpeedCurrent -= slideFriction * Time.deltaTime;
        slideSpeedCurrent = Mathf.Clamp(slideSpeedCurrent, 0f, maxSlideSpeed);
        slideSpeedCurrent -= (slideForward * slideSpeedCurrent).y * Time.deltaTime * slideSpeedMod;

        _movement.Velocity = slideForward * slideSpeedCurrent;
    }

    public TryMoveResult ClipVelocity(Vector3 input, Vector3 normal, ref Vector3 output, float overbounce)
    {
        var angle = normal[1];
        var blocked = TryMoveResult.Unblocked;

        if (angle > 0f)
        {
            blocked = TryMoveResult.BlockedByFloor;
        }

        if (angle == 0f)
        {
            blocked = TryMoveResult.BlockedByWall;
        }

        var backoff = Vector3.Dot(input, normal) * overbounce;
        var change = normal * backoff;
        output = input - change;

        float adjust = Vector3.Dot(output, normal);
        if (adjust < 0.0f)
        {
            output -= normal * adjust;
        }

        return blocked;
    }

    public CastInfo CastCapsule(Vector3 point1, Vector3 point2, float radius, Vector3 start, Vector3 destination, float contactOffset, int layerMask, float colliderScale = 1f)
    {
        CastInfo castInfo = new();
        var longSide = Mathf.Sqrt(2 * (contactOffset * contactOffset));
        radius *= 1f - contactOffset;
        var direction = (destination - start).normalized;
        var maxDistance = Vector3.Distance(start, destination) + longSide;

        //Debug.DrawLine(point1 + Vector3.down * colliderScale * 0.5f, point1 + Vector3.down * colliderScale * 0.5f + (Vector3.right * 5f), Color.red);
        //Debug.DrawLine(point2 + Vector3.up * colliderScale * 0.5f, point2 + Vector3.up * colliderScale * 0.5f + (Vector3.right * 5f), Color.red);
        RaycastHit hit;
        if (Physics.CapsuleCast(point1 + Vector3.down*(1 - colliderScale)*0.5f, point2 + Vector3.up*(1 - colliderScale)*0.5f, radius * colliderScale,
                                direction, out hit, maxDistance, layerMask, QueryTriggerInteraction.Ignore)) {
            castInfo.fraction = hit.distance / maxDistance;
            castInfo.collider = hit.collider;
            castInfo.collisionPoint = hit.point;
            castInfo.normal = hit.normal;
            castInfo.distance = hit.distance;

            RaycastHit normalCheckHit;
            var normalRay = new Ray(hit.point - direction * 0.001f, direction);
            if (hit.collider.Raycast(normalRay, out normalCheckHit, 0.002f))
            {
                castInfo.normal = normalCheckHit.normal;
            }
        }
        else
        {
            castInfo.fraction = 1f;
        }

        return castInfo;
    }

    public CastInfo CheckCollision(CapsuleCollider collider, Vector3 start, Vector3 destination, LayerMask layerMask, float colliderScale = 1.0f)
    {
        var distToPoint = collider.height / 2f - collider.radius;
        var point1 = _pCollider.transform.position + _pCollider.center + Vector3.up * distToPoint;
        var point2 = _pCollider.transform.position + _pCollider.center + Vector3.down * distToPoint;
        return CastCapsule(point1, point2, collider.radius, start, destination, collider.contactOffset, layerMask, colliderScale);
    }
}
