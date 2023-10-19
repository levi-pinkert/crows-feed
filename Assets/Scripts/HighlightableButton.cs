using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HighlightableButton : Highlightable, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private void Awake()
    {
		sr = GetComponent<SpriteRenderer>();
		image = GetComponent<Image>();

	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		SetHighlighted(true);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		SetHighlighted(false);
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (IsHighlighted())
		{
			SetHighlighted(false);
		}
	}
}
