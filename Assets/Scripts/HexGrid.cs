using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class HexGrid
{
    public int3 right = new int3(1, 0, -1);
    public int3 upRight = new int3(1, -1, 0);
    public int3 upLeft = new int3(0, -1, 1);
    public int3 left = new int3(-1, 0, 1);
    public int3 downLeft = new int3(-1, 1, 0);
    public int3 downRight = new int3(0, 1, -1);
	public int3[] directions;

    public int size = 0;
    public Dictionary<int3, Hex> hexes = new Dictionary<int3, Hex>();

    public HexGrid(int startSize)
    {
		size = startSize;

		//init directions array
		directions = new int3[6] { right, upRight, upLeft, left, downLeft, downRight };

        //create the hexes
        for(int q = -size; q <= size; q++)
        {
            for(int r = Mathf.Max(-size, -q - size); r <= Mathf.Min(size, -q + size); r++)
            {
                int s = -q - r;
                int3 pos = new int3(q, r, s);
                Hex newHex = new Hex(pos);
                bool isBorder = QRSDist(int3.zero, pos) == size;
				newHex.type = isBorder ? Hex.Type.Border : Hex.Type.Normal;
				hexes.Add(pos, newHex);
            }
        }
    }

    public Hex Get(int3 qrs)
    {
        Hex res = null;
        hexes.TryGetValue(qrs, out res);
        return res;
    }

    public int QRSDist(int3 a, int3 b)
    {
        return (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z)) / 2;
    }

    public Piece FindPieceById(long id)
    {
        foreach(Hex h in hexes.Values)
        {
            if(h.piece != null && h.piece.id == id)
            {
                return h.piece;
            }
        }
        return null;
    }
}

public class Hex
{
    public enum Type
    {
        Invalid,
        Normal,
        Border
    }

    public int3 pos;
    public Type type;
    public Piece piece;

    public Hex(int3 pos)
    {
        this.pos = pos;
    }
}

public class Piece
{
    public static long nextId = 0;

    public enum Type
    {
        Invalid,
        Normal,
        Corrupted
    }

    public long id;
    public int3 pos;
    public Type type;
    public int storedEnergy = 1;
    public bool dead = false;
    public PieceAnimation animation = new PieceAnimation();

    public Piece()
    {
        id = nextId++;
    }
}

public class PieceAnimation
{
    public enum Type
    {
        None,
        Move,
        Die,
        GrabbedByCorruption,
        Generated
    }

    public Type type = Type.None;
    public int3 fromPos;
}
