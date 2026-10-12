using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Configurações de Dificuldade")]
    public int maxTargets = 5; // Inicia com limite de 5 para o editor
    public float currentTargetSpeed = 0f;

    private List<GameObject> activeTargets = new List<GameObject>();

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
    }
}