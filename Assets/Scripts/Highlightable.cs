using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Highlightable : MonoBehaviour
{
    public Material activeMat;
    public Material inactiveMat;

    private bool highlighted = false;
    protected SpriteRenderer sr = null;
    protected Image image = null;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        image = GetComponent<Image>();
    }

    public bool IsHighlighted()
    {
        return highlighted;
    }

    public void SetHighlighted(bool val)
    {
        if(highlighted == val) { return; }
        highlighted = val;
        if (sr != null)
        {
            sr.material = highlighted ? activeMat : inactiveMat;
        }
        if(image != null)
        {
            image.material = highlighted ? activeMat : inactiveMat;
		}
    }
}
