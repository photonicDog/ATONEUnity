using Assets.Scripts.Gameplay.Components;
using Assets.Scripts.Gameplay.Models;
using Assets.Scripts.Gameplay.Models.Configurations;
using Assets.Scripts.Gameplay.Models.Enums;
using System.Collections;
using System.Net.WebSockets;
using UnityEditor.U2D;
using UnityEngine;

namespace Assets.Scripts.Gameplay.Controllers
{
    public class TetherController : MonoBehaviour
    {
        public PlayerController Player;
        private TetherConfig _config;
        private TetherModel _tether;
        private PhysMoveComponent _physics;

        private void Start()
        {
            _config = new TetherConfig();
            var tetherHook = new GameObject("TetherHook").transform;
            tetherHook.parent = transform;
            tetherHook.transform.localPosition = Vector3.zero;
            var lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 3;
            lineRenderer.startWidth = 1;
            lineRenderer.endWidth = 1;
            _tether = new TetherModel(tetherHook, lineRenderer);

            _tether.State = TetherStatus.Ready;
        }

        private void Update()
        {
            switch (_tether.State)
            {
                case TetherStatus.Ready:
                    _tether.NextHookPosition = transform.position;
                    break;

                case TetherStatus.Firing:
                    break;

                case TetherStatus.Tethered:
                    //TODO: move hook position with attached object
                    CheckBreak(transform.position, _tether.TetherHook.position, _tether.TetheredCollider);
                    break;

                case TetherStatus.AwaitingRecline:
                    _tether.SetState(TetherStatus.Reclining);
                    StartCoroutine(Reclining(_config.RetractTime));
                    break;

                case TetherStatus.Reclining:
                    break;

                case TetherStatus.Cooldown:
                        if (_tether.Cooldown <= 0f)
                        {
                            _tether.Cooldown = 0f;
                            _tether.SetState(TetherStatus.Ready);
                        }
                        else
                        {
                            if (!(_config.CooldownOnlyOnGround && !Player.Data.OnGround))
                            {
                                _tether.Cooldown -= Time.deltaTime;
                            }
                        }
                    _tether.NextHookPosition = transform.position;
                    break;
            }

            AdjustVelocityToTether();

            var dir = (_tether.NextHookPosition - transform.position).normalized;
            _tether.TetherRenderer.SetPosition(0, _tether.NextHookPosition - dir * (_tether.TetherRadius + _config.TetherLeashMaxRange));
            _tether.TetherRenderer.SetPosition(1, _tether.NextHookPosition - dir * _tether.TetherRadius);
            _tether.TetherRenderer.SetPosition(2, _tether.NextHookPosition);

            _tether.TetherRenderer.enabled = !(_tether.CheckState(TetherStatus.Ready) || _tether.CheckState(TetherStatus.Cooldown));

            _tether.TetherHook.position = _tether.NextHookPosition;

            //Debug.DrawLine(transform.position, _tetherHook.position, Color.blue);
            //Debug.DrawLine(transform.position, _hookPosition, Color.red);
        }

        public void AddPhysMoveComponent(PhysMoveComponent physics)
        {
            _physics = physics;
        }

        public void AdjustVelocityToTether()
        {
            if (!_tether.CheckState(TetherStatus.Tethered))
            {
                return;
            }

            var tetherHookPos = _tether.TetherHook.position;
            var currentPos = transform.position;
            var nextFramePos = _physics.GetPosition();
            var distFromHook = (nextFramePos - tetherHookPos).magnitude;

            if (distFromHook < _tether.TetherRadius)
            {
                return;
            } else if (distFromHook > _tether.TetherRadius + _config.TetherLeashMaxRange)
            {
                _tether.TetheredCollider = null;
                _tether.SetState(TetherStatus.AwaitingRecline);
            }

            //TODO: adjust leash force so that it work
            //IDEA: what if we work out the vector opposite from the hook, separately to its right and up counterparts?
            //      those forces should cancel out, but have a max force it can apply (effectively it can only cancel so much)
            //      the other two shouldn't be affected by the tether at all other than the velocity they add right?
            //      work this out later
            //define pull/push force
            var positionToHook = tetherHookPos - nextFramePos;
            var leashForce = positionToHook - (positionToHook.normalized * _tether.TetherRadius);
            nextFramePos += leashForce;
            var currentToHook = currentPos - tetherHookPos;
            var nextFrameToHook = nextFramePos - tetherHookPos;
            //TODO: this v returns zero all the time. find out why
            var anglVel = Vector3.Angle(currentToHook, nextFrameToHook);
            var centiVel = -1 * Mathf.Pow(anglVel / Time.deltaTime, 2) * (nextFrameToHook) * Time.deltaTime;
            Debug.DrawLine(transform.position, nextFramePos, Color.red);
            Debug.DrawLine(nextFramePos, nextFramePos + leashForce, Color.blue);
            /*
                        if (Mathf.Abs(leashDeviance / _config.TetherLeashRange) >= 1)
                        {
                            _tether.TetheredCollider = null;
                            _tether.SetState(TetherStatus.AwaitingRecline);
                        }
                        else
                        {
                            _physics.SetVelocity(newVel);
                        }
            */
            _physics.SetBaseVelocity(centiVel * (distFromHook - _tether.TetherRadius)/_config.TetherLeashMaxRange);
        }

