using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ZombieRagdollState : ZombieState
{
    private bool _hasHitGround;
    private Coroutine _monitorGroundContact;

    public ZombieRagdollState(ZombieController zombieController) : base(zombieController) { }

    public override void Enter()
    {
        _zombieController.Agent.enabled = false;
        _zombieController.Animator.enabled = false;
        _zombieController.Collider.enabled = true;
        _zombieController.Rigidbody.isKinematic = false;
        _hasHitGround = false;
        
        _monitorGroundContact = _zombieController.StartCoroutine(MonitorGroundContact(_zombieController.RagdollRecoveryTime));
    }

    public override void Exit()
    {
        if (_monitorGroundContact != null)
            _zombieController.StopCoroutine(_monitorGroundContact);
    }

    public override void HandleCollision(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & _zombieController.GroundLayerMask) != 0)
        {
            _hasHitGround = true;
        }
    }

    private IEnumerator MonitorGroundContact(float maxDuration)
    {
        float startTime = Time.time;

        while (Time.time - startTime < maxDuration && !_hasHitGround)
        {
            yield return null;
        }

        if (_zombieController.Health.IsDead)
        {
            PlayDeathAnimation();
        }
        else
        {
            RecoverFromRagdoll();
        }
    }

    private void PlayDeathAnimation()
    {
        _zombieController.Rigidbody.isKinematic = true;
        _zombieController.TargetCollider.enabled = false;
        
        _zombieController.Animator.enabled = true;
        _zombieController.Animator.SetTrigger(AnimatorParameters.ZombieDie);
        
        _zombieController.ChangeState(ZombieStateType.Dead);
    }

    private void RecoverFromRagdoll()
    {
        _zombieController.Rigidbody.isKinematic = true;
        _zombieController.Animator.enabled = true;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(_zombieController.transform.position, out hit, 2.0f, NavMesh.AllAreas))
        {
            _zombieController.Agent.enabled = true;
            _zombieController.Agent.Warp(hit.position);
            
            if (_zombieController.Target != null && !_zombieController.IsTargetDead())
            {
                _zombieController.ChangeState(ZombieStateType.Chasing);
            }
            else
            {
                _zombieController.ChangeState(ZombieStateType.Idle);
            }
        }
        else
        {
            _zombieController.ChangeState(ZombieStateType.Idle);
        }
    }
}