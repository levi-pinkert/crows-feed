using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Counter : MonoBehaviour
{
    public const int blank = -1;
    public const int up = -2;
    public const int down = -3;

    public float openOffset;
    public float closeTime;
    public AnimationCurve closeAnim;
    public float openTime;
    public AnimationCurve openAnim;
    public GameObject shutter;
    public Sprite blankSprite;
    public Sprite upSprite;
    public Sprite downSprite;
    public SpriteRenderer backgroundSR;
    public TextMeshPro tmp;

    private float closeProg = 1f;
    private int currentDisplay = -1;
    private int goalDisplay = -1;

    private void Start()
    {
        UpdateSprite();
    }

    public void SetDisplay(int val)
    {
        goalDisplay = val;
    }

    void Update()
    {
        AnimationCurve animCurve;
        if(currentDisplay != goalDisplay)
        {
            closeProg = Mathf.Clamp01(closeProg + Time.deltaTime / closeTime);
            if(closeProg >= 1f - float.Epsilon)
            {
                UpdateSprite();
            }
            animCurve = closeAnim;
        }
        else
        {
			closeProg = Mathf.Clamp01(closeProg - Time.deltaTime / openTime);
            animCurve = openAnim;
		}

        shutter.transform.position = transform.position + Vector3.down * Mathf.Lerp(openOffset, 0f, animCurve.Evaluate(closeProg));
	}

    private void UpdateSprite()
    {
        if(goalDisplay >= 0)
        {
            tmp.text = goalDisplay.ToString();
            backgroundSR.sprite = blankSprite;
        }
        else if(goalDisplay == blank)
        {
            tmp.text = "";
            backgroundSR.sprite = blankSprite;
        }
		else if (goalDisplay == up)
		{
			tmp.text = "";
			backgroundSR.sprite = upSprite;
		}
		else if (goalDisplay == down)
		{
			tmp.text = "";
			backgroundSR.sprite = downSprite;
		}
		currentDisplay = goalDisplay;
	}
}
