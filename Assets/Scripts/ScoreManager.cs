using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HitData {
    public float HitBeat;
    public bool WasHit;
    public bool WrongColor;
    public bool WrongDirection;
    public HitData(float hitBeat, bool wasHit, bool wrongColor, bool wrongDirection) {
        HitBeat = hitBeat;
        WasHit = wasHit;
        WrongColor = wrongColor;
        WrongDirection = wrongDirection;
    }
}

public enum HitResult {
    Perfect = 300,
    Ok = 100,
    Meh = 50,
    Miss = 0
}


public class ScoreManager : MonoBehaviour {
    public float hitWindowPerfect = 0.050f;
    public float hitWindowOk = 0.100f;
    public float hitWindowMeh = 0.150f;
    public int maxScore = 100_000;
    public float comboPow = 0.5f;
    public float accuracyPow = 3.0f;
    public TMP_Text scoreDisplay;
    public TMP_Text accuracyDisplay;
    public TMP_Text comboDisplay;

    public int combo = 0;
    public int highestCombo = 0;
    public double baseScore = 0;
    public double comboPortion = 0;
    public int judgedNotes = 0;

    private double maxComboPortion = 0;
    private int totalMapNotes = 0;
    private int bpm;

    public long totalScore = 0;
    public double accuracy = 1.0;

    public Dictionary<HitResult, int> HitStatistics = new()
    {
        { HitResult.Perfect, 0 },
        { HitResult.Ok, 0 },
        { HitResult.Meh, 0 },
        { HitResult.Miss, 0 }
    };

    public void Init(int totalNotesInSong, int _bpm) {
        totalMapNotes = totalNotesInSong;
        bpm = _bpm;

        maxComboPortion = 0;
        for (int i = 1; i <= totalNotesInSong; i++) {
            maxComboPortion += Mathf.Pow(i, comboPow);
        }
    }

    public void RegisterHit(Note note, HitData hitData) {
        judgedNotes++;

        HitResult result = JudgeHit(note, hitData);

        HitStatistics[result]++;
        int baseScoreValue = (int)result;

        if (result != HitResult.Miss) {
            combo++;
            highestCombo = Mathf.Max(highestCombo, combo);

            baseScore += baseScoreValue;
            comboPortion += Mathf.Pow(combo, comboPow);
        } else {
            combo = 0;
        }

        UpdateScore();
    }

    private HitResult JudgeHit(Note note, HitData hit) {
        if (!hit.WasHit || hit.WrongColor || hit.WrongDirection) {
            return HitResult.Miss;
        }

        float absoluteOffset = Mathf.Abs((hit.HitBeat - note.beat) * (1 / bpm));

        if (absoluteOffset <= hitWindowPerfect) return HitResult.Perfect;
        if (absoluteOffset <= hitWindowOk) return HitResult.Ok;
        if (absoluteOffset <= hitWindowMeh) return HitResult.Meh;

        return HitResult.Miss;
    }

    private void UpdateScore() {
        double currentMaxPossibleBaseScore = judgedNotes * (int)HitResult.Perfect;
        accuracy = currentMaxPossibleBaseScore > 0 ? baseScore / currentMaxPossibleBaseScore : 1.0;

        double comboProgress = maxComboPortion > 0 ? comboPortion / maxComboPortion : 1.0;
        double accuracyProgress = totalMapNotes > 0 ? (double)judgedNotes / totalMapNotes : 1.0;

        double calculatedScore =
            (maxScore / 2f * accuracy * comboProgress) +
            (maxScore / 2f * Math.Pow(accuracy, accuracyPow) * accuracyProgress);

        totalScore = (long)Math.Round(calculatedScore);

        comboDisplay.text = $"{combo}";
        accuracyDisplay.text = $"{accuracy * 100f:f2}%";
        scoreDisplay.text = $"{calculatedScore:f0}";
    }

    public long GetScore() {
        return totalScore;
    }

    public float GetAccuracy() {
        return (float)(accuracy * 100f);
    }

    public int GetMaxCombo() {
        return highestCombo;
    }

    public string GetRank() {
        float acc = GetAccuracy();

        if (acc >= 95) return "S";
        if (acc >= 90) return "A";
        if (acc >= 80) return "B";
        if (acc >= 70) return "C";
        return "D";
    }
}

