using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class DrawLine : MonoBehaviourPunCallbacks
{
    public GameObject linePrefab;  // ���� �׸� �� ����� ������
    private LineRenderer lr;
    private EdgeCollider2D collider2D;
    private List<Vector2> points = new List<Vector2>();
    private bool isDrawing = false;
    private bool isInitialized = false;

    // �߰��� ����
    private Rigidbody2D rb;

    void Start()
    {
        // �ʱ�ȭ�� �ʿ��� ��� �߰����� ������ �̰����� �����մϴ�.
        if (PhotonNetwork.IsConnected)
        {
            OnPhotonConnected();
        }
        else
        {
            // Photon ��Ʈ��ũ�� ����Ǹ� OnConnectedToMaster ȣ��
            PhotonNetwork.AddCallbackTarget(this);
        }

        // Rigidbody2D ������Ʈ�� �����ɴϴ�.
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
            rb.isKinematic = true; // �׸� �׸� �� Rigidbody2D�� ��Ȱ��ȭ
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

        // �߰��� Rigidbody2D�� ���� ���ӿ�����Ʈ�� �����ɴϴ�.
        if (lr != null)
        {
            // Rigidbody2D�� �߰��ϰ�, �Ӽ��� �����մϴ�.
            Rigidbody2D rb = lr.gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;  // Rigidbody2D�� bodyType�� Dynamic���� �����մϴ�.

            // Collider2D�� ������ ��ȣ�ۿ��� Ȱ��ȭ�մϴ�.
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
        // ���� ���� �� �ݹ��� �����մϴ�.
        PhotonNetwork.RemoveCallbackTarget(this);
    }
}
