using System;
using System.Collections.Generic;
using UnityEngine;
using ArcheryGame.Scoring;
using ArcheryGame.UI;

public enum TargetDifficulty
{
    Easy,
    Medium,
    Hard
}

public class GameManager : MonoBehaviour
{
    [Header("Configurações de Dificuldade")]
    public int maxTargets = 1;
    public float currentTargetSpeed = 2f;
    public int movingTargetCount = 0;
    [SerializeField] private TargetDifficulty currentDifficulty = TargetDifficulty.Easy;

    public TargetDifficulty CurrentDifficulty => currentDifficulty;

    [Header("Pontuação")]
    [SerializeField] private int totalScore = 0;
    [SerializeField] private int totalShots = 0;
    [SerializeField] private int totalHits = 0;

    [Header("HUD")]
    [Tooltip("Mostra a pontuação no canto superior direito da visão do jogador.")]
    [SerializeField] private bool showScoreHud = true;

    private List<GameObject> activeTargets = new List<GameObject>();

    /// <summary>Fired when the score changes. Parameter is the new total score.</summary>
    public event Action<int> OnScoreChanged;

    /// <summary>Fired when shot/hit statistics change. Parameters: (totalShots, totalHits).</summary>
    public event Action<int, int> OnStatsChanged;

    /// <summary>Fired after the difficulty and target movement have been updated.</summary>
    public event Action<TargetDifficulty> OnDifficultyChanged;

    /// <summary>Current total score.</summary>
    public int TotalScore => totalScore;

    /// <summary>Total arrows fired.</summary>
    public int TotalShots => totalShots;

    /// <summary>Total arrows that hit a target.</summary>
    public int TotalHits => totalHits;

    private void Awake()
    {
        ApplyDifficultySettings(currentDifficulty, false);

        if (showScoreHud && GetComponent<ScoreHud>() == null)
        {
            gameObject.AddComponent<ScoreHud>();
        }

        if (GetComponent<WorldMenuController>() == null)
            gameObject.AddComponent<WorldMenuController>();
    }

    public bool CanPlaceTarget()
    {
        activeTargets.RemoveAll(item => item == null); 
        return activeTargets.Count < maxTargets;
    }

    public void RegisterNewTarget(GameObject newTarget)
    {
        if (newTarget == null) return;
        activeTargets.Add(newTarget);

        if (newTarget.GetComponent<TargetMovement>() == null)
            newTarget.AddComponent<TargetMovement>();

        UpdateTargetMovement();

        // Ensure the target has a collision handler for arrow scoring
        TargetCollisionHandler handler = newTarget.GetComponent<TargetCollisionHandler>();
        if (handler == null)
        {
            handler = newTarget.AddComponent<TargetCollisionHandler>();
        }
        handler.OnScored += HandleTargetScored;
    }

    public void RegisterShot()
    {
        totalShots++;
        OnStatsChanged?.Invoke(totalShots, totalHits);
    }

    public void AddScore(int points)
    {
        if (points <= 0) return;

        totalScore += points;
        OnScoreChanged?.Invoke(totalScore);
    }

    private void HandleTargetScored(int points, Vector3 hitPoint)
    {
        if (points > 0)
        {
            totalHits++;
            AddScore(points);
        }

        OnStatsChanged?.Invoke(totalShots, totalHits);
    }

    public bool IsTarget(GameObject obj)
    {
        if (obj == null) return false;
        activeTargets.RemoveAll(item => item == null);
        return activeTargets.Exists(t => t != null && (t == obj || obj.transform.IsChildOf(t.transform)));
    }

    public void RemoveTarget(GameObject target)
    {
        if (target == null) return;
        activeTargets.RemoveAll(item => item == null);

        GameObject targetToRemove = activeTargets.Find(t => t != null && (t == target || target.transform.IsChildOf(t.transform)));
        if (targetToRemove != null)
        {
            activeTargets.Remove(targetToRemove);
            Destroy(targetToRemove);
            UpdateTargetMovement();
            Debug.Log("Alvo removido com sucesso!");
        }
        else
        {
            Destroy(target);
        }
    }

    private void ApplyDifficultySettings(TargetDifficulty difficulty, bool notify)
    {
        currentDifficulty = difficulty;
        currentTargetSpeed = 2f;

        switch (difficulty)
        {
            case TargetDifficulty.Easy:
                maxTargets = 1;
                movingTargetCount = 0;
                break;
            case TargetDifficulty.Medium:
                maxTargets = 3;
                movingTargetCount = 1;
                break;
            case TargetDifficulty.Hard:
                maxTargets = 5;
                movingTargetCount = 3;
                break;
        }

        UpdateTargetMovement();
        if (notify)
            OnDifficultyChanged?.Invoke(currentDifficulty);
    }

    public void SetDifficultyEasy()
    {
        ApplyDifficultySettings(TargetDifficulty.Easy, true);
        Debug.Log("Dificuldade: FÁCIL (0 alvos em movimento)");
    }

    public void SetDifficultyMedium()
    {
        ApplyDifficultySettings(TargetDifficulty.Medium, true);
        Debug.Log("Dificuldade: MÉDIO (1 alvo em movimento)");
    }

    public void SetDifficultyHard()
    {
        ApplyDifficultySettings(TargetDifficulty.Hard, true);
        Debug.Log("Dificuldade: DIFÍCIL (3 alvos em movimento)");
    }

    public void CycleDifficulty()
    {
        switch (currentDifficulty)
        {
            case TargetDifficulty.Easy:
                SetDifficultyMedium();
                break;
            case TargetDifficulty.Medium:
                SetDifficultyHard();
                break;
            default:
                SetDifficultyEasy();
                break;
        }
    }

    private void UpdateTargetMovement()
    {
        activeTargets.RemoveAll(item => item == null);

        int movingTargetsAssigned = 0;
        foreach (GameObject target in activeTargets)
        {
            TargetMovement movement = target.GetComponent<TargetMovement>();
            if (movement == null)
                continue;

            if (movingTargetsAssigned < movingTargetCount)
            {
                movement.speed = currentTargetSpeed;
                movingTargetsAssigned++;
            }
            else
            {
                movement.speed = 0f;
            }
        }
    }

    public void DeleteAllTargets()
    {
        Debug.Log("Apagando todos os alvos...");
        ClearActiveTargets();
        ResetScoreAndStats();
    }

    public void ResetTargets()
    {
        Debug.Log("Reiniciando jogo e apagando alvos...");
        ClearActiveTargets();
        ResetScoreAndStats();
    }

    private void ResetScoreAndStats()
    {
        totalScore = 0;
        totalShots = 0;
        totalHits = 0;
        OnScoreChanged?.Invoke(totalScore);
        OnStatsChanged?.Invoke(totalShots, totalHits);
    }

    private void ClearActiveTargets()
    {
        foreach (GameObject target in activeTargets)
        {
            if (target != null)
            {
                Destroy(target);
            }
        }
        activeTargets.Clear();
        UpdateTargetMovement();
    }
}
