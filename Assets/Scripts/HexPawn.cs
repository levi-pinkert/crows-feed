using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class HexPawn : MonoBehaviour
{
    public int3 pos;
    [Header("References")]
    public GameObject highlightSprite;

    private bool highlighted = false;

    public void SetHighlighted(bool val)
    {
        highlighted = val;
        highlightSprite.SetActive(highlighted);
    }
}
