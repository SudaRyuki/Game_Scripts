using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

public class Player : MonoBehaviourPunCallbacks, IPunObservable
{
    #region コンポーネント参照
    private InputActionAsset playerActions;
    private Rigidbody2D rb;
    private Animator animator;
    private AudioSource audioSource;
    private SpriteRenderer[] allSpriteRenderers;
    #endregion

    #region Movement Settings
    [Header("Movement")][SerializeField] private float speed = 1.0f;
    [SerializeField] private float jumpForce = 5.0f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    public GameObject invincibleEffectPrefab;
    public GameObject droppedItemPrefab;
    #endregion

    #region Attack Settings
    [Header("Attack")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 0.5f;
    [SerializeField] private float attackDuration = 0.3f;
    [SerializeField] private float attackStartupTime = 0.2f;
    [SerializeField] private float attackCooldown = 1.0f;
    public GameObject attackCooldownGaugePrefab;
    #endregion

    #region Special Attack Settings
    [Header("Special Attack")]
    [SerializeField] private int maxSpecialPoints = 10;
    private int currentSpecialPoints = 0;
    public GameObject specialBeamPrefab;
    public GameObject specialGaugePrefab;
    private SpecialGauge mySpecialGauge;
    #endregion

    #region Shield & Alien Settings
    [Header("Shield")]
    public GameObject shieldObject;

    [Header("Alien Power")]
    public GameObject tentaclesObject;
    public GameObject alienBeamPrefab;
    public Transform alienBeamSpawnPoint;
    public GameObject helmetObject;
    [SerializeField] private float beamCooldown = 5.0f;
    public GameObject beamCooldownGaugePrefab;
    #endregion

    #region Layer & UI & Effects Settings
    [Header("レイヤー設定")]
    public LayerMask stageLayer;
    public LayerMask opponentLayer;

    [Header("UI / Indicators")]
    public GameObject youIndicator;
    public SpriteRenderer teamIndicatorSprite;

    [Header("Audio")]
    public AudioClip attackSound;
    public AudioClip shieldGetSound;
    public AudioClip speedUpGetSound;
    public AudioClip alienBeamSound;

    [Header("Effects")]
    public GameObject hitEffectPrefab;

    [Header("Color Settings")]
    public Material hueMaterialBase;
    public SpriteRenderer bodyRenderer;
    public SpriteRenderer[] tentacleRenderers;
    private float originalHue = 0f;
    #endregion

    #region 内部変数・状態フラグ
    public int teamID;

    // --- 状態管理フラグ ---
    private bool isJumping, isDead, isStunned, isInvincible, hasShield, isShielding, isAttacking, isAbducted, hasAlienPower;
    private bool isFiringSpecialLaser = false;

    // --- ネットワーク同期用変数 ---
    private float networkSpeed = 0f;
    private bool networkIsJumping = false;
    private float networkScaleX = 1f;
    private bool networkIsShielding = false;

    // --- その他内部変数 ---
    private float originalSpeed;
    private float nextAttackTime = 0f;
    private float nextBeamFireTime = 0f;
    private List<GameObject> hitOpponents = new List<GameObject>();
    private Coroutine speedUpCoroutine, invincibleCoroutine;
    private GameObject invincibleEffectInstance;
    public Vector3 invincibleEffectOffset = new Vector3(0, 0.5f, 0);
    private CooldownGauge attackCooldownGauge;
    private CooldownGauge beamCooldownGauge;
    #endregion

    #region Unity Lifecycle (Awake, Start, Update)
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        originalSpeed = speed;
        allSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    void Start()
    {
        // UIゲージの初期化を共通化
        attackCooldownGauge = CreateGaugeUI(attackCooldownGaugePrefab);
        beamCooldownGauge = CreateGaugeUI(beamCooldownGaugePrefab);

        if (PhotonNetwork.InRoom && photonView.IsMine)
        {
            if (youIndicator != null)
            {
                youIndicator.SetActive(true);
                Invoke(nameof(HideIndicator), 13.0f);
            }
        }

        if (bodyRenderer != null && bodyRenderer.material != null)
        {
            //オフライン版で現在のプレイヤーカラーをコピー
            Material independentMat = new Material(bodyRenderer.material);
            bodyRenderer.material = independentMat;

            //現在の色を元の色として記憶する
            if (independentMat.HasProperty("_ShiftValue"))
            {
                originalHue = independentMat.GetFloat("_ShiftValue");
            }


        }

        SetupTeamIndicator();
        SetupSpecialGauge();
    }

    void Update()
    {
        if (!GameManager.IsGameStarted || isFiringSpecialLaser) return;

        // 他人のネットワーク同期キャラクターの処理
        if (PhotonNetwork.InRoom && !photonView.IsMine)
        {
            UpdateNetworkedPlayer();
            return;
        }

        if (playerActions == null) return;

        // 行動不能状態ならスキップ
        if (!CanAct()) return;

        // 盾構え中は移動不可
        if (isShielding)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            animator.SetFloat("Speed", 0);
            return;
        }

        HandleMovement();
        HandleJump();
        HandleActionInput();
    }

