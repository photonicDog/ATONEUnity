using Assets.Scripts.Extensions;
using Assets.Scripts.Gameplay.Models;
using Assets.Scripts.Gameplay.Models.Configurations;
using Assets.Scripts.Gameplay.Structures;
using Assets.Scripts.Gameplay.Types.Enums;
using UnityEngine;

namespace Assets.Scripts.Gameplay.Components
{
    public class PhysicsComponent
    {
        private PhysicsEntity _entity;
        private PhysicsConfig _config;

        private LayerMask _groundLayerMask;

        public PhysicsComponent()
        {
            var config = new PhysicsConfig();
            SetInitValues(new PhysicsEntity(config), config);
        }

        public PhysicsComponent(PhysicsEntity entity, PhysicsConfig config)
        {
            SetInitValues(entity, config);
        }

        private void SetInitValues(PhysicsEntity entity, PhysicsConfig config)
        {
            _entity = entity;
            _config = config;
            _groundLayerMask = LayerMask.GetMask("Ground");
        }

        //////////////////
        //// HELPER

        public void AlignPosition(Vector3 position)
        {
            _entity.Position = position;
        }

        public void Impulse(Vector3 impulse)
        {
            _entity.Velocity += impulse;
        }

        public void Burst(Vector3 burst)
        {
            // stops velocity in that direction, then adds on the burst (for jumps)
            _entity.Velocity = Vector3.Scale(_entity.Velocity, Vector3.one - burst.normalized) + burst;
        }

        public void KillVelocity()
        {
            _entity.Velocity = Vector3.zero;
        }

        //////////////////
        //// STATES

        public void SetJumpState(bool input)
        {
            _entity.IsJumping = input;
        }

        public void CheckGravity()
        {
            if (_entity.IsGrounded == false)
            {
                Impulse(Vector3.up * _config.Gravity * _config.GravityMod * Time.deltaTime);
            }
        }

        public void StopSliding()
        {
            _entity.IsSliding = false;
            _entity.WasSliding = false;
        }

        //////////////////
        //// COLLISIONS

