using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;
public class PlayerEffectName : MonoBehaviourPun
{
    [SerializeField] private GameObject effectIconPrefab; // Image 오브젝트 프리팹
    [SerializeField] private Sprite Titan;
    [SerializeField] private Sprite Invincibility;
    [SerializeField] private Sprite GodMode;
    [SerializeField] private Sprite EnemySmall;
    private PlayerEffectHandler effectHandler;
    private Dictionary<ItemEffectType, Sprite> effectIcons = new Dictionary<ItemEffectType, Sprite>();
    private List<GameObject> currentIcons = new List<GameObject>();
    void Start()
    {
        effectHandler = photonView.GetComponentInParent<PlayerEffectHandler>();
        effectIcons = new Dictionary<ItemEffectType, Sprite>
        {
            { ItemEffectType.TitanizeSelf, Titan },
            { ItemEffectType.NoInkConsumption, GodMode },
            { ItemEffectType.Invincibility, Invincibility },
            { ItemEffectType.EnemySmall, EnemySmall }
        };
    }
    void Update()
    {
        if (effectHandler == null || effectHandler.ActiveEffects == null)
        {
            ClearIcons();
            return;
        }
        List<ItemEffectType> activeEffects = effectHandler.ActiveEffects;
        // 1. 기존 아이콘 삭제
        ClearIcons();
        // 2. 효과 개수만큼 동적으로 생성
        foreach (var effect in activeEffects)
        {
            if (effectIcons.TryGetValue(effect, out Sprite iconSprite))
            {
                var iconGO = Instantiate(effectIconPrefab, transform); // transform=Horizontal Layout Group의 transform
                var img = iconGO.GetComponent<Image>();
                img.sprite = iconSprite;
                img.color = Color.white;
                currentIcons.Add(iconGO);
            }
        }
    }
    private void ClearIcons()
    {
        foreach (var go in currentIcons)
        {
            Destroy(go);
        }
        currentIcons.Clear();
    }
    void FixedUpdate()
    {
        var cam = Camera.main;
        if (cam != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }
}