using System;
using System.Linq.Expressions;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using TMPro;


public class PlayerControler : NetworkBehaviour
{
    GameManager gameManager;
    TMP_Text lblName; //nombre del jugador creado por el GM.

    [Header("Cosas para moverse asi bien rico")]
    public Vector3 desiredDir;

    //rapidez
    public float RunSpeed;

    //vida
    NetworkVariable<int> health = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    NetworkVariable<int> nickId = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    NetworkVariable<bool> isBerserker = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    NetworkVariable<bool> isAttacking = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);


    [Tooltip("En estado de berserk, cuanto se va a mover de rapido extra")]
    public float multBerserk = 2;

    public float AttackTimer;
    public float AttackCD;
    [Header("SFX")] public ParticleSystem psBerkserk;
    [Header("SFX")] public ParticleSystem psAttacking;

    public AudioClip TakeDmgSound;
    public AudioClip DeathSound;
    Animator animator;
    AudioSource audioSource;

    public GameObject collider;

    void Start()
    {
        AttackCD = 0.5f;
        desiredDir = Vector3.zero;
        animator = GetComponent<Animator>();
        gameManager = GameObject.Find("Game Manager").GetComponent<GameManager>();
        audioSource = GetComponent<AudioSource>();
        lblName = Instantiate(gameManager.playerNameTemplate,
            gameManager.playerNameTemplate.transform.parent);

        lblName.enabled = true;

        //Conectar el nombre del id con el dropdown
        if (IsOwner)
        {
            nickId.Value = gameManager.dropdownNames.value;
        }
    }

    public override void OnNetworkSpawn()
    {
        isBerserker.OnValueChanged += SetBerserk;
        isAttacking.OnValueChanged += SetAttack;
    }

    public override void OnNetworkDespawn()
    {
        isBerserker.OnValueChanged -= SetBerserk;
        isAttacking.OnValueChanged -= SetAttack;
        Debug.Log("Se desconecto: " + lblName.text);
        if (lblName != null)
        {
            Destroy(lblName.gameObject);
        }


        if (IsOwner)
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (IsOwner) //Para que no se muevan todos los jugadores.
        {
            desiredDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0,
                Input.GetAxisRaw("Vertical"));

            if (desiredDir != Vector3.zero)
            {
                Quaternion q = Quaternion.LookRotation(desiredDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, q, 10 * Time.deltaTime);
                desiredDir.Normalize();

                float speed = desiredDir.sqrMagnitude * RunSpeed * Time.deltaTime *
                              (isBerserker.Value ? multBerserk : 1);
                transform.Translate(0, 0, speed);
            }

            if (Input.GetButtonDown("Fire3"))
            {
                ResquestSetBerserk_ServerRpc(true);
            }

            if (Input.GetKeyDown(KeyCode.L) && AttackTimer <= 0)
            {
                Attack();
                AttackTimer = AttackCD;
            }

            if (Input.GetKeyDown(KeyCode.N))
            {
                NetworkManager.Singleton.Shutdown();
            }

            if (AttackTimer > 0)
            {
                AttackTimer -= Time.deltaTime;
            }

            //Actualizar HUD
            gameManager.lblHealth.text = health.Value + "";
            // animator.SetBool("isAttacking", isAttacking.Value);
        }

        if (lblName != null)
        {
            lblName.text = gameManager.dropdownNames.options[nickId.Value].text;
            lblName.transform.position = Camera.main.WorldToScreenPoint(transform.position) + new Vector3(0, 150, 0);
        }
    }

    [ServerRpc]
    void ResquestSetBerserk_ServerRpc(bool newState)
    {
        Debug.Log("Request de berserk" + " " + newState);
        isBerserker.Value = !isBerserker.Value;
    }


    [ServerRpc(RequireOwnership = false)]
    void RequestSetAttack_ServerRpc(bool newState)
    {
        isAttacking.Value = newState;
    }


    [ServerRpc(RequireOwnership = false)]
    void RequestTakeDamage_ServerRpc(int damage)
    {
        if (isAlive())
        {
            health.Value -= damage;
            if (health.Value <= 0)
            {
                OnDeath();
                health.Value = 0;
            }
            else
            {
                audioSource?.PlayOneShot(TakeDmgSound);
            }
        }
    }

    //Activar efectos de particulas
    public void SetBerserk(bool old, bool nuevo)
    {
        if (psBerkserk != null)
        {
            psBerkserk.gameObject.SetActive(isBerserker.Value);
            //Activar Sonido.
        }
    }

    public void SetAttack(bool old, bool nuevo)
    {
        if (psAttacking != null)
        {
            psAttacking.gameObject.SetActive(isAttacking.Value);
            animator.SetBool("isAttacking", isAttacking.Value);
        }
    }

    public void TakeDamage(int damage)
    {
        if (IsOwner)
        {
            RequestTakeDamage_ServerRpc(damage);
        }
    }

    public void Attack()
    {
        RequestSetAttack_ServerRpc(true);
        collider.SetActive(true);
        //animator.SetBool("isAttacking", true);
    }

    public void StopAttacking()
    {
        RequestSetAttack_ServerRpc(false);
        // animator.SetBool("isAttacking", false);
        collider.SetActive(false);
    }

    bool isAlive()
    {
        return health.Value > 0;
    }

    public void OnDeath()
    {
        audioSource?.PlayOneShot(DeathSound);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.gameObject.name);
        if (other.CompareTag("Attack"))
        {
            Debug.Log("HIT");

            RequestTakeDamage_ServerRpc(1);
        }
    }
}