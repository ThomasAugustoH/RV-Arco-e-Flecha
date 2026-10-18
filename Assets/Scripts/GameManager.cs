using System;
using System.Collections.Generic;
using UnityEngine;
using ArcheryGame.Scoring;

public class GameManager : MonoBehaviour
{
    [Header("Configurações de Dificuldade")]
    public int maxTargets = 5; // Inicia com limite de 5 para o editor
    public float currentTargetSpeed = 0f;

    [Header("Pontuação")]
    [SerializeField] private int totalScore = 0;
    [SerializeField] private int totalShots = 0;
    [SerializeField] private int totalHits = 0;

    private List<GameObject> activeTargets = new List<GameObject>();

    /// <summary>Fired when the score changes. Parameter is the new total score.</summary>
    public event Action<int> OnScoreChanged;

    /// <summary>Fired when shot/hit statistics change. Parameters: (totalShots, totalHits).</summary>
    public event Action<int, int> OnStatsChanged;

    /// <summary>Current total score.</summary>
    public int TotalScore => totalScore;

    /// <summary>Total arrows fired.</summary>
    public int TotalShots => totalShots;

    /// <summary>Total arrows that hit a target.</summary>
    public int TotalHits => totalHits;

    public bool CanPlaceTarget()
    {
        activeTargets.RemoveAll(item => item == null); 
        return activeTargets.Count < maxTargets;
    }

    public void RegisterNewTarget(GameObject newTarget)
    {
        if (newTarget == null) return;
        activeTargets.Add(newTarget);

        TargetMovement movement = newTarget.GetComponent<TargetMovement>();
        if (movement == null)
        {
            movement = newTarget.AddComponent<TargetMovement>();
        }
        movement.speed = currentTargetSpeed;

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
            Debug.Log("Alvo removido com sucesso!");
        }
        else
        {
            Destroy(target);
        }
    }

    // Trava que verifica se o jogo já começou
    private bool CanChangeDifficulty()
    {
        activeTargets.RemoveAll(item => item == null);
        
        if (activeTargets.Count > 0)
        {
            Debug.LogWarning("Jogo em andamento! Use o Reset antes de trocar a dificuldade.");
            return false; // Bloqueia a mudança
        }
        
        return true; // Libera a mudança
    }

    public void SetDifficultyEasy()
    {
        if (!CanChangeDifficulty()) return;
        
        Debug.Log("Dificuldade: FACIL (1 Alvo, Parado)");
        maxTargets = 1;
        currentTargetSpeed = 0f;
    }

    public void SetDifficultyMedium()
    {
        if (!CanChangeDifficulty()) return;
        
        Debug.Log("Dificuldade: MEDIO (3 Alvos, Parados)");
        maxTargets = 3;
        currentTargetSpeed = 0f;
    }

    public void SetDifficultyHard()
    {
        if (!CanChangeDifficulty()) return;
        
        Debug.Log("Dificuldade: DIFICIL (5 Alvos, Movimento)");
        maxTargets = 5;
        currentTargetSpeed = 2f; // Apenas os alvos criados no modo Hard terão movimento
    }

    public void ResetTargets()
    {
        Debug.Log("Reiniciando jogo e apagando alvos...");
        foreach (GameObject target in activeTargets)
        {
            if (target != null)
            {
                Destroy(target);
            }
        }
        activeTargets.Clear();

        totalScore = 0;
        totalShots = 0;
        totalHits = 0;
        OnScoreChanged?.Invoke(totalScore);
        OnStatsChanged?.Invoke(totalShots, totalHits);
    }
}