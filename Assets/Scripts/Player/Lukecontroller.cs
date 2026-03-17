using UnityEngine;

// ============================================================
//  LukeController.cs
//  Attach this to your Luke player GameObject
//  Requires: Rigidbody2D, Collider2D, Animator
// ============================================================

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class LukeController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 14f;
    public float dashForce = 18f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 1f;

    [Header("Ground Check")]
    public Transform groundCheck;          // Empty child GameObject at feet
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    [Header("Combat")]
    public int maxHealth = 100;
    public float attackCooldown = 0.4f;

    [Header("Visual")]
    public GameObject lightsaberGlow;      // Optional: a glow sprite on the saber

    // ── private state ─────────────────────────────────────────
    private Rigidbody2D _rb;
    private Animator _anim;
    private bool _isGrounded;
    private bool _facingRight = true;
    private float _moveInput;

    private bool _isDashing;
    private float _dashTimer;
    private float _dashCooldownTimer;

    private int _currentHealth;
    private float _attackTimer;
    private bool _isAttacking;

    // animator parameter hashes (faster than string lookup)
    private static readonly int HashSpeed = Animator.StringToHash("Speed");
    private static readonly int HashIsGround = Animator.StringToHash("IsGrounded");
    private static readonly int HashJump = Animator.StringToHash("Jump");
    private static readonly int HashAttack = Animator.StringToHash("Attack");
    private static readonly int HashDash = Animator.StringToHash("Dash");
    private static readonly int HashHurt = Animator.StringToHash("Hurt");
    private static readonly int HashDead = Animator.StringToHash("Dead");

    // ── Unity lifecycle ───────────────────────────────────────
    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();
        _currentHealth = maxHealth;
    }

    void Update()
    {
        if (_isDashing) return;   // skip input during dash

        ReadInput();
        HandleJump();
        HandleDash();
        HandleAttack();
        UpdateAnimator();
    }

    void FixedUpdate()
    {
        CheckGround();

        if (_isDashing)
        {
            _dashTimer -= Time.fixedDeltaTime;
            if (_dashTimer <= 0f) EndDash();
            return;
        }

        Move();
    }

    // ── Input ─────────────────────────────────────────────────
    void ReadInput()
    {
        _moveInput = Input.GetAxisRaw("Horizontal");
    }

    // ── Movement ──────────────────────────────────────────────
    void Move()
    {
        _rb.linearVelocity = new Vector2(_moveInput * moveSpeed, _rb.linearVelocity.y);

        // Flip sprite
        if (_moveInput > 0 && !_facingRight) Flip();
        if (_moveInput < 0 && _facingRight) Flip();
    }

    void Flip()
    {
        _facingRight = !_facingRight;
        transform.localScale = new Vector3(
            -transform.localScale.x,
             transform.localScale.y,
             transform.localScale.z);
    }

    // ── Jump ──────────────────────────────────────────────────
    void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && _isGrounded)
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
            _anim.SetTrigger(HashJump);
        }
    }

    // ── Dash ──────────────────────────────────────────────────
    void HandleDash()
    {
        _dashCooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.LeftShift) && _dashCooldownTimer <= 0f)
        {
            _isDashing = true;
            _dashTimer = dashDuration;
            _dashCooldownTimer = dashCooldown;

            float dir = _facingRight ? 1f : -1f;
            _rb.linearVelocity = new Vector2(dir * dashForce, _rb.linearVelocity.y);
            _anim.SetTrigger(HashDash);

            // Optional: invincibility frames here
        }
    }

    void EndDash()
    {
        _isDashing = false;
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
    }

    // ── Attack ────────────────────────────────────────────────
    void HandleAttack()
    {
        _attackTimer -= Time.deltaTime;

        // Left Mouse / Z = Lightsaber swing
        if (Input.GetButtonDown("Fire1") && _attackTimer <= 0f)
        {
            _attackTimer = attackCooldown;
            _isAttacking = true;
            _anim.SetTrigger(HashAttack);
            // Hitbox is handled in the Attack animation via LukeAttackHitbox.cs
        }

        // Right Mouse / X = Force push (placeholder)
        if (Input.GetButtonDown("Fire2"))
        {
            ForcePush();
        }
    }

    void ForcePush()
    {
        // TODO: Instantiate force wave prefab in front of Luke
        Debug.Log("Force Push!");
    }

    // ── Ground Check ──────────────────────────────────────────
    void CheckGround()
    {
        _isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer);
    }

    // ── Animator ──────────────────────────────────────────────
    void UpdateAnimator()
    {
        _anim.SetFloat(HashSpeed, Mathf.Abs(_moveInput));
        _anim.SetBool(HashIsGround, _isGrounded);
    }

    // ── Health / Damage ───────────────────────────────────────
    public void TakeDamage(int amount)
    {
        if (_isDashing) return;   // dash = invincible frames

        _currentHealth -= amount;
        _anim.SetTrigger(HashHurt);

        // Knockback
        float knockDir = _facingRight ? -1f : 1f;
        _rb.AddForce(new Vector2(knockDir * 5f, 3f), ForceMode2D.Impulse);

        if (_currentHealth <= 0) Die();
    }

    void Die()
    {
        _currentHealth = 0;
        _anim.SetTrigger(HashDead);
        _rb.linearVelocity = Vector2.zero;
        enabled = false;   // disable input
        // TODO: trigger GameManager.OnPlayerDied()
    }

    public void Heal(int amount)
    {
        _currentHealth = Mathf.Min(_currentHealth + amount, maxHealth);
    }

    public int GetHealth() => _currentHealth;
    public int GetMaxHealth() => maxHealth;

    // ── Gizmos (editor only) ──────────────────────────────────
    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}