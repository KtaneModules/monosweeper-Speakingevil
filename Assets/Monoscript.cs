using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Monoscript : MonoBehaviour {

    public KMAudio Audio;
    public KMBombModule module;
    public List<KMSelectable> cells;
    public Transform[] cpush;
    public Renderer[] crends;
    public Material[] wb;
    public TextMesh[] digits;

    private int count;
    private bool[] press = new bool[49];
    private bool[] pressed = new bool[49];

    private static int moduleIDCounter;
    private int moduleID;
    private bool moduleSolved;

    private void Awake()
    {
        moduleID = ++moduleIDCounter;
        count = Random.Range(16, 34);
        int[] rarr = Enumerable.Range(0, 49).ToArray().Shuffle().Take(count).ToArray();
        for (int i = 0; i < 49; i++)
            press[i] = rarr.Contains(i);
        for (int i = 0; i < 49; i++)
        {
            int adj = -1;
            for (int j = -1; j < 2; j++)
            {
                int x = (i / 7) + j;
                for (int k = -1; k < 2; k++)
                {
                    int y = (i % 7) + k;
                    if (x >= 0 && x < 7 && y >= 0 && y < 7 && press[(x * 7) + y] == press[i])
                        adj++;
                }
            }
            digits[i].text = adj.ToString();
        }
        Debug.LogFormat("[Monosweeper #{0}] The digits of the grid are:\n[Monosweeper #{0}] {1}", moduleID, string.Join("\n[Monosweeper #" + moduleID + "] ", Enumerable.Range(0, 7).Select(x => string.Join(" ", Enumerable.Range(0, 7).Select(y => digits[(x * 7) + y].text).ToArray())).ToArray()));
        int g = rarr.PickRandom();
        Press(g, true);
        int h = Enumerable.Range(0, 49).Except(rarr).PickRandom();
        Press(h, false);
        Debug.LogFormat("[Monosweeper #{0}] Clues: {1}{2} is black. {3}{4} is white. {5} total black cells.", moduleID, "ABCDEFG"[g % 7], (g / 7) + 1, "ABCDEFG"[h % 7], (h / 7) + 1, count);
        count--;
        digits[49].text = count.ToString();
        Debug.LogFormat("[Monosweeper #{0}] Soultion:\n[Monosweeper #{0}] {1}", moduleID, string.Join("\n[Monosweeper #" + moduleID + "] ", Enumerable.Range(0, 7).Select(x => string.Join(" ", Enumerable.Range(0, 7).Select(y => press[(x * 7) + y] ? "O" : "X").ToArray())).ToArray()));
        foreach(KMSelectable cell in cells)
        {
            int c = cells.IndexOf(cell);
            cell.OnInteract += delegate ()
            {
                if(!moduleSolved && !pressed[c])
                {
                    Audio.PlaySoundAtTransform("Select", cpush[c]);
                    if (press[c])
                    {
                        Press(c, true);
                        count--;
                        if (count > 0)
                            digits[49].text = count.ToString();
                        else
                        {
                            cell.AddInteractionPunch();
                            Audio.PlaySoundAtTransform("Solve", transform);
                            moduleSolved = true;
                            module.HandlePass();
                            digits[49].text = "!!!";
                            for (int i = 0; i < 49; i++)
                                if (!pressed[i])
                                    Press(i, false);
                        }
                    }
                    else
                    {
                        module.HandleStrike();
                        Press(c, false);
                    }
                }
                return false;
            };
        }
    }

    private void Press(int c, bool b)
    {
        pressed[c] = true;
        crends[c].material = wb[b ? 1 : 0];
        cpush[c].localPosition -= new Vector3(0, 0, 0.065f);
        if (b)
            digits[c].color = new Color(1, 1, 1);
    }
#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} <A-G><1-7> [Selects cell. Chain with spaces.]";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        string[] commands = command.ToUpperInvariant().Split(' ');
        List<int> p = new List<int> { };
        for(int i = 0; i < commands.Length; i++)
        {
            if(commands[i].Length != 2)
            {
                yield return "sendtochaterror!f Command \"" + commands[i] + "\" has an invalid length.";
                yield break;
            }
            int x = commands[i][0] - 'A';
            int y = commands[i][1] - '1';
            if(x < 0 || x > 6 || y < 0 || y > 6)
            {
                yield return "sendtochaterror!f \"" + commands[i] + "\" is an invalid coordinate.";
                yield break;
            }
            p.Add((y * 7) + x);
        }
        for(int i = 0; i < p.Count; i++)
        {
            cells[p[i]].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        int[] r = Enumerable.Range(0, 49).ToArray().Shuffle().ToArray();
        for (int i = 0; i < 49; i++)
        {
            int k = r[i];
            if (press[k] && !pressed[k])
            {
                cells[k].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}
