using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DiceDrive.Characters
{
    /// <summary>
    /// The actions a board character can perform: walk a path, jump, attack, take a hit, die.
    /// Not wired to any input: gameplay code (dice roll → walk, attack card → attack, enemy turn → receive hit)
    /// calls these methods. Works in world positions, so it doesn't depend on the grid; the caller converts
    /// grid coordinates (e.g. Tilemap.GetCellCenterWorld).
    ///
    /// Two ways to call each action:
    ///  - Fire-and-forget, e.g. <c>MoveAlong(path, onComplete)</c>: queued, so calls made while the character is
    ///    busy run one after another.
    ///  - Coroutine, e.g. <c>yield return actor.MoveAlongRoutine(path)</c>: for sequencing from a presenter.
    /// </summary>
    public sealed class CharacterActor : MonoBehaviour
    {
        [SerializeField] private CharacterAnimator visual;

        [Header("Movement")]
        [Tooltip("Walking speed in world units per second.")]
        [SerializeField, Min(0.1f)] private float moveSpeed = 3f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.2f;
        [SerializeField, Min(0.1f)] private float jumpDuration = 0.55f;

        [Header("Combat")]
        [Tooltip("How far the character steps toward the target while swinging.")]
        [SerializeField, Min(0f)] private float attackLunge = 0.2f;
        [Tooltip("Alternate Attack1/Attack2 on consecutive attacks.")]
        [SerializeField] private bool alternateAttacks = true;
        [Tooltip("How far the character is pushed back when hit.")]
        [SerializeField, Min(0f)] private float hitKnockback = 0.15f;

        private readonly Queue<IEnumerator> _queue = new Queue<IEnumerator>();
        private bool _running;
        private int _attackCount;
        private bool _impactReceived;

        /// <summary>True while an action is playing or queued.</summary>
        public bool IsBusy => _running || _queue.Count > 0;

        public bool IsDead { get; private set; }

        public CharacterAnimator Visual => visual;

        /// <summary>Any action started / all queued actions finished.</summary>
        public event Action<CharacterActor> ActionStarted;
        public event Action<CharacterActor> BecameIdle;

        /// <summary>Reached waypoint <c>index</c> of a MoveAlong path (footsteps, tile effects...).</summary>
        public event Action<CharacterActor, int> StepReached;

        /// <summary>The moment this character's attack connects (damage numbers, hit sounds, camera shake...).</summary>
        public event Action<CharacterActor> AttackLanded;

        public event Action<CharacterActor> HitReceived;
        public event Action<CharacterActor> Died;

        private void Awake()
        {
            if (visual == null)
            {
                visual = GetComponentInChildren<CharacterAnimator>();
            }

            visual.AttackImpact += () => _impactReceived = true;

            // Characters always render on the top-most sorting layer, above every environment layer.
            if (!TryGetComponent<CharacterSorting>(out _))
            {
                gameObject.AddComponent<CharacterSorting>();
            }
        }

        private void OnEnable()
        {
            if (!IsDead)
            {
                visual.Play(CharacterAnim.Idle);
            }
        }

        // ------------------------------------------------------------------ queued API

        public void MoveAlong(IReadOnlyList<Vector3> path, Action onComplete = null) => Enqueue(MoveAlongRoutine(path), onComplete);

        public void JumpTo(Vector3 target, Action onComplete = null) => Enqueue(JumpToRoutine(target), onComplete);

        public void Attack(Vector3 targetPosition, Action onImpact = null, Action onComplete = null) =>
            Enqueue(AttackRoutine(targetPosition, onImpact), onComplete);

        /// <summary>Full exchange: this character attacks, and on impact the target flinches (or dies if lethal).</summary>
        public void AttackTarget(CharacterActor target, bool lethal = false, Action onComplete = null) =>
            Enqueue(AttackTargetRoutine(target, lethal), onComplete);

        public void ReceiveHit(Vector3 attackerPosition, Action onComplete = null) => Enqueue(ReceiveHitRoutine(attackerPosition), onComplete);

        public void Die(Action onComplete = null) => Enqueue(DieRoutine(), onComplete);

        // ------------------------------------------------------------------ coroutine API

        public IEnumerator MoveAlongRoutine(IReadOnlyList<Vector3> path)
        {
            if (IsDead || path == null || path.Count == 0)
            {
                yield break;
            }

            visual.Play(CharacterAnim.Run);
            for (var i = 0; i < path.Count; i++)
            {
                var target = path[i];
                visual.Face(target.x - transform.position.x);
                while ((transform.position - target).sqrMagnitude > 0.0001f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
                    yield return null;
                }

                StepReached?.Invoke(this, i);
            }

            visual.Play(CharacterAnim.Idle);
        }

        public IEnumerator JumpToRoutine(Vector3 target)
        {
            if (IsDead)
            {
                yield break;
            }

            var start = transform.position;
            visual.Face(target.x - start.x);
            visual.Play(CharacterAnim.Jump);

            var falling = false;
            for (var t = 0f; t < 1f; t += Time.deltaTime / jumpDuration)
            {
                if (!falling && t >= 0.5f)
                {
                    falling = true;
                    visual.Play(CharacterAnim.Fall);
                }

                var arc = 4f * jumpHeight * t * (1f - t); // parabola peaking at jumpHeight
                transform.position = Vector3.Lerp(start, target, t) + Vector3.up * arc;
                yield return null;
            }

            transform.position = target;
            visual.Play(CharacterAnim.Idle);
        }

        /// <param name="onImpact">Called on the frame the blow connects.</param>
        public IEnumerator AttackRoutine(Vector3 targetPosition, Action onImpact = null)
        {
            if (IsDead)
            {
                yield break;
            }

            var origin = transform.position;
            var dir = Mathf.Sign(targetPosition.x - origin.x);
            visual.Face(dir);

            var anim = alternateAttacks && _attackCount % 2 == 1 ? CharacterAnim.Attack2 : CharacterAnim.Attack1;
            _attackCount++;

            _impactReceived = false;
            visual.Play(anim);
            var length = Mathf.Max(0.05f, visual.GetLength(anim));
            var impacted = false;

            for (var t = 0f; t < length; t += Time.deltaTime)
            {
                // Small lunge toward the target and back over the swing.
                var lunge = Mathf.Sin(Mathf.Clamp01(t / length) * Mathf.PI) * attackLunge;
                transform.position = origin + new Vector3(dir * lunge, 0f, 0f);

                if (!impacted && _impactReceived)
                {
                    impacted = true;
                    Impact(onImpact);
                }

                yield return null;
            }

            if (!impacted)
            {
                Impact(onImpact); // clip without the animation event: land at the end
            }

            transform.position = origin;
            visual.Play(CharacterAnim.Idle);
        }

        public IEnumerator AttackTargetRoutine(CharacterActor target, bool lethal)
        {
            if (target == null)
            {
                yield break;
            }

            var reacting = false;
            yield return AttackRoutine(target.transform.position, () =>
            {
                reacting = true;
                var reaction = lethal ? target.DieRoutine() : target.ReceiveHitRoutine(transform.position);
                target.StartCoroutine(Then(reaction, () => reacting = false));
            });

            // The exchange is done once the defender has finished reacting too.
            while (reacting && target != null && target.isActiveAndEnabled)
            {
                yield return null;
            }
        }

        private static IEnumerator Then(IEnumerator routine, Action after)
        {
            yield return routine;
            after();
        }

        public IEnumerator ReceiveHitRoutine(Vector3 attackerPosition)
        {
            if (IsDead)
            {
                yield break;
            }

            var origin = transform.position;
            var away = Mathf.Sign(origin.x - attackerPosition.x);
            visual.Face(-away); // look at the attacker
            visual.Play(CharacterAnim.Hit);
            HitReceived?.Invoke(this);

            var length = Mathf.Max(0.05f, visual.GetLength(CharacterAnim.Hit));
            for (var t = 0f; t < length; t += Time.deltaTime)
            {
                var push = Mathf.Sin(Mathf.Clamp01(t / length) * Mathf.PI) * hitKnockback;
                transform.position = origin + new Vector3(away * push, 0f, 0f);
                yield return null;
            }

            transform.position = origin;
            visual.Play(CharacterAnim.Idle);
        }

        public IEnumerator DieRoutine()
        {
            if (IsDead)
            {
                yield break;
            }

            IsDead = true;
            _queue.Clear();
            visual.Play(CharacterAnim.Death);
            Died?.Invoke(this);
            yield return new WaitForSeconds(visual.GetLength(CharacterAnim.Death));
        }

        // ------------------------------------------------------------------ immediate

        /// <summary>Instantly places the character (level start, respawn).</summary>
        public void SnapTo(Vector3 position)
        {
            transform.position = position;
        }

        public void Face(Vector3 worldPoint)
        {
            visual.Face(worldPoint.x - transform.position.x);
        }

        /// <summary>Clears death/queue and returns to Idle (level retry).</summary>
        public void Revive()
        {
            StopAllCoroutines();
            _queue.Clear();
            _running = false;
            IsDead = false;
            visual.Play(CharacterAnim.Idle);
        }

        // ------------------------------------------------------------------ queue

        private void Impact(Action onImpact)
        {
            AttackLanded?.Invoke(this);
            onImpact?.Invoke();
        }

        private void Enqueue(IEnumerator routine, Action onComplete)
        {
            _queue.Enqueue(WithCallback(routine, onComplete));
            if (!_running)
            {
                StartCoroutine(RunQueue());
            }
        }

        private static IEnumerator WithCallback(IEnumerator routine, Action onComplete)
        {
            yield return routine;
            onComplete?.Invoke();
        }

        private IEnumerator RunQueue()
        {
            _running = true;
            while (_queue.Count > 0)
            {
                ActionStarted?.Invoke(this);
                yield return _queue.Dequeue();
            }

            _running = false;
            BecameIdle?.Invoke(this);
        }

        // ------------------------------------------------------------------ editor testing (no input bindings)

        [ContextMenu("Test/Walk 3 Right")]
        private void TestWalkRight() => MoveAlong(Line(Vector3.right, 3));

        [ContextMenu("Test/Walk 3 Left")]
        private void TestWalkLeft() => MoveAlong(Line(Vector3.left, 3));

        [ContextMenu("Test/Jump 2 Right")]
        private void TestJump() => JumpTo(transform.position + Vector3.right * 2f);

        [ContextMenu("Test/Attack Right")]
        private void TestAttack() => Attack(transform.position + Vector3.right);

        [ContextMenu("Test/Receive Hit From Right")]
        private void TestHit() => ReceiveHit(transform.position + Vector3.right);

        [ContextMenu("Test/Die")]
        private void TestDie() => Die();

        [ContextMenu("Test/Revive")]
        private void TestRevive() => Revive();

        private List<Vector3> Line(Vector3 dir, int steps)
        {
            var points = new List<Vector3>();
            for (var i = 1; i <= steps; i++)
            {
                points.Add(transform.position + dir * i);
            }

            return points;
        }
    }
}
