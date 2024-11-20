using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using Cinemachine;

public class PlayerScript : MonoBehaviourPunCallbacks, IPunObservable
{
    // Player movement related variables
    public Rigidbody2D RB;
    public Animator AN;
    public SpriteRenderer SR;
    public PhotonView PV;
    public Text NickNameText;
    public Image HealthImage;
    public GameObject drawLinePrefab;

    // Drawing line related variables
    private LineRenderer lr;
    private EdgeCollider2D collider2D;
    private List<Vector2> points = new List<Vector2>();
    private bool isDrawing = false;

    bool isGround;
    Vector3 curPos;

    void Awake()
    {
        // Set nickname
        NickNameText.text = PV.IsMine ? PhotonNetwork.NickName : PV.Owner.NickName;
        NickNameText.color = PV.IsMine ? Color.green : Color.red;

        if (PV.IsMine)
        {
            // Connect 2D camera
            var CM = GameObject.Find("CMCamera").GetComponent<CinemachineVirtualCamera>();
            CM.Follow = transform;
            CM.LookAt = transform;
        }
    }

    void Start()
    {
        if (PV.IsMine)
        {
            // Initialize DrawLine functionality
            var drawLineObject = Instantiate(drawLinePrefab);
            drawLineObject.GetComponent<PhotonView>().RequestOwnership();
        }
    }

    void Update()
    {
        if (PV.IsMine)
        {
            HandleMovement();
            HandleJump();
            HandleShooting();
            HandleDrawing();
        }
        else
        {
            SmoothSyncPosition();
        }
    }

    void HandleMovement()
    {
        float axis = Input.GetAxisRaw("Horizontal");
        RB.velocity = new Vector2(4 * axis, RB.velocity.y);

        AN.SetBool("walk", axis != 0);

        if (axis != 0)
        {
            PV.RPC("FlipXRPC", RpcTarget.AllBuffered, axis);
        }
    }

    void HandleJump()
    {
        // Check for collisions with both "Ground" and "Line" layers
        LayerMask groundMask = 1 << LayerMask.NameToLayer("Ground");
        LayerMask lineMask = 1 << LayerMask.NameToLayer("Line");
        LayerMask combinedMask = groundMask | lineMask;

        // Perform a check for the combined mask to determine if the player is grounded
        isGround = Physics2D.OverlapCircle((Vector2)transform.position + new Vector2(0, -0.5f), 0.07f, combinedMask);
        AN.SetBool("jump", !isGround);

        if (Input.GetKeyDown(KeyCode.UpArrow) && isGround)
        {
            PV.RPC("JumpRPC", RpcTarget.All);
        }
    }

    void HandleShooting()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var bullet = PhotonNetwork.Instantiate("Bullet", transform.position + new Vector3(SR.flipX ? -0.4f : 0.4f, -0.11f, 0), Quaternion.identity);
            bullet.GetComponent<PhotonView>().RPC("DirRPC", RpcTarget.All, SR.flipX ? -1 : 1);
            AN.SetTrigger("shot");
        }
    }

    void HandleDrawing()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StartDrawing();
        }
        else if (Input.GetMouseButton(0) && isDrawing)
        {
            AddPoint();
        }
        else if (Input.GetMouseButtonUp(0) && isDrawing)
        {
            StopDrawing();
        }
    }

    private void StartDrawing()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogError("PhotonNetwork is not connected. Cannot start drawing.");
            return;
        }

        GameObject go = PhotonNetwork.Instantiate(drawLinePrefab.name, Vector3.zero, Quaternion.identity);
        lr = go.GetComponent<LineRenderer>();
        collider2D = go.GetComponent<EdgeCollider2D>();
        points.Clear();
        Vector2 startPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        points.Add(startPos);
        lr.positionCount = 1;
        lr.SetPosition(0, startPos);
        isDrawing = true;

        PV.RPC("StartDrawingRPC", RpcTarget.OthersBuffered, startPos);
    }

    private void AddPoint()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogError("PhotonNetwork is not connected. Cannot add points.");
            return;
        }

        Vector2 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        points.Add(pos);
        lr.positionCount++;
        lr.SetPosition(lr.positionCount - 1, pos);
        collider2D.points = points.ToArray();
        PV.RPC("AddPointRPC", RpcTarget.OthersBuffered, pos);
    }

    private void StopDrawing()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogError("PhotonNetwork is not connected. Cannot stop drawing.");
            return;
        }

        points.Clear();
        isDrawing = false;

        PV.RPC("StopDrawingRPC", RpcTarget.OthersBuffered);
    }

    void SmoothSyncPosition()
    {
        transform.position = Vector3.Lerp(transform.position, curPos, Time.deltaTime * 10);
    }

    [PunRPC]
    void FlipXRPC(float axis) => SR.flipX = axis == -1;

    [PunRPC]
    void JumpRPC()
    {
        RB.velocity = Vector2.zero;
        RB.AddForce(Vector2.up * 700);
    }

    [PunRPC]
    void StartDrawingRPC(Vector2 startPos)
    {
        if (!PV.IsMine)
        {
            GameObject go = Instantiate(drawLinePrefab);
            lr = go.GetComponent<LineRenderer>();
            collider2D = go.GetComponent<EdgeCollider2D>();
            points.Clear();
            points.Add(startPos);
            lr.positionCount = 1;
            lr.SetPosition(0, startPos);
        }
    }

    [PunRPC]
    void AddPointRPC(Vector2 point)
    {
        if (!PV.IsMine)
        {
            points.Add(point);
            lr.positionCount++;
            lr.SetPosition(lr.positionCount - 1, point);
            collider2D.points = points.ToArray();
        }
    }

    [PunRPC]
    void StopDrawingRPC()
    {
        if (!PV.IsMine)
        {
            points.Clear();
            isDrawing = false;
        }
    }

    public void Hit()
    {
        HealthImage.fillAmount -= 0.1f;
        if (HealthImage.fillAmount <= 0)
        {
            GameObject.Find("Canvas").transform.Find("RespawnPanel").gameObject.SetActive(true);
            PV.RPC("DestroyRPC", RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    void DestroyRPC() => Destroy(gameObject);

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(HealthImage.fillAmount);
        }
        else
        {
            curPos = (Vector3)stream.ReceiveNext();
            HealthImage.fillAmount = (float)stream.ReceiveNext();
        }
    }
}
