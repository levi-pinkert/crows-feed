using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

public class Pawn : MonoBehaviour
{
    public AnimationCurve movementAnim;
    public GameObject corruptionObject;
    public TextMeshPro corruptionText;
    public Sprite normalSprite;
    public Sprite corruptedSprite;
    public Sprite destroySpriteOne;
    public Sprite destroySpriteTwo;
    public AnimationCurve grabbedMovementAnim;
    public AnimationCurve grabbedScaleAnim;
    public AnimationCurve generatedScaleAnim;
    public AnimationCurve dieOpacityAnim;

    [Header("Runtime Variables")]
    public HexGridDisplay gridDisplay;
    public long pieceId;
    public Piece piece;

    private Highlightable highlightable;
    private SpriteRenderer sr;
    private Vector3 baseScale;

    private void Awake()
    {
        highlightable = GetComponent<Highlightable>();
        sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
    }

    public void Init(HexGridDisplay gD, Piece piece)
    {
        gridDisplay = gD;
        pieceId = piece.id;
        this.piece = piece;

        gameObject.name = $"Pawn ({pieceId})";
    }

    public void OnTurnStart(Piece piece)
    {
        this.piece = piece;
	}

    public void PawnUpdate(float turnTimer, float turnLength)
    {
        //Either move slowly or instantly, depending on stuff
        if(piece.animation.type == PieceAnimation.Type.Move || piece.animation.type == PieceAnimation.Type.Die)
        {
            Vector3 oldPos = gridDisplay.ToWorldPos(piece.animation.fromPos);
            Vector3 newPos = gridDisplay.ToWorldPos(piece.pos);
            transform.position = Vector3.Lerp(oldPos, newPos, movementAnim.Evaluate(turnTimer / turnLength));
        }
        else if(piece.animation.type == PieceAnimation.Type.GrabbedByCorruption)
        {
			Vector3 oldPos = gridDisplay.ToWorldPos(piece.animation.fromPos);
			Vector3 newPos = gridDisplay.ToWorldPos(piece.pos);
			transform.position = Vector3.Lerp(oldPos, newPos, grabbedMovementAnim.Evaluate(turnTimer / turnLength));
		}
        else
        {
			transform.position = gridDisplay.ToWorldPos(piece.pos);
		}

        //Scale animation?
        if(piece.animation.type == PieceAnimation.Type.GrabbedByCorruption)
        {
			transform.localScale = baseScale * grabbedScaleAnim.Evaluate(turnTimer / turnLength);
		}
        else if(piece.animation.type == PieceAnimation.Type.Generated)
        {
			transform.localScale = baseScale * generatedScaleAnim.Evaluate(turnTimer / turnLength);
		}
        else
        {
            transform.localScale = baseScale;
        }

        //Opacity animation awooga
        if(piece.animation.type == PieceAnimation.Type.Die)
        {
            sr.color = new Color(1f, 1f, 1f, dieOpacityAnim.Evaluate(turnTimer / turnLength));
        }
        else
        {
            sr.color = Color.white;
        }

        //set sprite
        if (piece.animation.type == PieceAnimation.Type.Die)
        {
            float prog = turnTimer / turnLength;
            if(prog < .4)
            {
				sr.sprite = destroySpriteOne;
			}
            else
            {
				sr.sprite = destroySpriteTwo;
			}
        }
        else
        {
			sr.sprite = piece.type == Piece.Type.Corrupted ? corruptedSprite : normalSprite;
		}

		//Activate or deactivate corruption text
		corruptionObject.SetActive(piece.type == Piece.Type.Corrupted && piece.animation.type != PieceAnimation.Type.Die);
        corruptionText.text = piece.storedEnergy.ToString();
        

        //Highlight if the player is hovering over this
        highlightable.SetHighlighted(!piece.dead && math.all(gridDisplay.highlightedPos == piece.pos));
    }
}
