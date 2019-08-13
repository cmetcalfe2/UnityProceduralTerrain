using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MathUtilities
{
	public static int Pow(int i, int p)
	{
		int answer = 1;

		for (int j = 0; j < p; j++)
		{
			answer *= i;
		}

		return answer;
	}
}
