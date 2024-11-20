using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class DrawLine : MonoBehaviourPunCallbacks
{
    public GameObject linePrefab;  // 선을 그릴 때 사용할 프리팹
    private LineRenderer lr;
    private EdgeCollider2D collider2D;
    private List<Vector2> points = new List<Vector2>();
    private bool isDrawing = false;
    private bool isInitialized = false;

    // 추가된 변수
    private Rigidbody2D rb;

    void Start()
    {
        // 초기화가 필요한 경우 추가적인 설정을 이곳에서 진행합니다.
        if (PhotonNetwork.IsConnected)
        {
            OnPhotonConnected();
        }
        else
        {
            // Photon 네트워크에 연결되면 OnConnectedToMaster 호출
            PhotonNetwork.AddCallbackTarget(this);
        }

        // Rigidbody2D 컴포넌트를 가져옵니다.
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        OnPhotonConnected();
    }

    private void OnPhotonConnected()
    {
        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized || !PhotonNetwork.IsConnected)
        {
            return;
        }

        PhotonView photonView = GetComponent<PhotonView>();

        if (photonView.IsMine)
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
    }

    private void StartDrawing()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogError("PhotonNetwork is not connected. Cannot start drawing.");
            return;
        }

        if (rb != null)
        {
            rb.isKinematic = true; // 그림 그릴 때 Rigidbody2D를 비활성화
        }

        GameObject go = Instantiate(linePrefab);
        lr = go.GetComponent<LineRenderer>();
        collider2D = go.GetComponent<EdgeCollider2D>();
        points.Clear();
        Vector2 startPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        points.Add(startPos);
        lr.positionCount = 1;
        lr.SetPosition(0, startPos);
        isDrawing = true;
        photonView.RPC("StartDrawingRPC", RpcTarget.AllBuffered, startPos);
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
        photonView.RPC("AddPointRPC", RpcTarget.AllBuffered, pos);
    }

    private void StopDrawing()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogError("PhotonNetwork is not connected. Cannot stop drawing.");
            return;
        }

        // 추가된 Rigidbody2D를 넣을 게임오브젝트를 가져옵니다.
        if (lr != null)
        {
            // Rigidbody2D를 추가하고, 속성을 설정합니다.
            Rigidbody2D rb = lr.gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;  // Rigidbody2D의 bodyType을 Dynamic으로 설정합니다.

            // Collider2D의 물리적 상호작용을 활성화합니다.
            collider2D.isTrigger = false;
        }

        points.Clear();
        isDrawing = false;
        photonView.RPC("StopDrawingRPC", RpcTarget.AllBuffered);
    }

    [PunRPC]
    void AddPointRPC(Vector2 point)
    {
        if (!photonView.IsMine)
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
        if (!photonView.IsMine)
        {
            points.Clear();
        }
    }

    private void OnDestroy()
    {
        // 연결 해제 시 콜백을 제거합니다.
        PhotonNetwork.RemoveCallbackTarget(this);
    }
}
