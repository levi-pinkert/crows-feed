using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CounterManager : MonoBehaviour
{
    public Counter growKill;
    public Counter objective10s;
    public Counter objective1s;
    public Counter turns10s;
    public Counter turns1s;
    public Counter level10s;
    public Counter level1s;

    public Color baseColor;
    public Color warningColor;

    public void SetValues(bool grow, int objectiveNum, int turns, int level)
    {
        growKill.SetDisplay(grow ? Counter.up : Counter.down);

        List<int> objectiveNumDigits = GetReversedDigits(objectiveNum);
        objective10s.SetDisplay(objectiveNumDigits[1]);
        objective1s.SetDisplay(objectiveNumDigits[0]);

		List<int> turnsDigits = GetReversedDigits(turns);
		turns10s.SetDisplay(turnsDigits[1]);
		turns1s.SetDisplay(turnsDigits[0]);

        turns10s.tmp.color = (turns == 0) ? warningColor : baseColor;
        turns1s.tmp.color = (turns == 0) ? warningColor : baseColor;

		List<int> levelDigits = GetReversedDigits(level);
		level10s.SetDisplay(levelDigits[1]);
		level1s.SetDisplay(levelDigits[0]);
	}

    public static List<int> GetReversedDigits(int num)
    {
        List<int> res = new List<int>();
        while(num > 0)
        {
            res.Add(num % 10);
            num /= 10;
        }
        while(res.Count < 10)
        {
            res.Add(0);
        }
        return res;
    }
}
