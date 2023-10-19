using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameMouseInput : MonoBehaviour, IPointerDownHandler, IPointerMoveHandler, IPointerUpHandler
{
	[System.Serializable]
	public class CustomButton
	{
		public Transform pos;
		public float radius;
		public Highlightable highlightable;
	}

	public CustomButton pieceBag;
	public GameObject dragPiece;

	public CustomButton crystalBag;
	public GameObject dragCorruptor;

	public CustomButton restartButton;
	public CustomButton mailButton;
	public Letter letterUi;

	private GameManager gameManager;
    private bool isDragging;
	private Piece.Type draggingType;

    private void Awake()
    {
		gameManager = FindObjectOfType<GameManager>();
	}

	public void OnPointerDown(PointerEventData eventData)
    {
		//Calculate the world position of the click
		Vector3 clickWorldPos = GetClickWorldPos(eventData);

		//if they're selecting, then  go straight to them
		if(gameManager.selectedPiece != null)
		{
			gameManager.GameInputClick(clickWorldPos);
			return;
		}

		//check if they're picking up a piece
		if (isDragging) { return; }
		if(Vector3.Distance(clickWorldPos, pieceBag.pos.position) < pieceBag.radius)
		{
			isDragging = true;
			draggingType = Piece.Type.Normal;
			dragPiece.transform.position = new Vector3(clickWorldPos.x, clickWorldPos.y);
			dragPiece.SetActive(true);
			SoundManager.instance.PlaySound("pickup_piece");
		}
		else if (gameManager.crystalsAreAvailable && Vector3.Distance(clickWorldPos, crystalBag.pos.position) < crystalBag.radius)
		{
			isDragging = true;
			draggingType = Piece.Type.Corrupted;
			dragCorruptor.transform.position = new Vector3(clickWorldPos.x, clickWorldPos.y);
			dragCorruptor.SetActive(true);
			SoundManager.instance.PlaySound("get_crystal");
		}
		else if (Vector3.Distance(clickWorldPos, mailButton.pos.position) < mailButton.radius)
		{
			//open mail
			letterUi.Open();
		}
		else if (Vector3.Distance(clickWorldPos, restartButton.pos.position) < restartButton.radius)
		{
			//restart game
			gameManager.RestartButtonPressed();
			SoundManager.instance.PlaySound("press_button");
		}

		//inform the game of the input
		if (!isDragging)
		{
			gameManager.GameInputClick(clickWorldPos);
		}

		//update highlighting
		UpdateHighlighting(clickWorldPos);
	}

    public void OnPointerMove(PointerEventData eventData)
    {
		Vector3 clickWorldPos = GetClickWorldPos(eventData);

		//update highlighting
		UpdateHighlighting(clickWorldPos);

		//update dragging
		if (isDragging)
		{
			if (draggingType == Piece.Type.Normal)
			{
				dragPiece.transform.position = clickWorldPos;
			}
			else if(draggingType == Piece.Type.Corrupted)
			{
				dragCorruptor.transform.position = clickWorldPos;
			}
		}
        
    }

	private void UpdateHighlighting(Vector3 clickWorldPos)
	{
		CustomButton[] bs = { pieceBag, crystalBag, mailButton, restartButton };
		bool highlightEnabled = !isDragging && gameManager.selectedPiece == null;
		foreach (CustomButton b in bs)
		{
			b.highlightable.SetHighlighted(highlightEnabled && Vector3.Distance(clickWorldPos, b.pos.position) < b.radius);
		}

		if (highlightEnabled && gameManager.PlayerHasControl())
		{
			gameManager.gridDisplay.highlightedPos = gameManager.gridDisplay.ToQRS(clickWorldPos);
		}
		else
		{
			gameManager.gridDisplay.highlightedPos = new int3(100, 100, 100);
		}
	}

    public void OnPointerUp(PointerEventData eventData)
    {
		if (isDragging)
		{
			isDragging = false;
			Vector3 clickWorldPos = GetClickWorldPos(eventData);
			if(draggingType == Piece.Type.Normal)
			{
				dragPiece.SetActive(false);
				gameManager.GameInputAddPiece(clickWorldPos);
			}
			else if(draggingType == Piece.Type.Corrupted)
			{
				dragCorruptor.SetActive(false);
				gameManager.GameInputCorruptPiece(clickWorldPos);
			}

			UpdateHighlighting(clickWorldPos);
		}


    }

	private Vector3 GetClickWorldPos(PointerEventData eventData)
	{
		Vector2 clickScreenPos = eventData.position;
		Ray clickRay = Camera.main.ScreenPointToRay(new Vector3(clickScreenPos.x, clickScreenPos.y));
		Plane gamePlane = new Plane(Vector3.back, 0f);
		Vector3 clickWorldPos = Vector3.zero;
		float rayEnterLength = 0f;
		if (gamePlane.Raycast(clickRay, out rayEnterLength))
		{
			clickWorldPos = clickRay.GetPoint(rayEnterLength);
		}
		return clickWorldPos;
	}

	private void Update()
	{
		crystalBag.highlightable.gameObject.SetActive(gameManager.crystalsAreAvailable);
	}
}