        public bool CheckGround(CapsuleCollider entCollider)
        {
            var down = entCollider.transform.position;
            down.y -= 0.15f;

            var castInfoDebug = CheckCollision(entCollider, entCollider.transform.position, down, _groundLayerMask);

            if (castInfoDebug.collider != null)
            {
                if (castInfoDebug.collider.tag == "debug")
                {
                    //Debug.Log("Touched slope");
                }
            }

            var castInfo = CheckCollision(entCollider, entCollider.transform.position, down, _groundLayerMask);
            if (castInfo.collider == null || Vector3.Angle(Vector3.up, castInfo.normal) > _config.SlopeAngleLimit || (_entity.IsJumping && _entity.Velocity.y > 0f))
            {
                _entity.IsGrounded = false;
                _entity.Ground = null;
                _entity.GroundNormal = Vector3.zero;

                if (_entity.Velocity.y > 0f)
                {
                    _entity.SurfaceFriction = _config.AirFriction;
                }

                return false;
            }
            else
            {
                _entity.IsGrounded = true;
                _entity.Ground = castInfo.collider.gameObject;
                _entity.GroundNormal = castInfo.normal;
                _entity.SurfaceFriction = _config.GroundFriction;
                _entity.Velocity = _entity.Velocity.KillY();
                return true;
            }
        }
        public bool CheckSliding()
        {
            if (_entity.WasSliding == false)
            {
                _entity.SlideDirection = _entity.Velocity.KillY().normalized;
                _entity.CurrentSlideSpeed = Mathf.Max(_config.MaxSlideSpeed, _entity.Velocity.KillY().magnitude);
            }

            _entity.IsSliding = false;
            if (_entity.Speed > _config.MinSlideSpeed && _config.SlideDelay <= 0f && Vector3.Angle(Vector3.up, _entity.GroundNormal) > 5)
            {
                if (_entity.WasSliding == false)
                {
                    _entity.CurrentSlideSpeed = Mathf.Clamp(_entity.CurrentSlideSpeed * _config.SlideSpeedMod, _config.MinSlideSpeed, _config.MaxSlideSpeed);

                    _entity.IsSliding = true;
                    _entity.WasSliding = true;
                    SlideMovement();
                }
            }
            else
            {
                if (_entity.SlideDelayTimer > 0f)
                {
                    _entity.SlideDelayTimer -= Time.deltaTime;
                }

                if (_entity.WasSliding == true)
                {
                    _entity.SlideDelayTimer = 0.5f;
                }

                _entity.WasSliding = false;
            }

            return _entity.IsSliding;
        }
        public void ResolveCollisions(CapsuleCollider entCollider)
        {
            var distToPoint = entCollider.height / 2f - entCollider.radius;
            var point1 = entCollider.transform.position + entCollider.center + Vector3.up * distToPoint;
            var point2 = entCollider.transform.position + entCollider.center + Vector3.down * distToPoint;
            var velocity = _entity.Velocity;

            int overlaps = Physics.OverlapCapsuleNonAlloc(point1, point2, entCollider.radius, _entity.Collisions, _groundLayerMask, QueryTriggerInteraction.Ignore);
            if (overlaps == 0) return;

            var groundVel = _entity.Velocity.KillY();
            foreach (var collider in _entity.Collisions)
            {
                if (collider != null)
                {
                    Vector3 direction;
                    float distance;

                    if (Physics.ComputePenetration(entCollider, entCollider.transform.position, Quaternion.identity,
                                                    collider, collider.transform.position, collider.transform.rotation, out direction, out distance))
                    {
                        //check if we allow steps, if so, do it. and if that doesnt work or it isnt just move on
                        if (_config.StepOffset > 0f)
                        {
                            if (StepOffset(entCollider, entCollider.transform.position, velocity, groundVel))
                            {
                                return;
                            }
                        }

                        //use the penetration computation to move the player away from it and set the velocity back (vis a vis laws of motion)
                        direction.Normalize();
                        Vector3 penetrationVector = direction * distance;
                        Vector3 projectedVelocity = Vector3.Project(_entity.Velocity, -direction);
                        projectedVelocity.y = 0;
                        entCollider.transform.position += penetrationVector;
                        velocity -= projectedVelocity;
                    }
                }
            }
        }
        public CastInfo CheckCollision(CapsuleCollider collider, Vector3 start, Vector3 destination, LayerMask layerMask, float colliderScale = 1.0f)
        {
            var distToPoint = collider.height / 2f - collider.radius;
            var point1 = collider.transform.position + collider.center + Vector3.up * distToPoint;
            var point2 = collider.transform.position + collider.center + Vector3.down * distToPoint;
            return CastCapsule(point1, point2, collider.radius, start, destination, collider.contactOffset, layerMask, colliderScale);
        }
        private CastInfo CastCapsule(Vector3 point1, Vector3 point2, float radius, Vector3 start, Vector3 destination, float contactOffset, int layerMask, float colliderScale = 1f)
        {
            CastInfo castInfo = new();
            var longSide = Mathf.Sqrt(2 * (contactOffset * contactOffset));
            radius *= 1f - contactOffset;
            var direction = (destination - start).normalized;
            var maxDistance = Vector3.Distance(start, destination) + longSide;

            //Debug.DrawLine(point1 + Vector3.down * colliderScale * 0.5f, point1 + Vector3.down * colliderScale * 0.5f + (Vector3.right * 5f), Color.red);
            //Debug.DrawLine(point2 + Vector3.up * colliderScale * 0.5f, point2 + Vector3.up * colliderScale * 0.5f + (Vector3.right * 5f), Color.red);
            RaycastHit hit;
            if (Physics.CapsuleCast(point1 + Vector3.down * (1 - colliderScale) * 0.5f, point2 + Vector3.up * (1 - colliderScale) * 0.5f, radius * colliderScale,
                                    direction, out hit, maxDistance, layerMask, QueryTriggerInteraction.Ignore))
            {
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


        //////////////////
        //// MOVEMENT

        public Vector3 ProcessMovement(CapsuleCollider collider, Vector2 moveInput, float runSpeed, AngleVectors lookDirection, bool jumpInput, float jumpHeight, float deltaTime)
        {
            SetJumpState(jumpInput);
            CheckGravity();
            CheckGround(collider);
            if (_entity.IsGrounded)
            {
                CheckSliding();
            }

            if (_entity.IsJumping && _entity.IsGrounded)
            {
                Jump(jumpHeight);
            }
            else if (_entity.IsGrounded)
            {
                WalkMove(moveInput, lookDirection, runSpeed);
            }
            else
            {
                AirMove(moveInput, lookDirection);
                TryMove(collider, deltaTime);
            }

            MovePosition(deltaTime);

            ResolveCollisions(collider);

            return _entity.Position;
        }
        public void Jump(float jumpHeight)
        {
            Burst(jumpHeight * Vector3.up);
            _entity.IsGrounded = false;
            _entity.SurfaceFriction = 0f;
        }
        public void WalkMove(Vector2 moveInput, AngleVectors lookAt, float speed)
        {
            _entity.Velocity = ApplyFriction(_entity.Velocity);
            _entity.WishVelocity = GetNextFrameWishDirection(lookAt, moveInput) * speed;
            _entity.Velocity = GetNewGroundVelocity(_entity.WishDirection, _entity.WishSpeed, _entity.Velocity);
        }
        public void AirMove(Vector2 moveInput, AngleVectors lookAt)
        {
            _entity.WasSliding = false;
            _entity.WishVelocity = GetNextFrameWishDirection(lookAt, moveInput, false);
            _entity.Velocity = GetNewAirVelocity(_entity.WishDirection, _entity.WishSpeed, _entity.Velocity);
        }
        public void MovePosition(float deltaTime)
        {
            _entity.Position += _entity.Velocity * deltaTime;
        }
        private Vector3 GetNextFrameWishDirection(AngleVectors vectors, Vector3 moveDir, bool yKill = true)
        {
            //var vectors = GetLookAtAsVectors(_pCamera.transform);
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

            return new Vector3(moveDir.x * forward.x + moveDir.z * right.x, moveDir.x * forward.y + moveDir.z * right.y, moveDir.x * forward.z + moveDir.z * right.z);
        }
        private Vector3 GetNewGroundVelocity(Vector3 wishDir, float wishSpeed, Vector3 prevFrameVel)
        {

            Vector3 forwardVel = Vector3.Cross(_entity.GroundNormal, Quaternion.AngleAxis(-90, Vector3.up) * new Vector3(_entity.Velocity.x, 0f, _entity.Velocity.z));

            var yVel = _entity.Velocity.y * Vector3.up;
            var newVel = Accelerate(wishDir, wishSpeed, prevFrameVel, _config.GroundAccelerate).KillY();

            _entity.Velocity = Vector3.ClampMagnitude(new Vector3(newVel.x, 0, newVel.z), _config.MaxSpeed) + yVel;

            var ySpeed = forwardVel.normalized.y * new Vector3(_entity.Velocity.x, 0, _entity.Velocity.z).magnitude;
            _entity.Velocity = new Vector3(_entity.Velocity.x, ySpeed * (wishDir.y < 0f ? 1.2f : 1.0f), _entity.Velocity.z);

            return newVel;
        }
        private Vector3 GetNewAirVelocity(Vector3 wishDir, float wishSpeed, Vector3 prevFrameVel)
        {
            if (wishSpeed > _config.MaxAirWishSpeed)
            {
                wishSpeed = _config.MaxAirWishSpeed;
                //Debug.Log("Wishspeed Capped");
            }

            var newVel = prevFrameVel + Accelerate(wishDir, wishSpeed, prevFrameVel, _config.AirAccelerate);
            newVel = newVel.magnitude > _config.MaxSpeed ? newVel.normalized * _config.MaxSpeed : newVel;

            return newVel;
        }
        private Vector3 Accelerate(Vector3 wishDir, float wishSpeed, Vector3 currentVel, float accelerate)
        {
            if (!_entity.IsGrounded)
            {
                wishSpeed = Mathf.Min(wishSpeed, _config.MaxAirWishSpeed);
            }

            // project current velocity onto wish direction, then calculate following instance of accelerated speed
            var projectedSpeed = Vector3.Dot(currentVel, wishDir);
            var addSpeed = wishSpeed - projectedSpeed;
            if (addSpeed <= 0f)
            {
                return Vector3.zero;
            }

            var accelSpeed = accelerate * Time.deltaTime * wishSpeed;

            if (_entity.IsGrounded) accelSpeed *= _entity.SurfaceFriction;

            if (accelSpeed > addSpeed)
            {
                accelSpeed = addSpeed;
            }

            //Debug.Log($"Accelerate returned: {prevFrameVel + (nextFrameAccel * nextFrameWishDir)}");

            return accelSpeed * wishDir;
        }
        private Vector3 ApplyFriction(Vector3 prevFrameVel)
        {
            prevFrameVel = prevFrameVel.KillY();
            float speed = prevFrameVel.magnitude;

            if (speed < 0.0001f)
            {
                return Vector3.zero;
            }

            float drop = 0f;

            float control = speed < _config.StopSpeed ? _config.StopSpeed : speed;
            drop += control * _config.GroundFriction * Time.deltaTime;

            var newspeed = speed - drop < 0 ? 0 : speed - drop;

            if (newspeed != speed)
            {
                newspeed /= speed;
            }

            return prevFrameVel * newspeed;
        }
        public void SlideMovement()
        {
            _entity.SlideDirection += new Vector3(_entity.GroundNormal.x, 0f, _entity.GroundNormal.z) * _entity.CurrentSlideSpeed * Time.deltaTime;
            _entity.SlideDirection = _entity.SlideDirection.normalized;

            var slideForward = Vector3.Cross(_entity.GroundNormal, Quaternion.AngleAxis(-90, Vector3.up) * _entity.SlideDirection);

            _entity.CurrentSlideSpeed -= _config.SlideFriction * Time.deltaTime;
            _entity.CurrentSlideSpeed = Mathf.Clamp(_entity.CurrentSlideSpeed, 0f, _config.MaxSlideSpeed);
            _entity.CurrentSlideSpeed -= (slideForward * _entity.CurrentSlideSpeed).y * Time.deltaTime * _config.SlideSpeedMod;

            _entity.Velocity = slideForward * _entity.CurrentSlideSpeed;
        }
        public bool StepOffset(CapsuleCollider entCollider, Vector3 position, Vector3 velocity, Vector3 forwardVelocity)
        {
            //dont bother if there's no offset
            if (_config.StepOffset < 0f) return false;

            //or if they're not moving horizontally
            var forwardDirection = forwardVelocity.normalized;
            if (forwardDirection.sqrMagnitude == 0f) return false;

            var center = entCollider.center;
            var radius = entCollider.radius;
            var height = entCollider.height;
            var distToPoint = height / 2f - radius;
            var point1 = center + Vector3.up * distToPoint;
            var point2 = center + Vector3.down * distToPoint;
            var contactOffset = entCollider.contactOffset;

            //ground check
            var groundCast = CastCapsule(point1, point2, radius, position, position + Vector3.down * 0.1f,
                                         contactOffset, _groundLayerMask);
            if (groundCast.collider == null || Vector3.Angle(Vector3.up, groundCast.normal) > _config.SlopeAngleLimit)
            {
                return false;
            }

            //wall check (lower collider scale to not hit floor/ceiling)
            var wallCast = CastCapsule(point1, point2, radius, position, position + velocity,
                                       contactOffset, _groundLayerMask, 0.9f);
            if (wallCast.collider == null || Vector3.Angle(Vector3.up, wallCast.normal) <= _config.SlopeAngleLimit)
            {
                return false;
            }

            //ceiling check
            var upDistance = _config.StepOffset;
            var ceilingCast = CastCapsule(point1, point2, radius, position, position + Vector3.up * _config.StepOffset,
                                          contactOffset, _groundLayerMask);
            if (ceilingCast.collider != null)
            {
                upDistance = ceilingCast.distance;
            }

            //updistance <= 0 means the ceiling is directly above us, so dont bother stepping
            if (upDistance <= 0) return false;

            var nextStepPos = position + Vector3.up * upDistance;

            //forward movement check at next step
            float forwardMagnitude = _config.StepOffset;
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

            float verticalDistance = Mathf.Clamp(upDistance - downDistance, 0f, _config.StepOffset);
            float stepAngle = Vector3.Angle(Vector3.forward, new Vector3(0f, verticalDistance, forwardDistance));
            if (stepAngle > _config.SlopeAngleLimit)
            {
                return false;
            }

            nextStepPos = position + Vector3.up * verticalDistance;

            if (position != nextStepPos && forwardDistance > 0f)
            {
                //TODO: not like this lol
                entCollider.transform.position = nextStepPos + forwardDirection * forwardDistance * Time.deltaTime;
                return true;
            }
            else
            {
                return false;
            }
        }
        public TryMoveResult TryMove(CapsuleCollider collider, float deltaTime)
        {
            var origin = collider.transform.position;
            var velocity = _entity.Velocity;
            float d;
            var originalVel = velocity;
            var primalVel = velocity; //i love this variable name
            var newVel = new Vector3();
            var timeLeft = deltaTime;
            var allFraction = 0f;
            var numPlanes = 0;
            TryMoveResult blocked = TryMoveResult.Unblocked;

            for (int bumpcount = 0; bumpcount < _config.NumBumps; bumpcount++)
            {

                //end if stopped
                if (_entity.Velocity.magnitude == 0f)
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

                //if the normal is 1 its a floor. if its between 1 and 0.7 its a walkable slope
                if (castInfo.normal.y > _config.SlopeYNormalLimit)
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
                if (numPlanes >= _config.MaxClipPlanes)
                {
                    velocity = Vector3.zero;
                    break;
                }

                _entity.ClipPlanes[numPlanes] = castInfo.normal;
                numPlanes++;

                if (numPlanes == 1)
                {
                    if (_entity.ClipPlanes[0].y > _config.SlopeYNormalLimit)
                    {
                        _entity.Velocity = velocity;
                        return blocked;
                    }
                    else
                    {
                        ClipVelocity(originalVel, _entity.ClipPlanes[0], ref newVel, 1 + _config.BounceMod * (1 - _entity.SurfaceFriction));
                    }

                    velocity = newVel;
                    originalVel = newVel;
                }
                else
                {
                    for (int i = 0; i < numPlanes; i++)
                    {
                        newVel = _entity.Velocity;
                        ClipVelocity(originalVel, _entity.ClipPlanes[0], ref newVel, 1);

                        for (int j = 0; j < numPlanes; j++)
                        {
                            if (j != 1)
                            {
                                if (Vector3.Dot(_entity.Velocity, _entity.ClipPlanes[j]) < 0)
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
                                _entity.Velocity = origin;
                                break;
                            }
                            var dir = Vector3.Cross(_entity.ClipPlanes[0], _entity.ClipPlanes[1]).normalized;
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

            _entity.Velocity = velocity;
            return blocked;
        }

    }
}
