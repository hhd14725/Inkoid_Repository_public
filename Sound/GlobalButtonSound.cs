using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GlobalButtonSound : MonoBehaviour
{
    [SerializeField] private ClipIndex_SFX defaultSfx = ClipIndex_SFX.mwfx_Click;
    [SerializeField] private float defaultVolume = 1f;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        InjectButtonSounds();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += (_, __) => InjectButtonSounds();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= (_, __) => InjectButtonSounds();
    }

    private void InjectButtonSounds()
    {
        var buttons = FindObjectsOfType<Button>(true);

        foreach (var btn in buttons)
        {
            // 기존 리스너 제거 없이 추가 (중복 방지할 거면 RemoveListener 필요)
            btn.onClick.AddListener(() =>
            {
                var tag = btn.GetComponent<ButtonSoundTag>();
                var sfx = tag != null ? tag.customSfx : defaultSfx;
                var vol = tag != null ? tag.volume : defaultVolume;
                SoundManager.Instance.PlaySfx(sfx, vol);
            });
        }
    }
}
