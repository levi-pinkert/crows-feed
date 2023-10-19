using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	[Header("Variables")]
	public int gridSize;
    public float turnLength;
    public AnimationCurve growGoals;
    public AnimationCurve growTurns;
    public AnimationCurve killGoals;
    public AnimationCurve killTurns;
    public float letterDelay;

	[Header("References")]
    public Letter letterUi;
    public CounterManager counter;

	[HideInInspector]
    public HexGrid grid;

    [HideInInspector]
    public List<Piece> recentlyDeadPieces = new List<Piece>();

    [HideInInspector]
    public HexGridDisplay gridDisplay;

    [HideInInspector]
    public Piece selectedPiece = null;

    [HideInInspector]
    public bool crystalsAreAvailable = false;

    private float turnTimer = 0f;
    private int queuedMoves = 0;
    private long queuedMovePieceId = -1;
    private int3 queuedMoveDirection = int3.zero;

    private bool isCreateQueued = false;
    private int3 queuedCreatePos = int3.zero;

    private bool isCorruptQueued = false;
    private int3 queuedCorruptPos = int3.zero;

    private int queuedReleases = 0;
    private int3 queuedReleasePos = int3.zero;
    private int3 queuedReleaseDir = int3.zero;

    private int level = 1;
    private int phase = 0;
    private int growGoal = 10;
    private int killGoal = 3;
    private int levelTurnsLeft = 10;

    private int lettersRecieved = 0;

    private bool playDestroyPiece = false;
    private bool playGeneratePiece = false;
    private bool playCorruptPiece = false;

	private void Awake()
    {
        //find references
        gridDisplay = FindObjectOfType<HexGridDisplay>();

        //create grid
        grid = new HexGrid(gridSize);

        //start level
        StartLevel(1);
        UpdateStatusText();
    }

    public void GameInputClick(Vector3 worldPos)
    {
        //check that the player currently can make inputs
        if (!PlayerHasControl()) { return; }

        //check that they clicked on the board
        int3 gamePos = gridDisplay.ToQRS(worldPos);
		Hex selection = grid.Get(gamePos);
        if(selection == null) {
            if(selectedPiece != null) {
                selectedPiece = null;
                gridDisplay.SetHighlightedHexes(null);
				SoundManager.instance.PlaySound("deselect_piece");
			}
            return;
        }

        //either select a piece or move the previously selected piece
        if (selectedPiece == null)
        {
            if(selection.piece != null)
            {
				SoundManager.instance.PlaySound("select_piece");
				selectedPiece = selection.piece;
                gridDisplay.SetHighlightedHexes(GetValidMoves(selectedPiece));
            }
        }
        else
        {
            List<int3> validMoves = GetValidMoves(selectedPiece);
            if (validMoves.Contains(gamePos))
            {
                //figure out the direction and magnitude of the click
                int moveDist = grid.QRSDist(selectedPiece.pos, gamePos);
                int3 moveDir = (gamePos - selectedPiece.pos) / moveDist;

                //either queue a move or queue a release...
                if (selectedPiece.type == Piece.Type.Normal)
                {
					levelTurnsLeft--;
					queuedMoves = moveDist;
                    queuedMoveDirection = moveDir;
                    queuedMovePieceId = selectedPiece.id;
                }
                else if(selectedPiece.type == Piece.Type.Corrupted)
                {
					levelTurnsLeft--;
					queuedReleases = selectedPiece.storedEnergy;
                    queuedReleaseDir = moveDir;
                    queuedReleasePos = selectedPiece.pos;
                }

                //deselect the piece
				selectedPiece = null;
				gridDisplay.SetHighlightedHexes(null);
			}
            else
            {
                selectedPiece = null;
				gridDisplay.SetHighlightedHexes(null);
				SoundManager.instance.PlaySound("deselect_piece");
			}
        }
    }

    public void RestartButtonPressed()
    {
        //Delete current pieces
        ClearPieceAnimations();
		foreach(Hex h in grid.hexes.Values)
        {
            if(h.piece != null)
            {
                DestroyPiece(h);
            }
        }
		turnTimer = 0f;
		gridDisplay.OnTurnStart();
        

		//start level
		StartLevel(1);
		UpdateStatusText();

		
	}

    public void GameInputAddPiece(Vector3 worldPos)
    {
        if (PlayerHasControl())
        {
			int3 gamePos = gridDisplay.ToQRS(worldPos);
			Hex selection = grid.Get(gamePos);
			if (selection == null || selection.piece != null) { return; }
			levelTurnsLeft--;
			isCreateQueued = true;
			queuedCreatePos = gamePos;
		}
    }

    public void GameInputCorruptPiece(Vector3 worldPos)
    {
        if (PlayerHasControl())
        {
			int3 gamePos = gridDisplay.ToQRS(worldPos);
			Hex selection = grid.Get(gamePos);
			if (selection == null) { return; }
			Piece selectedPiece = selection.piece;
            if (selectedPiece != null && selectedPiece.type == Piece.Type.Normal)
            {
                levelTurnsLeft--;
                isCorruptQueued = true;
                queuedCorruptPos = gamePos;
            }
        }
    }

    public bool PlayerHasControl()
    {
        return levelTurnsLeft > 0 && queuedMoves == 0 && !isCorruptQueued && !isCreateQueued && queuedReleases == 0 && turnTimer >= turnLength;
    }

    private Piece CreatePiece(int3 pos)
    {
        Hex hex = grid.Get(pos);
        Piece newPiece = null;
        if(hex != null && hex.type == Hex.Type.Normal && hex.piece == null)
        {
			newPiece = new Piece();
			newPiece.pos = pos;
            newPiece.animation.fromPos = pos;
			newPiece.type = Piece.Type.Normal;
            hex.piece = newPiece;
		}
        gridDisplay.OnTurnStart();
        return newPiece;
    }

    private void MovePiece(Piece p, int3 dir)
    {
        Hex fromHex = grid.Get(p.pos);
        int3 newPos = p.pos + dir;
        Hex toHex = grid.Get(newPos);
        if(fromHex == null || toHex == null || toHex.piece != null) { return; }
        toHex.piece = p;
        fromHex.piece = null;
		p.pos = newPos;

        p.animation.type = PieceAnimation.Type.Move;
        p.animation.fromPos = fromHex.pos;
    }

    private List<int3> GetValidMoves(Piece piece)
    {
        List<int3> res = new List<int3>();
        for(int i = 0; i < grid.directions.Length; i++)
        {
            int3 pos = piece.pos;
            while (true)
            {
                pos += grid.directions[i];
                if(grid.Get(pos) != null)
                {
                    res.Add(pos);
                }
                else
                {
                    break;
                }
            }
        }
        return res;
    }

    private void Update()
    {
        //start a new turn mayhaps?
        turnTimer += Time.deltaTime;
        bool startedNewTurn = false;
        if(turnTimer >= turnLength)
        {
            //check if we have a new turn we want to start
            if(queuedMoves > 0)
            {
                //we want to move a piece!
                if (TryMove(grid.FindPieceById(queuedMovePieceId), queuedMoveDirection))
                {
                    queuedMoves--;
                    startedNewTurn = true;
                    SoundManager.instance.PlaySound("move_piece");
                }
                else
                {
                    queuedMoves = 0;
                }
            }

			if (!startedNewTurn && queuedReleases > 0)
			{
				//we're in the process of pushing all of the stuff out of a corrupted piece
				Hex baseHex = grid.Get(queuedReleasePos);
				ClearPieceAnimations();
				if (baseHex.piece != null)
				{
					if (baseHex.piece.type == Piece.Type.Corrupted)
					{
						baseHex.piece = null;
					}
					else
					{
						TryMove(baseHex.piece, queuedReleaseDir);
						SoundManager.instance.PlaySound("move_piece");
					}
				}
				Piece p = CreatePiece(queuedReleasePos);
				SoundManager.instance.PlaySound("place_piece");
				p.animation.type = PieceAnimation.Type.Generated;
				queuedReleases--;
				startedNewTurn = true;
			}

			if (!startedNewTurn && isCreateQueued)
            {
                //we want to create a new piece
                isCreateQueued = false;
                ClearPieceAnimations();
                CreatePiece(queuedCreatePos);
                queuedCreatePos = int3.zero;
                startedNewTurn = true;
				SoundManager.instance.PlaySound("place_piece");
			}

            if(!startedNewTurn && isCorruptQueued)
            {
				//we want to corrupt a piece
				isCorruptQueued = false;
				ClearPieceAnimations();
                CorruptPiece(queuedCorruptPos);
				queuedCorruptPos = int3.zero;
				startedNewTurn = true;
                playCorruptPiece = true;
			}
        }
		if (startedNewTurn)
		{
			DestroyBorderPieces();
			EnforceCorruption();
			FillSurroundedRegions();
            DoObjectiveLogic();
			turnTimer = 0f;
			gridDisplay.OnTurnStart();
		}

        //update the pawns
        gridDisplay.UpdatePawns(turnTimer, turnLength);

        //play sounds that we queued up
        if (playDestroyPiece)
        {
			SoundManager.instance.PlaySound("destroy_piece");
            playDestroyPiece = false;
		}
        if (playCorruptPiece)
        {
			SoundManager.instance.PlaySound("corrupt_piece");
            playCorruptPiece = false;
		}
        if (playGeneratePiece)
        {
            SoundManager.instance.PlaySound("generate_piece");
            playGeneratePiece = false;
        }
	}

    private bool TryMove(Piece pusher, int3 dir)
    {
		//Check if piece exists
		if (pusher == null) { return false; }

        //try to move forwards and remember pieces to push as we go
        List<Piece> piecesToMove = new List<Piece>();
        piecesToMove.Add(pusher);
        int maxAttempts = 100;
        for(int i = 1; i < maxAttempts; i++)
        {
            int3 newPos = pusher.pos + dir * i;
            Hex h = grid.Get(newPos);
            if(h == null || i == maxAttempts)
            {
                //we've run out of space, which shouldn't be possible, so just fail
                return false;
            }
            else if(h.piece == null)
            {
                //we can move!
                break;
            }
            else
            {
                //we might be able to push this piece, but we need to keep searching forward
                piecesToMove.Add(h.piece);
            }
        }

        //since we're about to queue animations, clear all of the old ones
        ClearPieceAnimations();

        //make the moves
		for(int i = piecesToMove.Count - 1; i >= 0; i--)
        {
            MovePiece(piecesToMove[i], dir);
        }

        //return that we succesfully moved
		return true;
	}

    private void ClearPieceAnimations()
    {
        recentlyDeadPieces.Clear();
        foreach(Hex h in grid.hexes.Values)
        {
            if(h.piece != null)
            {
                h.piece.animation = new PieceAnimation();
                h.piece.animation.fromPos = h.pos;
            }
        }
    }

    private void DestroyBorderPieces()
    {
        foreach(Hex h in grid.hexes.Values)
        {
            if(h.type == Hex.Type.Border && h.piece != null)
            {
                DestroyPiece(h);
            }
        }
    }

    private void DestroyPiece(Hex h)
    {
		recentlyDeadPieces.Add(h.piece);
		h.piece.animation.type = PieceAnimation.Type.Die;
        h.piece.dead = true;
		h.piece = null;
        playDestroyPiece = true;
	}

    private List<List<int3>> GetRegions(bool getEmptyRegions = true)
    {
        //compile a list of empty board spots
        HashSet<int3> openSet = new HashSet<int3>();
        foreach(Hex h in grid.hexes.Values)
        {
            if (getEmptyRegions)
            {
                if (h.piece == null)
                {
                    openSet.Add(h.pos);
                }
            }
            else
            {
                if(h.piece != null)
                {
                    openSet.Add(h.pos);
                }
            }
        }

        //bfs the whole dealio
        List<List<int3>> boardSegments = new List<List<int3>>();
        while(openSet.Count > 0)
        {
            Queue<int3> placesToExplore = new Queue<int3>();
            List<int3> currentSegment = new List<int3>();

            //Get a random element to start at
            var enumerator = openSet.GetEnumerator();
            enumerator.MoveNext();
			int3 startPos = enumerator.Current;

            //remove it from open set
            openSet.Remove(startPos);
            placesToExplore.Enqueue(startPos);
            currentSegment.Add(startPos);

            while(placesToExplore.Count > 0)
            {
                int3 explorePos = placesToExplore.Dequeue();

                //iterate over neighbors, and maybe explore them
                foreach(int3 dir in grid.directions)
                {
                    int3 newPos = explorePos + dir;
                    if (openSet.Contains(newPos))
                    {
                        openSet.Remove(newPos);
						placesToExplore.Enqueue(newPos);
						currentSegment.Add(newPos);
					}
                }
            }

            boardSegments.Add(currentSegment);
        }
        return boardSegments;

    }

    public void FillSurroundedRegions()
    {
        List<List<int3>> regions = GetRegions(true);
        foreach(List<int3> region in regions)
        {
            //determine if region is surrounded
            bool surrounded = true;
            foreach(int3 pos in region)
            {
                if(grid.Get(pos).type == Hex.Type.Border)
                {
                    surrounded = false;
                    break;
                }
            }

            //if it was surrounded, fill it in
            if (surrounded)
            {
                playGeneratePiece = true;
                foreach(int3 pos in region)
                {
                    Piece p = CreatePiece(pos);
                    p.animation.type = PieceAnimation.Type.Generated;
                }
            }
        }

    }

    public void CorruptPiece(int3 pos)
    {
        Hex h = grid.Get(pos);
        if(h.piece != null && h.piece.type == Piece.Type.Normal)
        {
            h.piece.type = Piece.Type.Corrupted;
        }
    }

    public void EnforceCorruption()
    {
        List<List<int3>> regions = GetRegions(false);
        foreach(List<int3> region in regions)
        {
            //find if there is a corruptor in this region
            Piece corruptor = null;
            foreach(int3 pos in region)
            {
                Hex h = grid.Get(pos);
                if(h != null && h.piece != null && h.piece.type == Piece.Type.Corrupted)
                {
                    corruptor = h.piece;
                    break;
                }
            }

            //destroy the pieces!
            if (corruptor != null)
            {
                foreach(int3 pos in region)
                {
                    Hex h = grid.Get(pos);
                    if(h.piece != null && h.piece.type != Piece.Type.Corrupted)
                    {
                        recentlyDeadPieces.Add(h.piece);
                        h.piece.pos = corruptor.pos;
                        h.piece.animation.type = PieceAnimation.Type.GrabbedByCorruption;
                        h.piece = null;
                        corruptor.storedEnergy++;
						playCorruptPiece = true;
					}
                }
            }
        }
    }

    public int CountPieces()
    {
        int pieces = 0;
        foreach(Hex h in grid.hexes.Values)
        {
            if(h.piece != null)
            {
                pieces++;
            }
        }
        return pieces;
    }

    private void DoObjectiveLogic()
    {
        //progress?
        int livingPieces = CountPieces();
        if(phase == 0)
        {
            if(livingPieces >= growGoal)
            {
                phase = 1;
				levelTurnsLeft = Mathf.RoundToInt(killTurns.Evaluate(level));
                UpdateLetter();
				SoundManager.instance.PlaySound("turn_victory");
			}
        }
        else if(phase == 1)
        {
            if(livingPieces <= killGoal)
            {
                StartLevel(level + 1);
				SoundManager.instance.PlaySound("turn_victory");
			}
        }

		//update status text
		UpdateStatusText();
    }

    private void StartLevel(int newLevelNum)
    {
        level = newLevelNum;
        phase = 0;
        growGoal = Mathf.RoundToInt(growGoals.Evaluate(level));
        killGoal = Mathf.RoundToInt(killGoals.Evaluate(level)); ;
        levelTurnsLeft = Mathf.RoundToInt(growTurns.Evaluate(level));
        UpdateLetter();
    }

    private void UpdateLetter()
    {
		//maybe recieve a letter?
		if (level >= 1 && lettersRecieved == 0)
		{
			GetLetter(0);
		}
		if (level >= 1 && phase >= 1 && lettersRecieved == 1)
		{
			GetLetter(1);
		}
		if (level >= 2 && lettersRecieved == 2)
		{
			GetLetter(2);
		}
		if (level >= 2 && phase >= 1 && lettersRecieved == 3)
		{
			GetLetter(3);
		}
        if(level >= 3 && phase >= 1 && lettersRecieved == 4)
        {
            GetLetter(4);
        }
        if(level >= 5 && lettersRecieved == 5)
        {
            GetLetter(5);
        }
	}

    private void GetLetter(int i)
    {
		lettersRecieved = Mathf.Max(lettersRecieved, i + 1);
		letterUi.UnlockLetter(i);
        if (i == 0)
        {
            letterUi.Open();
        }
        else
        {
            StartCoroutine(OpenLetterAfterDelay(letterDelay));
        }
	}

    private IEnumerator OpenLetterAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        letterUi.Open();
        if(lettersRecieved >= 5 && !crystalsAreAvailable)
        {
			crystalsAreAvailable = true;
		}
    }

    private void UpdateStatusText()
    {
		int livingPieces = CountPieces();
        int displayLevel = (level * 2) - 1 + phase;
		if (phase == 0)
		{
			counter.SetValues(true, growGoal - livingPieces, levelTurnsLeft, displayLevel);
		}
		else if (phase == 1)
		{
			counter.SetValues(false, livingPieces - killGoal, levelTurnsLeft, displayLevel);
		}

        
	}
}
