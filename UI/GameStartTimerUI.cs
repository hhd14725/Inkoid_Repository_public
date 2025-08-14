
using System.Collections.Generic;
using UnityEngine;
using Image = UnityEngine.UI.Image;

public class GameStartTimerUI : MonoBehaviour
{
    public Image currentImage;
    [SerializeField] public List<Sprite> NumSprites;

    public void Start()
    {
        currentImage = GetComponentInChildren<Image>();
        SetNumImage(NumSprites.Count-1);
    }

    public void SetNumImage(int Num)
    {
        currentImage.sprite = NumSprites[Num];
    }
}