        public void Fire(Vector3 origin, Vector3 aimDirection)
        {
            _tether.HookTarget = origin + aimDirection * _config.MaxHookDistance;
            //if currently tethered, recline
            if (_tether.CheckState(TetherStatus.Tethered))
            {
                _tether.SetState(TetherStatus.Reclining);
                StartCoroutine(Reclining(_config.RetractTime));
                return;
            }

            //if not ready to tether, don't
            if (!_tether.CheckState(TetherStatus.Ready))
            {
                return;
            }

            //else, fire
            _tether.SetState(TetherStatus.Firing);

            //limit distance to max distance
            Vector3.ClampMagnitude(_tether.HookTarget, _config.MaxHookDistance);
            StartCoroutine(Firing(_config.FireSpeed, _config.FireTimeMax));
        }

        private bool CheckBreak(Vector3 tetherStart, Vector3 tetherEnd, Collider tetheredCollider = null)
        {
            var tether = tetherEnd - tetherStart;
            var hits = Physics.RaycastAll(tetherStart, tether.normalized, tether.magnitude, _config.BreakTetherLayerMasks);
            foreach (var hit in hits) {
                if (hit.collider != null)
                {
                    if (tetheredCollider != null)
                    {
                        if (ReferenceEquals(hit.collider, tetheredCollider)) continue;
                    }

                    if (_config.BreakTetherLayerMasks == (_config.BreakTetherLayerMasks | (1 << hit.collider.gameObject.layer)))
                    {
                        Break();
                        return true;
                    }
                }
            }
            return false;
        }

        private void Break()
        {
            _tether.SetState(TetherStatus.AwaitingRecline);
            _tether.Cooldown = _config.CooldownOnBreak;
        }

        private IEnumerator Firing(float fireSpeed, float fireTimeMax)
        {
            _tether.NextHookPosition = _tether.TetherHook.position;
            var startPos = _tether.NextHookPosition;
            var newPos = startPos;
            var lastPos = startPos;
            var direction = (_tether.HookTarget - startPos).normalized;
            var timeSpentFiring = 0f;
            RaycastHit hit;

            while (_tether.TetherHook.position != _tether.HookTarget && timeSpentFiring < fireTimeMax && (newPos - startPos).magnitude < _config.MaxHookDistance && !_tether.CheckState(TetherStatus.Tethered))
            {
                timeSpentFiring += Time.deltaTime;
                lastPos = newPos;
                newPos += direction * fireSpeed * Time.deltaTime;
                Physics.Raycast(lastPos, direction, out hit, (newPos - lastPos).magnitude, _config.TetherableLayerMasks);
                if (hit.collider != null)
                {
                    Debug.Log("Hit collider");
                    if (!hit.collider.CompareTag(_config.NoTetherTag))
                    {
                        //if we hit a tetherable collider, tether to it
                        _tether.TetheredCollider = hit.collider;
                        _tether.HookTarget = hit.point;
                        _tether.TetherHook.position = _tether.HookTarget;
                        _tether.TetherRadius = (_tether.HookTarget - transform.position).magnitude;
                        _tether.SetState(TetherStatus.Tethered);
                        yield break;
                    } else
                    {
                        //if we hit a collider that can't be tethered to, start recline
                        _tether.TetheredCollider = null;
                        _tether.SetState(TetherStatus.AwaitingRecline);
                        yield break;
                    }
                }

                if (CheckBreak(transform.position, newPos))
                {
                    //if the tether broke mid-fire, start recline
                    _tether.TetheredCollider = null;
                    _tether.SetState(TetherStatus.AwaitingRecline);
                    yield break;
                }

                _tether.NextHookPosition = newPos;
                yield return new WaitForEndOfFrame();
            }

            //assume we didn't hit anything by this point
            _tether.TetheredCollider = null;
            _tether.SetState(TetherStatus.AwaitingRecline);
        }

        private IEnumerator Reclining(float retractTime)
        {
            var startPos = _tether.TetherHook.position;
            var newPos = startPos;
            var time = 0f;

            retractTime *= Vector3.Distance(startPos, transform.position) / _config.MaxHookDistance;
            while (time < retractTime)
            {
                newPos = Vector3.Lerp(startPos, transform.position, time/retractTime);
                time += Time.deltaTime;
                _tether.NextHookPosition = newPos;
                yield return new WaitForEndOfFrame();
            }

            _tether.NextHookPosition = transform.position;
            _tether.SetState(TetherStatus.Cooldown);
        }
    }
}
