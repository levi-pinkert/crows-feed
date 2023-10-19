using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class HexGridDisplay : MonoBehaviour
{
    [HideInInspector]
    public float S3 = Mathf.Sqrt(3.0f);

    [Header("Variables")]
    public float hexSize;

    [Header("References")]
    public GameObject hexTilePrefab;
    public GameObject borderHexTilePrefab;
    public GameObject pawnPrefab;

    [HideInInspector]
    public int3 highlightedPos;

    private GameManager gameManager;

    private Dictionary<long, Pawn> pawns = new Dictionary<long, Pawn>();
    private Dictionary<int3, HexPawn> hexPawns = new Dictionary<int3, HexPawn>();

	private void Start()
	{
        gameManager = FindObjectOfType<GameManager>();

		foreach (Hex hex in gameManager.grid.hexes.Values)
		{
			Vector3 worldPos = ToWorldPos(hex.pos);
            GameObject prefab = hex.type == Hex.Type.Border ? borderHexTilePrefab : hexTilePrefab;
			HexPawn newHexPawn = Instantiate(prefab, worldPos, Quaternion.identity).GetComponent<HexPawn>();
            hexPawns.Add(hex.pos, newHexPawn);
		}
	}

	public Vector3 ToWorldPos(int3 qrs)
    {
        float x = S3 * qrs.x + S3 / 2.0f * qrs.y;
        float y = 3.0f / 2.0f * qrs.y;
        return transform.position + Vector3.right * hexSize * x + Vector3.up * hexSize * y * -1.0f;
    }

    public Vector3 ToFloatQRS(Vector3 world)
    {
        Vector3 pos = world - transform.position;
        pos.y *= -1f;
        float q = (S3 / 3.0f * pos.x - 1.0f / 3.0f * pos.y) / hexSize;
		float r = 2.0f / 3.0f * pos.y / hexSize;
        return new Vector3(q, r, -q - r);
    }

    public int3 RoundToNearestHex(Vector3 floatQrs)
    {
        //create an answer that might be incorrect (no guarantee that q + r + s = 0)
        int3 qrs = new int3(Mathf.RoundToInt(floatQrs.x), Mathf.RoundToInt(floatQrs.y), Mathf.RoundToInt(floatQrs.z));

        //calculate error for each axis
        float qDiff = Mathf.Abs(floatQrs.x - qrs.x);
        float rDiff = Mathf.Abs(floatQrs.y - qrs.y);
        float sDiff = Mathf.Abs(floatQrs.z - qrs.z);

        //correct along the axis with the most error
        if(qDiff > rDiff && qDiff > sDiff)
        {
            qrs.x = -qrs.y - qrs.z;
        }
        else if(rDiff > sDiff)
        {
            qrs.y = -qrs.x - qrs.z;
        }
        else
        {
            qrs.z = -qrs.x - qrs.y;
        }

        return qrs;
	}

    public int3 ToQRS(Vector3 world)
    {
        return RoundToNearestHex(ToFloatQRS(world));
    }

    public void OnTurnStart()
    {
        //Collect all of the pieces on the board
        List<Piece> pieces = new List<Piece>();
		foreach (Hex h in gameManager.grid.hexes.Values)
        {
            if (h.piece != null)
            {
                pieces.Add(h.piece);
            }
        }
        foreach(Piece deadPiece in gameManager.recentlyDeadPieces)
        {
            pieces.Add(deadPiece);
        }

        //Update existing pawns and make new ones too
        Dictionary<long, Pawn> newPawns = new Dictionary<long, Pawn>();
        foreach(Piece piece in pieces)
        {
            //find an existing pawn, or make a new one
            Pawn pawn = null;
            pawns.TryGetValue(piece.id, out pawn);
            if(pawn == null)
            {
                Vector3 startPos = ToWorldPos(piece.pos);
                pawn = Instantiate(pawnPrefab, startPos, Quaternion.identity).GetComponent<Pawn>();
                pawn.Init(this, piece);
            }
            newPawns.Add(piece.id, pawn);

            //update the position, etc of the pawn
            pawn.OnTurnStart(piece);
        }

        //delete old pawns that weren't used
        List<Pawn> pawnsToDestroy = new List<Pawn>();
        foreach(var idPawnPair in pawns)
        {
            if (!newPawns.ContainsKey(idPawnPair.Key))
            {
                pawnsToDestroy.Add(idPawnPair.Value);
            }
        }
        pawns = newPawns;
        foreach(Pawn p in pawnsToDestroy)
        {
            Destroy(p.gameObject);
        }
    }

    public void UpdatePawns(float turnTimer, float turnLength)
    {
        foreach(Pawn p in pawns.Values)
        {
            p.PawnUpdate(turnTimer, turnLength);
        }
    }

    public void SetHighlightedHexes(List<int3> positions)
    {
        foreach(HexPawn p in hexPawns.Values)
        {
            p.SetHighlighted(false);
        }
        if(positions == null) { return; }
		foreach (int3 pos in positions)
		{
            hexPawns[pos].SetHighlighted(true);
		}
    }


}