    void OnDestroy()
    {
        if (playerActions != null)
        {
            playerActions["Shield"].performed -= _ => StartShielding();
            playerActions["Shield"].canceled -= _ => StopShielding();
            playerActions.Disable();
        }
    }
    #endregion

    #region Initialization & Setup Methods
    public void Initialize(InputActionAsset actions, InputDevice device = null)
    {
        playerActions = actions;
        if (device != null)
        {
            playerActions.devices = new[] { device };
        }
        playerActions["Shield"].performed += _ => StartShielding();
        playerActions["Shield"].canceled += _ => StopShielding();
        playerActions.Enable();
    }

    private CooldownGauge CreateGaugeUI(GameObject prefab)
    {
        if (prefab == null) return null;
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            GameObject gaugeObj = Instantiate(prefab, canvas.transform);
            gaugeObj.SetActive(false);
            return gaugeObj.GetComponent<CooldownGauge>();
        }
        return null;
    }

    private void SetupTeamIndicator()
    {
        if (teamIndicatorSprite != null)
        {
            if (teamID == 1) teamIndicatorSprite.color = new Color(1f, 0.3f, 0.3f, 0.8f);
            else if (teamID == 2) teamIndicatorSprite.color = new Color(0.3f, 0.6f, 1f, 0.8f);
        }
    }

    private void SetupSpecialGauge()
    {
        if (specialGaugePrefab != null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                GameObject gaugeObj = Instantiate(specialGaugePrefab, canvas.transform);
                mySpecialGauge = gaugeObj.GetComponent<SpecialGauge>();
                mySpecialGauge.Initialize(this.transform);
            }
        }
    }

    void HideIndicator()
    {
        if (youIndicator != null) youIndicator.SetActive(false);
    }
    #endregion

    #region Update Input Handling
    // 動ける状態かどうかの判定
    private bool CanAct()
    {
        return !(isDead || isStunned || isAbducted);
    }

    private void UpdateNetworkedPlayer()
    {
        animator.SetFloat("Speed", networkSpeed);
        animator.SetBool("Jump", networkIsJumping);
        animator.SetBool("IsShielding", isShielding);
        transform.localScale = new Vector3(networkScaleX, transform.localScale.y, transform.localScale.z);
    }

    private void HandleMovement()
    {
        Vector2 moveInput = playerActions["Move"].ReadValue<Vector2>();
        rb.linearVelocity = new Vector2(moveInput.x * speed, rb.linearVelocity.y);
        animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));

        // 向きの反転処理
        if (moveInput.x != 0)
        {
            float currentAbsX = Mathf.Abs(transform.localScale.x);
            // x > 0 なら左(-), x < 0 なら右(+)
            float newScaleX = moveInput.x > 0 ? -currentAbsX : currentAbsX;
            transform.localScale = new Vector3(newScaleX, transform.localScale.y, transform.localScale.z);

            if (youIndicator != null)
            {
                Vector3 tScale = youIndicator.transform.localScale;
                tScale.x = moveInput.x > 0 ? -Mathf.Abs(tScale.x) : Mathf.Abs(tScale.x);
                youIndicator.transform.localScale = tScale;
            }
        }
    }

    private void HandleJump()
    {
        if (playerActions["Jump"].WasPressedThisFrame() && IsGrounded())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isJumping = true;
            animator.SetBool("Jump", true);
        }

        if (isJumping && rb.linearVelocity.y <= 0.1f && IsGrounded())
        {
            isJumping = false;
            animator.SetBool("Jump", false);
        }
    }

    private void HandleActionInput()
    {
        // 通常攻撃
        if (Time.time >= nextAttackTime && playerActions["Attack"].WasPressedThisFrame())
        {
            nextAttackTime = Time.time + attackCooldown;
            if (attackCooldownGauge != null) attackCooldownGauge.StartCooldown(this.transform, attackCooldown);
            Attack();
        }

        // 必殺技
        if (playerActions["SpecialAttack"].WasPressedThisFrame())
        {
            if (currentSpecialPoints >= maxSpecialPoints && !isAttacking && !isFiringSpecialLaser)
            {
                currentSpecialPoints = 0;
                UpdateSpecialGaugeUI();

                if (PhotonNetwork.InRoom) photonView.RPC(nameof(RpcFireSpecialLaser), RpcTarget.All);
                else StartCoroutine(FireSpecialLaserCoroutine());
            }
        }
    }
    #endregion

    #region Combat (Attack & Special)
    void Attack()
    {
        if (PhotonNetwork.InRoom) photonView.RPC(nameof(RpcDelayedAttack), RpcTarget.All);
        else StartCoroutine(DelayedAttackCoroutine());
    }

    [PunRPC]
    private void RpcDelayedAttack()
    {
        StartCoroutine(DelayedAttackCoroutine());
    }

    private IEnumerator DelayedAttackCoroutine()
    {
        isAttacking = true;
        animator.SetTrigger("Attack");

        if (!PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient || photonView.IsMine)
        {
            yield return new WaitForSeconds(attackStartupTime);
            hitOpponents.Clear();
            float timer = 0f;

            while (timer < attackDuration)
            {
                Collider2D[] hitObjects = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, opponentLayer);
                foreach (Collider2D hit in hitObjects)
                {
                    Player2 opponent = hit.GetComponent<Player2>();
                    if (opponent != null && opponent.teamID != this.teamID && !hitOpponents.Contains(opponent.gameObject))
                    {
                        if (opponent.IsVulnerable())
                        {
                            if (PhotonNetwork.InRoom)
                            {
                                opponent.GetComponent<PhotonView>().RPC(nameof(Player2.TakeDamage), RpcTarget.All);
                                photonView.RPC(nameof(RpcPlayHitSound), RpcTarget.All);
                            }
                            else
                            {
                                opponent.TakeDamage();
                                RpcPlayHitSound();
                            }

                            hitOpponents.Add(opponent.gameObject);

                            if (photonView.IsMine || !PhotonNetwork.InRoom) AddSpecialPoint(1);
                        }
                    }
                }
                timer += Time.deltaTime;
                yield return null;
            }

            if (hasAlienPower && Time.time >= nextBeamFireTime)
            {
                if (!PhotonNetwork.InRoom || photonView.IsMine) FireAlienBeam();
                nextBeamFireTime = Time.time + beamCooldown;
               
                if (beamCooldownGauge != null) beamCooldownGauge.StartCooldown(this.transform, beamCooldown);
            }
        }
        else
        {
            yield return new WaitForSeconds(attackStartupTime + attackDuration);
        }
        isAttacking = false;
    }
    [PunRPC]
    private void RpcFireSpecialLaser()
    {
        StartCoroutine(FireSpecialLaserCoroutine());
    }

    private IEnumerator FireSpecialLaserCoroutine()
    {
        isFiringSpecialLaser = true;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        animator.SetTrigger("Attack");

        if (!PhotonNetwork.InRoom || photonView.IsMine)
        {
            GameObject laser = PhotonNetwork.InRoom
                ? PhotonNetwork.Instantiate(specialBeamPrefab.name, alienBeamSpawnPoint.position, alienBeamSpawnPoint.rotation)
                : Instantiate(specialBeamPrefab, alienBeamSpawnPoint.position, alienBeamSpawnPoint.rotation);

            if (laser != null)
            {
                float direction = transform.localScale.x > 0 ? -1 : 1;
                SpecialLaser laserScript = laser.GetComponent<SpecialLaser>();
                if (laserScript != null) laserScript.SetInitialData(this.teamID, direction);
            }
        }

        yield return new WaitForSeconds(2.0f);

        rb.gravityScale = originalGravity;
        isFiringSpecialLaser = false;
    }

    void FireAlienBeam()
    {
        if (alienBeamPrefab == null || alienBeamSpawnPoint == null) return;

        GameObject beam;
        if (PhotonNetwork.InRoom)
        {
            beam = PhotonNetwork.Instantiate(alienBeamPrefab.name, alienBeamSpawnPoint.position, alienBeamSpawnPoint.rotation);
            photonView.RPC(nameof(RpcPlayBeamSound), RpcTarget.All);
        }
        else
        {
            beam = Instantiate(alienBeamPrefab, alienBeamSpawnPoint.position, alienBeamSpawnPoint.rotation);
            RpcPlayBeamSound();
        }

        if (beam != null)
        {
            AlienBeam beamScript = beam.GetComponent<AlienBeam>();
            if (beamScript != null) beamScript.shooterTeamID = this.teamID;

            float direction = transform.localScale.x > 0 ? -1 : 1;
            Rigidbody2D beamRb = beam.GetComponent<Rigidbody2D>();
            if (beamRb != null) beamRb.linearVelocity = new Vector2(10f * direction, 0);
        }
    }

    public void AddSpecialPoint(int amount)
    {
        if (!PhotonNetwork.InRoom || photonView.IsMine)
        {
            currentSpecialPoints = Mathf.Min(currentSpecialPoints + amount, maxSpecialPoints);
            UpdateSpecialGaugeUI();
        }
    }

    private void UpdateSpecialGaugeUI()
    {
        if (mySpecialGauge != null) mySpecialGauge.UpdateGauge(currentSpecialPoints, maxSpecialPoints);
    }
    #endregion

    #region Damage & Status Effects (Stun, Invincible, Shield)
    public bool IsVulnerable()
    {
        return !(isStunned || isInvincible || isDead || isShielding || isAbducted);
    }

    [PunRPC]
    public void TakeDamage()
    {
        if (!IsVulnerable()) return;
        animator.SetTrigger("Hurt");
        StartCoroutine(StunCoroutine(3f));

        if (!PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient)
        {
            if (ScoreManager.instance.GetScore("Player") > 0)
            {
                ScoreManager.instance.AddScore("Player", -1);
                DropItem();
            }
        }
    }

    IEnumerator StunCoroutine(float duration)
    {
        isStunned = true;
        animator.SetBool("IsStunned", true);

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            rb.angularVelocity = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        }

        yield return new WaitForSeconds(duration);

        isStunned = false;
        animator.SetBool("IsStunned", false);
        if (rb != null) rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // 起き上がり後の無敵時間
        isInvincible = true;
        float invincibleDuration = 1.0f;
        float blinkInterval = 0.1f;
        float timer = 0f;

        while (timer < invincibleDuration)
        {
            foreach (var sr in allSpriteRenderers)
            {
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = (c.a >= 1.0f) ? 0.2f : 1.0f;
                    sr.color = c;
                }
            }
            yield return new WaitForSeconds(blinkInterval);
            timer += blinkInterval;
        }

        isInvincible = false;
        foreach (var sr in allSpriteRenderers)
        {
            if (sr != null)
            {
                Color c = sr.color;
                c.a = 1.0f;
                sr.color = c;
            }
        }
    }

    void StartShielding()
    {
        if (hasShield && !isDead && !isStunned && !isAbducted)
        {
            isShielding = true;
            animator.SetBool("IsShielding", true);
        }
    }

    void StopShielding()
    {
        if (isShielding)
        {
            isShielding = false;
            animator.SetBool("IsShielding", false);
        }
    }
    #endregion

    #region UFO Abduction & Return
    public void GetAbductedByUFO(Transform ufoTransform)
    {
        if (isAbducted || isStunned || isDead || isAttacking) return;

        if (PhotonNetwork.InRoom) photonView.RPC(nameof(RpcStartAbduction), RpcTarget.All, ufoTransform.GetComponent<PhotonView>().ViewID);
        else StartCoroutine(AbductionCoroutine(ufoTransform));
    }

    [PunRPC]
    private void RpcStartAbduction(int ufoViewID)
    {
        PhotonView ufoView = PhotonView.Find(ufoViewID);
        if (ufoView != null) StartCoroutine(AbductionCoroutine(ufoView.transform));
    }

    private void SetPlayerActive(bool active)
    {
        foreach (var sr in allSpriteRenderers)
        {
            if (sr != null) sr.enabled = active;
        }
        if (rb != null) rb.simulated = active;

        if (!active)
        {
            if (youIndicator != null) youIndicator.SetActive(false);
            if (attackCooldownGauge != null) attackCooldownGauge.gameObject.SetActive(false);
            if (beamCooldownGauge != null) beamCooldownGauge.gameObject.SetActive(false);
        }
    }

    private IEnumerator AbductionCoroutine(Transform ufoTransform)
    {
        isAbducted = true;
        rb.simulated = false;
        animator.enabled = false;

        Vector3 offset = transform.position - ufoTransform.position;
        float flyAwayTime = 2.0f;
        float timer = 0f;

        while (timer < flyAwayTime)
        {
            if (ufoTransform != null) transform.position = ufoTransform.position + offset;
            transform.Rotate(0, 0, 720f * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }

        SetPlayerActive(false);
        GameManager.instance.HandlePlayerReturn(this, 15f);
    }

    [PunRPC]
    public void RpcBeginReturn(int ufoViewID)
    {
        PhotonView ufoView = PhotonView.Find(ufoViewID);
        if (ufoView != null) StartCoroutine(ReturnCoroutine(ufoView.transform));
    }

    public void StartReturnSequence(Transform returnUfoTransform)
    {
        StartCoroutine(ReturnCoroutine(returnUfoTransform));
    }

    private IEnumerator ReturnCoroutine(Transform returnUfoTransform)
    {
        SetPlayerActive(true);
        isAbducted = true;
        rb.simulated = false;
        animator.enabled = true;

        yield return null;

        ResetStatus();
        if (PhotonNetwork.InRoom) photonView.RPC(nameof(RpcResetRotation), RpcTarget.All);
        else RpcResetRotation();

        Vector3 offset = new Vector3(0, -1, 0);
        float duration = 2.5f;
        float elapsed = 0f;

        while (elapsed < duration && returnUfoTransform != null)
        {
            transform.position = returnUfoTransform.position + offset;
            elapsed += Time.deltaTime;
            yield return null;
        }

        rb.simulated = true;
        isAbducted = false;

        if (speedUpCoroutine != null)
        {
            StopCoroutine(speedUpCoroutine);
            speedUpCoroutine = null;
            speed = originalSpeed;
        }

        if (invincibleCoroutine != null)
        {
            StopCoroutine(invincibleCoroutine);
            invincibleCoroutine = null;
            isInvincible = false;
            if (invincibleEffectInstance != null) Destroy(invincibleEffectInstance);
        }

        if (PhotonNetwork.InRoom) photonView.RPC(nameof(RpcActivateAlienPower), RpcTarget.All);
        else RpcActivateAlienPower();
    }

    private void ResetStatus()
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        transform.rotation = Quaternion.identity;

        isAttacking = false;
        isJumping = false;
        isShielding = false;
        isStunned = false;

        animator.SetTrigger("ForceIdle");
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Hurt");
        animator.SetBool("Jump", false);
        animator.SetBool("IsShielding", false);
        animator.SetBool("IsStunned", false);
        animator.SetFloat("Speed", 0);
    }

    [PunRPC]
    private void RpcResetRotation()
    {
        transform.rotation = Quaternion.identity;
    }

    [PunRPC]
    private void RpcActivateAlienPower()
    {
        if (helmetObject != null) helmetObject.SetActive(false);
        if (tentaclesObject != null) tentaclesObject.SetActive(true);

        hasAlienPower = true;

        if (NotificationManager.instance != null)
        {
            string playerName = gameObject.name.Replace("(Clone)", "");
            NotificationManager.instance.ShowNotification(playerName + " Alien Power!");
        }
    }
    #endregion

    #region Items & Triggers
    public void SpeedUp(float multiplier, float duration)
    {
        if (PhotonNetwork.InRoom) photonView.RPC(nameof(RpcSpeedUp), RpcTarget.All, multiplier, duration);
        else RpcSpeedUp(multiplier, duration);
    }

    [PunRPC]
    private void RpcSpeedUp(float multiplier, float duration)
    {
        if (speedUpCoroutine != null) StopCoroutine(speedUpCoroutine);
        speedUpCoroutine = StartCoroutine(SpeedUpRoutine(multiplier, duration));
    }

    private IEnumerator SpeedUpRoutine(float multiplier, float duration)
    {
        if (originalSpeed == 0) originalSpeed = speed;
        speed = originalSpeed * multiplier;
        yield return new WaitForSeconds(duration);
        speed = originalSpeed;
        speedUpCoroutine = null;
    }

    public void BecomeInvincible(float duration)
    {
        if (PhotonNetwork.InRoom) photonView.RPC(nameof(RpcBecomeInvincible), RpcTarget.All, duration);
        else RpcBecomeInvincible(duration);
    }

    [PunRPC]
    private void RpcBecomeInvincible(float duration)
    {
        if (audioSource != null && speedUpGetSound != null) audioSource.PlayOneShot(speedUpGetSound);
        if (invincibleCoroutine != null) StopCoroutine(invincibleCoroutine);
        invincibleCoroutine = StartCoroutine(InvincibilityRoutine(duration));
    }

    private IEnumerator InvincibilityRoutine(float duration)
    {
        isInvincible = true;

        float timer = 0f;
        float rainbowSpeed = 1000f;
        float currentHue = originalHue;

        // 無敵時間の間、プレイヤーのカラーを変更
        while (timer < duration)
        {
            currentHue += rainbowSpeed * Time.deltaTime;
            if (currentHue >= 360f) currentHue -= 360f;

            // 計算した色をキャラクターに適用する
            SetMaterialHue(currentHue);

            timer += Time.deltaTime;
            yield return null;
        }

        isInvincible = false;

        // 無敵が終わったら、元の色に戻る
        SetMaterialHue(originalHue);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (PhotonNetwork.InRoom && !photonView.IsMine) return;

        if (other.CompareTag("Item") || other.CompareTag("ShieldItem") || other.GetComponent<SpeedUpItem>() != null)
        {
            if (!PhotonNetwork.InRoom) HandleItemPickup(other.gameObject, this.photonView);
            else
            {
                PhotonView itemPhotonView = other.GetComponent<PhotonView>();
                if (itemPhotonView != null) photonView.RPC(nameof(RpcNotifyItemPickup), RpcTarget.MasterClient, itemPhotonView.ViewID, this.photonView.ViewID);
            }
        }
    }

    [PunRPC]
    private void RpcNotifyItemPickup(int itemViewID, int playerViewID)
    {
        GameObject itemObject = PhotonView.Find(itemViewID)?.gameObject;
        PhotonView playerPhotonView = PhotonView.Find(playerViewID);

        if (itemObject == null || playerPhotonView == null || !PhotonNetwork.IsMasterClient) return;

        HandleItemPickup(itemObject, playerPhotonView);
    }

    private void HandleItemPickup(GameObject itemObject, PhotonView playerPhotonView)
    {
        if (itemObject.CompareTag("Item"))
        {
            ScoreManager.instance.AddScore("Player", 1);
        }
        else if (itemObject.CompareTag("ShieldItem"))
        {
            Player p = playerPhotonView.GetComponent<Player>();
            if (p != null) p.GrantShield();
            else playerPhotonView.GetComponent<Player2>()?.GrantShield();
        }
        else if (itemObject.GetComponent<SpeedUpItem>() != null)
        {
            SpeedUpItem item = itemObject.GetComponent<SpeedUpItem>();
            Player p = playerPhotonView.GetComponent<Player>();
            Player2 p2 = playerPhotonView.GetComponent<Player2>();

            if (p != null)
            {
                p.SpeedUp(item.speedMultiplier, item.duration);
                p.BecomeInvincible(item.invincibleDuration);
            }
            else if (p2 != null)
            {
                p2.SpeedUp(item.speedMultiplier, item.duration);
                p2.BecomeInvincible(item.invincibleDuration);
            }
        }

        if (!PhotonNetwork.InRoom) Destroy(itemObject);
        else if (PhotonNetwork.IsMasterClient) PhotonNetwork.Destroy(itemObject);
    }

    public void GrantShield()
    {
        if (PhotonNetwork.InRoom) photonView.RPC(nameof(RpcGrantShield), RpcTarget.All);
        else RpcGrantShield();
    }

    [PunRPC]
    private void RpcGrantShield()
    {
        hasShield = true;
        if (shieldObject != null) shieldObject.SetActive(true);
        if (audioSource != null && shieldGetSound != null) audioSource.PlayOneShot(shieldGetSound);
    }

    void DropItem()
    {
        if (droppedItemPrefab == null) return;

        Vector3 spawnPos = transform.position + new Vector3(0, 1f, 0);
        GameObject dropped = PhotonNetwork.InRoom ? PhotonNetwork.Instantiate(droppedItemPrefab.name, spawnPos, Quaternion.identity) : Instantiate(droppedItemPrefab, spawnPos, Quaternion.identity);

        if (!PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient)
        {
            Rigidbody2D itemRb = dropped.GetComponent<Rigidbody2D>();
            if (itemRb != null) itemRb.AddForce(new Vector2(Random.Range(-3f, 3f), Random.Range(5f, 7f)), ForceMode2D.Impulse);
        }
    }
    #endregion

    #region Audio, Visuals & Network Serialization
    [PunRPC]
    private void RpcPlayHitSound()
    {
        if (audioSource != null && attackSound != null) audioSource.PlayOneShot(attackSound);
        if (hitEffectPrefab != null && attackPoint != null) Instantiate(hitEffectPrefab, attackPoint.position, Quaternion.identity);
    }

    [PunRPC]
    private void RpcPlayBeamSound()
    {
        if (audioSource != null && alienBeamSound != null) audioSource.PlayOneShot(alienBeamSound, 0.5f);
    }

    [PunRPC]
    public void RpcSetColor(int colorId)
    {
        if (hueMaterialBase == null) return;

        Material mat = new Material(hueMaterialBase);
        float[] hues = NetworkManager.ColorHues;
        float shiftValue = (colorId >= 0 && colorId < hues.Length) ? hues[colorId] : hues[0];

        // 自分の色を記憶しておく
        originalHue = shiftValue;

        // マテリアルを適用する
        if (bodyRenderer != null) bodyRenderer.material = mat;
        if (tentacleRenderers != null)
        {
            foreach (var tr in tentacleRenderers) { if (tr != null) tr.material = mat; }
        }

        // 記憶した色をセットする
        SetMaterialHue(originalHue);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(animator.GetFloat("Speed"));
            stream.SendNext(animator.GetBool("Jump"));
            stream.SendNext(transform.localScale.x);
            stream.SendNext(isShielding);
            stream.SendNext(gameObject.activeSelf);
            stream.SendNext(teamID);
            stream.SendNext(isAbducted);
            stream.SendNext(currentSpecialPoints);
        }
        else
        {
            this.networkSpeed = (float)stream.ReceiveNext();
            this.networkIsJumping = (bool)stream.ReceiveNext();
            this.networkScaleX = (float)stream.ReceiveNext();
            this.isShielding = (bool)stream.ReceiveNext();
            animator.SetBool("IsShielding", this.isShielding);

            bool isActive = (bool)stream.ReceiveNext();
            if (gameObject.activeSelf != isActive) gameObject.SetActive(isActive);

            this.teamID = (int)stream.ReceiveNext();

            bool newIsAbducted = (bool)stream.ReceiveNext();
            if (newIsAbducted && !this.isAbducted) GetComponent<PhotonRigidbody2DView>().enabled = false;
            else if (!newIsAbducted && this.isAbducted) GetComponent<PhotonRigidbody2DView>().enabled = true;
            this.isAbducted = newIsAbducted;

            this.currentSpecialPoints = (int)stream.ReceiveNext();
            UpdateSpecialGaugeUI();
        }
    }

    bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, stageLayer);
    }

    private void SetMaterialHue(float hueValue)
    {
        if (bodyRenderer != null && bodyRenderer.material != null)
        {
            bodyRenderer.material.SetFloat("_ShiftValue", hueValue);
        }
        if (tentacleRenderers != null)
        {
            foreach (var tr in tentacleRenderers)
            {
                if (tr != null && tr.material != null) tr.material.SetFloat("_ShiftValue", hueValue);
            }
        }
    }
    #endregion
}