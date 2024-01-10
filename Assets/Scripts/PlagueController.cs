using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlagueController : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private float jumpHeight;
    [SerializeField] private float doubleJumpHeight;
    [SerializeField] private float doubleJumpAnimationDuration;
    [SerializeField] private float throwForce;
    [SerializeField] private float chargeTime;
    [SerializeField] private float burstHeight;
    [SerializeField] private float horizontalBurstMomentum;
    [SerializeField] private float burstBufferPeriod;
    [SerializeField] private float verticalBurstDuration;
    [SerializeField] private int maxProjectiles;
    [SerializeField] private float maxSubweaponAmmo;
    [SerializeField] private float subweaponRefillRate;
    [SerializeField] private float spawnedPlatformSubweaponCost;
    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashDuration;
    [SerializeField] private float pogoHeight;
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private GameObject platformPrefab;
    [SerializeField] private bool bombThrowOn;
    [SerializeField] private bool doubleJumpOn;
    [SerializeField] private bool burstOn;
    [SerializeField] private bool gravityFlipOn;
    [SerializeField] private bool platformSpawningOn;
    [SerializeField] private bool pogoOn;


    private Vector2 _movementVector;
    private Rigidbody2D _rb;
    private bool _isGrounded;
    private bool _facingRight;
    private bool _upsideDown;
    private Animator _anim;
    private PlayerInput _playerInput;
    private Transform _bombPos;
    private Transform _platformPos;
    private List<GameObject> _thrownProjectiles;
    private float _holdTime;
    private float _burstBufferTime;
    private float _dashTime;
    private bool _attackButtonHeld = false;
    private bool _canBurst = false;
    private bool _canDoubleJump = false;
    private bool _inBurstBuffer = false;
    private Vector3 vel;
    private float _gravityScale;
    private float _currentSubweaponAmmo;
    private bool _subweaponsDisabled = false;
    private GameObject _spawnedPlatform;
    private bool _dashing = false;
    private bool _inPogo = false;
    private PogoHitboxController _pogoHitboxController;
    private CircleCollider2D _pogoHitbox;
    private bool _burstingWithMomentum = false;
    private bool _inDoubleJump = false;
    private float _doubleJumpTime;
    private bool _verticalBursting = false;
    private float _verticalBurstTime = 0;
    private float _horziontalBurstMomentum;


    //START UNITY EVENT FUNCTIONS//
    private void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _gravityScale = _rb.gravityScale;
        _isGrounded = true;
        _facingRight = true;
        _upsideDown = false;
        _anim = GetComponent<Animator>();
        _playerInput = new PlayerInput();
        _playerInput.Enable();
        _bombPos = transform.Find("BombPos").transform;
        Transform pogo = transform.Find("PogoHitbox");
        _platformPos = pogo;
        _pogoHitboxController = pogo.GetComponent<PogoHitboxController>();
        _pogoHitbox = pogo.GetComponent<CircleCollider2D>();
        _thrownProjectiles = new List<GameObject>();
        _currentSubweaponAmmo = maxSubweaponAmmo;
        _horziontalBurstMomentum = horizontalBurstMomentum;
    }

    private void Update()
    {
        ReadMovement();
        HoldTimer();
        BurstBufferTimer();
        SubweaponRefillTimer();
        DashTimer();
        PogoCheck();
        DoubleJumpAnimTimer();
        VerticalBurstTimer();

        vel = _rb.velocity;
        for (int i = _thrownProjectiles.Count-1; i >= 0; i--)
        {
            GameObject projectile = _thrownProjectiles[i];
            if (projectile == null)
            {
                _thrownProjectiles.Remove(projectile);
            }
        }
    }
    
    private void FixedUpdate()
    {
        if ((!_facingRight && _movementVector.x > 0 || _facingRight && _movementVector.x < 0) && !_burstingWithMomentum)
        {
            HorizontalFlip();
        }
        
        if (_burstingWithMomentum)
        {
            if (vel.x < 0 && _facingRight)
            {
                HorizontalFlip();
            }
            else if (vel.x > 0 && !_facingRight)
            {
                HorizontalFlip();
            }
        }

        if (_movementVector.x < 0)
        {
            _horziontalBurstMomentum = horizontalBurstMomentum * -1;
        }
        else if (_movementVector.x > 0)
        {
            _horziontalBurstMomentum = horizontalBurstMomentum;
        }

        if(!_dashing && !_burstingWithMomentum && !_verticalBursting)
            _rb.velocity = new Vector2(_movementVector.x * speed * Time.deltaTime, _rb.velocity.y);

        if (_rb.velocity.y < 0 && !_inPogo && !_inDoubleJump)
        {
            _anim.CrossFade("Fall", 0, 0);
        }
    }
    
    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.collider.CompareTag("Ground"))
        {
            _anim.CrossFade("Stand", 0, 0);
            _isGrounded = true;
            _canDoubleJump = false;
            _burstingWithMomentum = false;
            _verticalBursting = false;
            _inDoubleJump = false;
        }
    }
    //END UNITY EVENT FUNCTIONS//
    
    //START INPUT FUNCTIONS//
    private void ReadMovement()
    {
        _movementVector = _playerInput.Player.Move.ReadValue<Vector2>();
        
        if (_isGrounded && !_dashing)
        {
            if (_movementVector.x > 0 || _movementVector.x < 0) 
            {
                _anim.CrossFade("Walk", 0, 0);
            }
            else if(_movementVector.x == 0)
            {
                _anim.CrossFade("Stand", 0, 0);
            }
        }
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        if (_isGrounded)
        {
            _isGrounded = false;
            float jumpForce = CalculateJumpForce(jumpHeight);
            AddJumpForce(jumpForce);
            _canDoubleJump = true;
            if (_dashing)
            {
                _anim.CrossFade("Burst", 0, 0);
            }
            else
            {
                _anim.CrossFade("Jump", 0, 0);
            }
            return;
        }
        if (_canDoubleJump && !_isGrounded && doubleJumpOn)
        {
            float jumpForce = CalculateJumpForce(doubleJumpHeight);
            AddJumpForce(jumpForce);
            _canDoubleJump = false;
            _inDoubleJump = true;
            _anim.CrossFade("Double Jump", 0, 0);
            if (_burstingWithMomentum || _dashing)
            {
                if ((_rb.velocity.x > 0 && _movementVector.x <= 0) || (_rb.velocity.x < 0 && _movementVector.x >= 0))
                {
                    Debug.Log("Attempted double jump burst momentum cancel cancel");
                    _burstingWithMomentum = false;
                    _dashing = false;
                }
            }
        }
    }

    private void DoubleJumpAnimTimer()
    {
        if (_inDoubleJump)
        {
            _doubleJumpTime += Time.deltaTime * 1f;
            if (_doubleJumpTime > doubleJumpAnimationDuration)
            {
                _inDoubleJump = false;
                _doubleJumpTime = 0;
            }
        }
        else
        {
            _doubleJumpTime = 0;
        }
    }

    public void PrimaryAttack(InputAction.CallbackContext context)
    {
        if (!bombThrowOn) return;
        if ((context.started || context.performed))
        {
            if (burstOn)
            {
                _attackButtonHeld = true;
            }
            if (!context.started || _thrownProjectiles.Count >= maxProjectiles) return;
            if (!_inBurstBuffer)
            {
                ThrowBomb();
            }
        }
        else if (context.canceled)
        {
            Burst();
            _canBurst = false;
            _holdTime = 0f;
            _attackButtonHeld = false;
        }
    }


    public void GravityFlip(InputAction.CallbackContext context)
    {
        if (!context.started || !gravityFlipOn || !_isGrounded) return;
        VerticalFlip();
        _rb.velocity = new Vector2(_rb.velocity.x, 0);
        _rb.gravityScale *= -1;
        _isGrounded = false;
    }

    public void SpawnPlatform(InputAction.CallbackContext context)
    {
        if (!context.started || _subweaponsDisabled || !platformSpawningOn || _isGrounded) return;
        if (_spawnedPlatform == null)
        {
            _inDoubleJump = false;
            _spawnedPlatform = Instantiate(platformPrefab, _platformPos.position, Quaternion.identity);
            _rb.velocity = new Vector2(_rb.velocity.x, 0);
            _currentSubweaponAmmo -= spawnedPlatformSubweaponCost;
            _inPogo = false;
        }
    }

    public void Dash(InputAction.CallbackContext context)
    {
        if (!context.started || !_isGrounded || _dashing) return;
        _anim.CrossFade("Dash", 0, 0);
        _dashing = true;
        _rb.velocity += new Vector2(dashSpeed, _rb.velocity.y);
        _inPogo = false;
        _inDoubleJump = false;
    }
    //END INPUT FUNCTIONS//
    
    //START HELPER METHODS//
    
    private void HorizontalFlip()
    {
        _facingRight = !_facingRight;
        dashSpeed *= -1;
        Vector2 scale = transform.localScale;
        transform.localScale = new Vector2(-scale.x, scale.y);
    }

    private void VerticalFlip()
    {
        _upsideDown = !_upsideDown;
        Vector2 scale = transform.localScale;
        transform.localScale = new Vector2(scale.x, -scale.y);
    }
    
    private float CalculateJumpForce(float height)
    {
        //gravity scale instance variable required to prevent square root of a negative number
        return Mathf.Sqrt(height * -2 * (Physics2D.gravity.y * _gravityScale));
    }
    
    public void ThrowBomb()
    {
        _anim.CrossFade("Throw", 0, 0);
        GameObject spawnedBomb = Instantiate(bombPrefab, _bombPos.position, Quaternion.identity);
        _inPogo = false;
        Rigidbody2D bombRigidBody = spawnedBomb.GetComponent<Rigidbody2D>();
        if (_facingRight)
        {
            bombRigidBody.AddForce(new Vector2(throwForce, 0), ForceMode2D.Impulse);
        }
        else
        {
            bombRigidBody.AddForce(new Vector2(-throwForce, 0), ForceMode2D.Impulse);
        }
        _thrownProjectiles.Add(spawnedBomb);
    }

    private void HoldTimer()
    {
        if (_attackButtonHeld)
        {
            _holdTime += 1f * Time.deltaTime;
            if (_holdTime >= chargeTime)
            {
                _canBurst = true;
                GetComponent<SpriteRenderer>().color = Color.blue;
            }
        }
        else
        {
            _holdTime = 0f;
            _canBurst = false;
            GetComponent<SpriteRenderer>().color = Color.white;
        }
    }

    private void BurstBufferTimer()
    {
        if (_inBurstBuffer)
        {
            _burstBufferTime += 1f * Time.deltaTime;
            if (_burstBufferTime >= burstBufferPeriod)
            {
                _inBurstBuffer = false;
            }
        }
        else
        {
            _burstBufferTime = 0;
        }
    }

    private void VerticalBurstTimer()
    {
        if (_verticalBursting)
        {
            _verticalBurstTime += 1f * Time.deltaTime;
            if (_verticalBurstTime >= verticalBurstDuration)
            {
                _verticalBursting = false;
            }
        }
        else
        {
            _verticalBurstTime = 0;
        }
    }

    private void DashTimer()
    {
        if (_dashing)
        {
            _dashTime += 1f * Time.deltaTime;
            if (_dashTime >= dashDuration && _isGrounded)
            {
                _dashing = false;
            }

            if ((_facingRight && _movementVector.x < 0) || (!_facingRight && _movementVector.x > 0))
            {
                if (_isGrounded)
                {
                    _dashing = false;
                }
                else
                {
                    float x = vel.x;
                    float y = vel.y;
                    _rb.velocity = new Vector2(x * -0.75f, y);
                }

            }
        }
        else
        {
            _dashTime = 0;

        }
    }

    private void SubweaponRefillTimer()
    {
        if (_currentSubweaponAmmo < 0)
        {
            _currentSubweaponAmmo = 0;
            _subweaponsDisabled = true;
        }

        if (_currentSubweaponAmmo >= maxSubweaponAmmo)
        {
            _currentSubweaponAmmo = maxSubweaponAmmo;
            _subweaponsDisabled = false;
        }
        else
        {
            _currentSubweaponAmmo += subweaponRefillRate * Time.deltaTime;
        }
    }
    
    private void Burst()
    {
        if (!_canBurst) return;

        if (_isGrounded)
        {
            _canDoubleJump = true;
        }

        if (_movementVector.x < 0 || _movementVector.x > 0)
        {
            float y = vel.y;
            _rb.velocity = new Vector2(_horziontalBurstMomentum, y);
            _burstingWithMomentum = true;
            _verticalBursting = false;
        }
        else
        {
            Debug.Log("VERTICAL BURST");
            float y = vel.y;
            _rb.velocity = new Vector2(0, y);
            _verticalBursting = true;
            _burstingWithMomentum = false;
        }

        _anim.CrossFade("Burst", 0, 0);
        float burstForce = CalculateJumpForce(burstHeight);
        AddJumpForce(burstForce);
        _isGrounded = false;
        _inBurstBuffer = true;
        _inPogo = false;
    }

    private void AddJumpForce(float force)
    {
        _rb.velocity = new Vector2(_rb.velocity.x, 0);
        if (_upsideDown) force *= -1;
        _rb.AddForce(new Vector2(0, force), ForceMode2D.Impulse);
    }

    private void PogoCheck()
    {
        
        if (_isGrounded)
        {
            _inPogo = false;
            _pogoHitbox.enabled = false;
            return;
        }

        if (_movementVector.y < -0.7 && pogoOn)
        {
            _inPogo = true;
            _pogoHitbox.enabled = true;
            _anim.CrossFade("Pogo", 0, 0);
        }

        if (_pogoHitboxController.OverlappingPogoable && _inPogo)
        {
            _pogoHitboxController.OverlappingPogoable = false;
            AddJumpForce(CalculateJumpForce(pogoHeight));
        }
    }

    //END HELPER METHODS//
}
