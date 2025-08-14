using UnityEngine;

public class Weapon_RPG : Weapon_Gun
{
    RPGDelayUI rpgUI;
    bool isMine = false;

    protected override void OnEnable()
    {
        base.OnEnable();

        if (isMine && rpgUI != null)
        {
            rpgUI.gameObject.SetActive(true);
            rpgUI.ResetFillAmount();
        }
    }

    protected override void Start()
    {
        base.Start();
        isMine = playerHandler.playerPhotonView.IsMine;
        if (isMine)
        {
            rpgUI = playerHandler.GetComponentInChildren<RPGDelayUI>(true);
            rpgUI.gameObject.SetActive(true);
            rpgUI.SetColor(teamID);
        }
    }
    protected override void FixedUpdate()
    {
        if (!playerHandler.photonView.IsMine) return;
        // 무기 쿨타임
        atkDelay_Left -= Time.fixedDeltaTime;
        // 무기 발사 가능하게 전환 및 알림
        isAttackable = CrosshairSwitcher.Instance.IsShootable = (atkDelay_Left < 0 && attackInkCost <= playerHandler.Status.Ink) && !playerHandler.isTryGrapple;

        // 무기 발사 조작 + 발사 가능 상태 >> 발사
        if (playerHandler.isAttack)
        {
            if (isAttackable)
            {
                // 카메라, 발사방향 동기화
                UpdateCameraWeaponSync();
                InkEvents.ConsumeInk(attackInkCost);
                Attack();
            }
            else if (warningSoundPlayDelay < 0)
            {
                SoundManager.Instance.PlaySfx(ClipIndex_SFX.mwfx_Warning, volumeScale);
                warningSoundPlayDelay = warningSoundPlayDelay_Played;
            }
        }
        else
        {
            warningSoundPlayDelay -= Time.fixedDeltaTime;
            //일단 임시 방편
            if (!playerHandler.isAttack)
            {
                gunModel.rotation = Quaternion.LookRotation(playerHandler.transform.forward);
            }
        }
    }

    private void OnDisable()
    {
        if (isMine)
            rpgUI.gameObject.SetActive(false);
    }

    protected override void SetDelay(float atkDelay)
    {
        if (isMine && rpgUI.gameObject.activeSelf)
        {
            base.SetDelay(atkDelay);
            // RPG는 공격 후 딜레이가 길기에 이를 표시할 UI 출현
            rpgUI.ShowDelayUI(atkDelay);
            // + 딜레이를 표시하기 위한 발사 불가 크로스헤어 표시
            CrosshairSwitcher.Instance.IsShootable = false;
        }
    }
}
