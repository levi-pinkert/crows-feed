using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Letter : MonoBehaviour
{
	[TextArea(15, 20)]
	public string[] letterTexts;

    public Button backButton;
    public Button forwardButton;
    public Button exitButton;
    public TextMeshProUGUI tmp;

    private int currentLetter = 0;
    private int unlockedLetters = 1;

    private void Awake()
    {
        backButton.onClick.AddListener(() =>
        {
            GoToLetter(Mathf.Max(currentLetter - 1, 0));
			SoundManager.instance.PlaySound("turn_page");
		});

		forwardButton.onClick.AddListener(() =>
		{
			GoToLetter(Mathf.Min(currentLetter + 1, unlockedLetters - 1));
			SoundManager.instance.PlaySound("turn_page");
		});

        exitButton.onClick.AddListener(() =>
        {
			SoundManager.instance.PlaySound("press_button");
			Close();
        });

        GoToLetter(0);
	}

    public void Open()
    {
        gameObject.SetActive(true);
		SoundManager.instance.PlaySound("open_envelope");
	}

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void GoToLetter(int index)
    {
        currentLetter = index;
        backButton.gameObject.SetActive(currentLetter > 0);
        forwardButton.gameObject.SetActive(currentLetter < unlockedLetters - 1);
		tmp.text = letterTexts[currentLetter];
	}

    public void UnlockLetter(int num)
    {
        unlockedLetters = Mathf.Max(unlockedLetters, num + 1);
        GoToLetter(num);
    }
}
