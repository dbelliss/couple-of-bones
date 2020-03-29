﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static bool blockingInput = false;

    [SerializeField]
    PlayerType playerType;
    public enum PlayerType {
        RED = 0,
        BLUE = 1,
        ON = 2,
        OFF = 3
    }

    public static PlayerController player1;
    public static PlayerController player2;

    GameManager gameManager;
    WorldCanvas worldCanvas;

    bool isJumping = false;
    bool isMovingRight = false;
    bool isMovingLeft = false;
    bool isGrabbing = false;
    bool startGrabbing = false;
    bool stopGrabbing = false;
    bool isPulling = false;
    bool startPulling = false;
    bool stopPulling = false;
    bool isLifting = false;
    bool startLifting = false;
    bool stopLifting = false;
    bool isGrounded = false;
    bool startHolding = false;
    bool stopHolding = false;
    bool isHolding = false;
    bool isHoldingSwap = false;
    Rigidbody2D rb;

    [SerializeField]
    Transform groundCheck;

    [SerializeField]
    float jumpForce = 1f;

    [SerializeField]
    float liftForce = 20f;

    [SerializeField]
    float movementSpeed = 1;

    float swapCooldown = .2f;
    float lastSwap = 0;

    [SerializeField]
    float friction = .4f;

    [SerializeField]
    float pullForce = 1;

    [SerializeField]
    float springActivationDistance = 2.5f;

    SpringJoint2D springJoint;
    DistanceJoint2D distanceJoint;

    [SerializeField]
    float maxDistanceJointDistance = 3f;

    public float curDistanceJointDistance = 3f;

    [SerializeField]
    float maxPullPower = 50f;

    [SerializeField]
    LiftTrigger liftTrigger;

    // Animations
    SpriteRenderer sr;
    Animator animator;
    const string animIsWalkingID = "isWalking";
    const string animVerticalSpeedID = "verticalSpeed";
    const string animIsGroundedID = "isGrounded";
    const string animIsGrabbing = "isGrabbing";
    const string animRespawnID = "respawn";
    const string animDieID = "die";

    // Music
    AudioSource jumpSource;

    [SerializeField]
    Transform grabPivot;
    [SerializeField]
    float grabDistance = 1f;
    Ring grabbedRing;


    private void Awake()
    {
        gameManager = GameManager.instance;
        if (gameManager.isSinglePlayer())
        {
            if (playerType == PlayerType.RED)
            {
                playerType = PlayerType.ON;
                player1 = this;
            }
            else
            {
                playerType = PlayerType.OFF;
                player2 = this;
            }
        }
        else
        {
            if (playerType == PlayerType.RED)
            {
                player1 = this;
            }
            else
            {
                player2 = this;
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        jumpSource = GetComponent<AudioSource>();
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        distanceJoint = GetComponent<DistanceJoint2D>();
        springJoint = GetComponent<SpringJoint2D>();

        if (groundCheck == null)
        {
            Debug.LogError("No ground check set for " + name);
        }

        worldCanvas = WorldCanvas.instance;
    }

    // Update is called once per frame
    void Update()
    {
        if (!blockingInput)
        {
            GetInputs();
        }

        CheckGrounded();
        UpdateAnimations();
    }

    void UpdateAnimations()
    {
        if (rb.velocity.x > 0)
        {
            sr.flipX = false;
        }
        if (rb.velocity.x < 0)
        {
            sr.flipX = true;
        }

        if (Mathf.Abs(rb.velocity.x) > .1f)
        {
            animator.SetBool(animIsWalkingID, true);
        }
        else
        {
            animator.SetBool(animIsWalkingID, false);
        }

        animator.SetFloat(animVerticalSpeedID, rb.velocity.y);
        animator.SetBool(animIsGroundedID, isGrounded);
        animator.SetBool(animIsGrabbing, isGrabbing);
    }

    void CheckGrounded()
    {
        if (groundCheck)
        {
            Debug.DrawLine(groundCheck.transform.position, groundCheck.transform.position + Vector3.down * .1f, Color.red, .5f);
            RaycastHit2D[] hits = Physics2D.CircleCastAll(groundCheck.position, .05f, Vector2.down, .2f);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.transform.gameObject.tag == GameManager.GROUND_TAG)
                {
                    isGrounded = true;
                    return;
                }
            }
            
        }
        isGrounded = false;
    }

    Vector3 GetTextSpawnPosition()
    {
        return transform.position + Vector3.one * .05f;
    }

    void GetInputs()
    {
        float swap = Input.GetAxisRaw("Swap");
     
        if(swap != 0)
        {
            if ((Time.time - lastSwap) > swapCooldown && !isHoldingSwap && gameManager.isSinglePlayer())
            {
                isHoldingSwap = true;
                worldCanvas.SpawnText(GetTextSpawnPosition(), "Swap!", Color.black);
                lastSwap = Time.time;
                if (playerType == PlayerType.ON)
                {
                    playerType = PlayerType.OFF;
                }
                else
                {
                    playerType = PlayerType.ON;
                }
            }
        }
        else {
            isHoldingSwap = false;
        }

        if (playerType == PlayerType.OFF)
        {
            return;
        }

        string playerInputIdentifier = "";
        if (playerType == PlayerType.ON || playerType == PlayerType.RED)
        {
            playerInputIdentifier = "0";
        }
        else
        {
            playerInputIdentifier = "1";
        }

        float horizontal = Input.GetAxisRaw("Horizontal" + playerInputIdentifier);
        if (horizontal > 0)
        {
            isMovingRight = true;
            isMovingLeft = false;
        }
        else if(horizontal < 0)
        {
            isMovingLeft = true;
            isMovingRight = false;
        }
        else
        {
            isMovingLeft = false;
            isMovingRight = false;
        }

        float jump = Input.GetAxisRaw("Jump" + playerInputIdentifier);
        if (jump > 0)
        {
            isJumping = true;
        }

        float grab = Input.GetAxis("Grab" + playerInputIdentifier);
        if (grab > 0)
        {
            startGrabbing = true;
        }
        else
        {
            stopGrabbing = true;
        }

        float pull = Input.GetAxis("Pull" + playerInputIdentifier);
        if (pull > 0) {
            startPulling = true;
        }
        else
        {
            stopPulling = true;
        }

        float lift = Input.GetAxis("Lift" + playerInputIdentifier);
        if (lift > 0) {
            startLifting = true;
        }
        else
        {
            stopLifting = true;
        }

        float hold = Input.GetAxis("Hold" + playerInputIdentifier);
        if (hold > 0)
        {
            startHolding = true;
        }
        else
        {
            stopHolding = true;
        }


        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            Die();
        }
    }

    private void FixedUpdate()
    {
        if (this == player1)
        {
            if (Vector2.Distance(player1.transform.position, player2.transform.position) > springActivationDistance)
            {
                springJoint.enabled = true;
            }
            else
            {
                springJoint.enabled = false;
            }
        }

        if (isJumping && isGrounded || isGrabbing)
        {
            jumpSource.Play();
            rb.AddForce(Vector2.up * jumpForce);
        }

        if (isMovingLeft)
        {
            rb.velocity = new Vector2(-1 * movementSpeed, rb.velocity.y);
        }
        else if (isMovingRight)
        {
            rb.velocity = new Vector2(movementSpeed, rb.velocity.y);
        }
        else
        {
            rb.velocity = new Vector2(rb.velocity.x * friction, rb.velocity.y);
        }

        if (startPulling && !isPulling)
        {
            isPulling = true;
            worldCanvas.SpawnText(GetTextSpawnPosition(), "Get over here!", Color.black);
        }
        else if (stopPulling && isPulling)
        {
            isPulling = false;
        }
        if (isPulling)
        {
            if (player1 == this)
            {
                player2.Pulled();
            }
            else
            {
                player1.Pulled();
            }
        }

        if (startGrabbing && !isGrabbing)
        {
            isGrabbing = true;
        }
        else if (stopGrabbing && isGrabbing)
        {
            isGrabbing = false;
            worldCanvas.SpawnText(transform.position + Vector3.one, "Release!", Color.black);
        }
        if (isGrabbing)
        {
            Ring ring = RingManager.instance.GetClosestRing(grabPivot.position, grabDistance);
            if (ring)
            {
                Vector3 pivotOffset = grabPivot.localPosition;
                Debug.Log(pivotOffset);
                Debug.Log(ring.transform.position);
                transform.position = ring.transform.position - pivotOffset;
                rb.velocity = new Vector2(rb.velocity.x, 0);
            }
            else
            {
                Debug.Log("No ring");
                isGrabbing = false;
            }
        }

        if (startHolding && !isHolding)
        {
            worldCanvas.SpawnText(GetTextSpawnPosition(), "Holding tight!", Color.black);
            isHolding = true;
            curDistanceJointDistance = Vector2.Distance(player1.transform.position, player2.transform.position);
            UpdateDistanceJointDistance();
        }
        if (stopHolding && isHolding)
        {
            worldCanvas.SpawnText(GetTextSpawnPosition(), "Letting go!", Color.black);
            isHolding = false;
            curDistanceJointDistance = maxDistanceJointDistance;
            UpdateDistanceJointDistance();
        }

        if (startLifting && !isLifting)
        {
            worldCanvas.SpawnText(GetTextSpawnPosition(), "Ready!", Color.black);
            isLifting = true;
            liftTrigger.SetTrigger(true);
        }
        
        if (stopLifting && isLifting)
        {
            worldCanvas.SpawnText(GetTextSpawnPosition(), "Done!", Color.black);
            liftTrigger.SetTrigger(false);
            isLifting = false;
        }

        // Mark inputs as processed
        stopHolding = false;
        startHolding = false;
        startLifting = false;
        stopLifting = false;
        isJumping = false;
        startPulling = false;
        stopPulling = false;
        startGrabbing = false;
        stopGrabbing = false;

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log(collision.gameObject.tag);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log(collision.gameObject.tag);
        if (collision.gameObject.tag == GameManager.SPIKE_TAG)
        {
            Die();   
        }
    }

    public void Toss()
    {
        Debug.Log("Tossing");
        rb.AddForce(liftForce * Vector2.up);
    }

    public void Die()
    {
        Debug.Log("Dying");
        animator.SetTrigger(animDieID);
        RoomManager.instance.StartRespawn();
    }

    public void Pulled()
    {
        Debug.Log("pulled");
        float distance = Vector2.Distance(player1.transform.position, player2.transform.position);
        float pullpower = maxPullPower * (distance / maxDistanceJointDistance);

        Vector2 direction;
        if (this == player1)
        {
            direction = (player2.transform.position - transform.position).normalized;
        }
        else
        {
            direction = (player1.transform.position - transform.position).normalized;
        }

        Debug.DrawLine(transform.position, transform.position + (Vector3)direction * 5, Color.red, 1f);

        rb.AddForce(direction * pullpower);
    }

    public PlayerController GetOtherPlayer()
    {
        if (this == player1)
        {
            return player2;
        }
        return player1;
    }

    public PlayerType GetPlayerType()
    {
        return playerType;
    }

    public void UpdateDistanceJointDistance()
    {
        player1.distanceJoint.distance = Mathf.Min(curDistanceJointDistance, GetOtherPlayer().curDistanceJointDistance);
    }

    public void Respawn()
    {
        animator.SetTrigger(animRespawnID);
        transform.position = RoomManager.instance.GetCheckpoint().transform.position + new Vector3(Random.Range(-.5f, .5f), 0, 0);
    }
}
