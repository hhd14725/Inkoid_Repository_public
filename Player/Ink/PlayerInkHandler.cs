using Photon.Pun;
using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInkHandler : MonoBehaviourPun, IPunObservable
{
    private PlayerHandler playerHandler;

    private float currentInkRecoveryRate;
    private float defaultInkRecoveryRate = 3f;
    private float BoostInkRecoveryRate = 60f;

    // 실제 잉크 변화 이벤트
    public event Action<float> OnInkChanged;

    //실험용
    public Image inkGage;

    //실험용
    public GameObject inkGageObject;

    public float Ink
    {
        get { return playerHandler.Status.Ink; }
        set
        {
            playerHandler.Status.Ink = value;
            OnInkChanged?.Invoke(playerHandler.Status.Ink);
        }
    }

    public float maximumInk
    {
        get { return playerHandler.Status.MaximumInk; }
        set { playerHandler.Status.MaximumInk = value; }
    }


    private void Awake()
    {
        playerHandler = GetComponent<PlayerHandler>();
    }

    private void OnEnable()
    {
        InkEvents.OnInkConsumed += ConsumeInk;
        InkEvents.OnInkRestored += RestoreInk;
        InkEvents.OnBoostInkRecovery += BoostInkRecovery;
        InkEvents.OnResetInkRecovery += ResetInkRecovery;
        InkEvents.OnFullInk += fullInk;
        InkEvents.OnItemInkBoostRecovery += ItemInkRecovery;
    }

    private void OnDisable()
    {
        InkEvents.OnInkConsumed -= ConsumeInk;
        InkEvents.OnInkRestored -= RestoreInk;
        InkEvents.OnBoostInkRecovery -= BoostInkRecovery;
        InkEvents.OnResetInkRecovery -= ResetInkRecovery;
        InkEvents.OnFullInk -= fullInk;
        InkEvents.OnItemInkBoostRecovery -= ItemInkRecovery;
    }

    private void Start()
    {
        if (!photonView.IsMine) return;
        currentInkRecoveryRate = defaultInkRecoveryRate;
        inkGageObject.SetActive(true);

        OnInkChanged?.Invoke(Ink);
    }

    private void Update()
    {
        if (playerHandler.playerPhotonView.Owner != PhotonNetwork.LocalPlayer) return;
        if (!photonView.IsMine || !playerHandler) return;

        UpdateInkGage();

        if (!playerHandler.isAttack && !playerHandler.isDash)
        {
            RecoverInk();
        }
    }

    private void RecoverInk()
    {
        // 시간 기반 회복
        Ink += currentInkRecoveryRate * Time.deltaTime;

        // Clamp 처리
        playerHandler.Status.Ink = Mathf.Clamp(Ink, 0f, maximumInk);
    }

    public void ConsumeInk(float amount)
    {
        // 오브젝트가 파괴되었는지 확인하여 MissingReferenceException 방지
        if (this == null)
        {
            //Debug.LogWarning("PlayerInkHandler object has been destroyed, cannot consume ink.");
            return;
        }

        if (!photonView.IsMine) return;
        Ink = Mathf.Clamp(Ink - amount, 0f, maximumInk);
    }

    private void RestoreInk(float amount)
    {
        if (!photonView.IsMine) return;
        Ink = Mathf.Clamp(Ink + amount, 0f, maximumInk);
    }

    private void BoostInkRecovery()
    {
        if (!photonView.IsMine) return;
        currentInkRecoveryRate = BoostInkRecoveryRate;
    }

    public void ItemInkRecovery(float amount)
    {
        if (!photonView.IsMine) return;
        currentInkRecoveryRate = amount;
    }

    public void ResetInkRecovery()
    {
        if (!photonView.IsMine) return;
        currentInkRecoveryRate = defaultInkRecoveryRate;
    }

    //실험용
    private void fullInk()
    {
        if (!photonView.IsMine) return;
        Ink = maximumInk;
    }

    private void UpdateInkGage()
    {
        Vector3 scale = inkGage.transform.localScale;
        scale.x = Ink / maximumInk;
        inkGage.transform.localScale = scale;
    }

    public void SetInkBarColor(Color color)
    {
        inkGage.color = color;
    }


    //내 잉크량 상대방이 알아야했나요?(창민)
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 내 로컬 잉크값을 다른 클라이언트로 보낸다
            stream.SendNext(Ink);
        }
        else
        {
            // 다른 클라이언트에서 보내온 잉크값을 받아서 적용
            float received = (float)stream.ReceiveNext();
            Ink = Mathf.Clamp(received, 0f, maximumInk);
        }
    }
}