using System;
using System.Collections.Generic;
using ArcheryGame.Arrow;
using ArcheryGame.Scoring;
using ArcheryGame.UI;
using UnityEngine;

public enum RoundPhase
{
    Menu,
    PlacingTargets,
    Shooting,
    Result
}

public class GameManager : MonoBehaviour
{
    public const int TargetLimit = 12;
    public const int ArrowLimit = 15;
    public const int WinningScore = 100;

    [Header("Pontuação")]
    [SerializeField] private int totalScore;
    [SerializeField] private int totalShots;
    [SerializeField] private int totalHits;

    [Header("HUD")]
    [SerializeField] private bool showScoreHud = true;

    private readonly List<GameObject> activeTargets = new List<GameObject>();
    private readonly List<ArrowController> firedArrows = new List<ArrowController>();

    public event Action<int> OnScoreChanged;
    public event Action<int, int> OnStatsChanged;
    public event Action OnRoundChanged;

    public RoundPhase Phase { get; private set; } = RoundPhase.Menu;
    public int TotalScore => totalScore;
    public int TotalShots => totalShots;
    public int TotalHits => totalHits;
    public int TargetCount => activeTargets.Count;
    public int ArrowsRemaining => ArrowLimit - totalShots;
    public bool CanShoot => Phase == RoundPhase.Shooting && ArrowsRemaining > 0;

    private void Awake()
    {
        if (showScoreHud && GetComponent<ScoreHud>() == null)
            gameObject.AddComponent<ScoreHud>();
    }

    public void StartRound()
    {
        if (Phase != RoundPhase.Menu) return;

        ResetTargets();
        SetPhase(RoundPhase.PlacingTargets);
    }

    public void StartShooting()
    {
        if (Phase != RoundPhase.PlacingTargets || TargetCount == 0) return;
        SetPhase(RoundPhase.Shooting);
    }

    public void ReturnToMenu()
    {
        Phase = RoundPhase.Menu;
        ResetTargets();
        OnRoundChanged?.Invoke();
    }

    public bool CanPlaceTarget() =>
        Phase == RoundPhase.PlacingTargets && TargetCount < TargetLimit;

    public bool RegisterNewTarget(GameObject newTarget)
    {
        if (newTarget == null || !CanPlaceTarget() || activeTargets.Contains(newTarget))
            return false;

        activeTargets.Add(newTarget);
        if (!newTarget.TryGetComponent<TargetMovement>(out _))
            newTarget.AddComponent<TargetMovement>();

        if (!newTarget.TryGetComponent(out TargetCollisionHandler handler))
            handler = newTarget.AddComponent<TargetCollisionHandler>();

        handler.OnScored += HandleTargetScored;
        OnRoundChanged?.Invoke();
        return true;
    }

    public bool RegisterShot(ArrowController arrow)
    {
        if (!CanShoot || arrow == null || !arrow.IsFlying || firedArrows.Contains(arrow))
            return false;

        firedArrows.Add(arrow);
        totalShots++;
        OnStatsChanged?.Invoke(totalShots, totalHits);
        if (ArrowsRemaining == 0)
            SetPhase(RoundPhase.Result);
        else
            OnRoundChanged?.Invoke();
        return true;
    }

    public void AddScore(int points)
    {
        if (points <= 0) return;
        totalScore += points;
        OnScoreChanged?.Invoke(totalScore);
    }

    private void HandleTargetScored(int points, Vector3 hitPoint)
    {
        if (Phase != RoundPhase.Shooting || points <= 0) return;

        totalHits++;
        AddScore(points);
        OnStatsChanged?.Invoke(totalShots, totalHits);
    }

    public bool IsTarget(GameObject obj) => FindTarget(obj) != null;

    public void RemoveTarget(GameObject target)
    {
        if (Phase != RoundPhase.PlacingTargets) return;

        GameObject registeredTarget = FindTarget(target);
        if (registeredTarget == null) return;

        activeTargets.Remove(registeredTarget);
        DestroyTarget(registeredTarget);
        OnRoundChanged?.Invoke();
    }

    private GameObject FindTarget(GameObject obj)
    {
        if (obj == null) return null;
        return activeTargets.Find(target => target != null &&
            (target == obj || obj.transform.IsChildOf(target.transform)));
    }

    private void SetPhase(RoundPhase phase)
    {
        Phase = phase;
        OnRoundChanged?.Invoke();
    }

    public void ResetTargets()
    {
        foreach (ArrowController arrow in firedArrows)
            if (arrow != null) Destroy(arrow.gameObject);
        firedArrows.Clear();

        foreach (GameObject target in activeTargets)
            if (target != null) DestroyTarget(target);
        activeTargets.Clear();

        totalScore = 0;
        totalShots = 0;
        totalHits = 0;
        OnScoreChanged?.Invoke(totalScore);
        OnStatsChanged?.Invoke(totalShots, totalHits);
    }

    private void DestroyTarget(GameObject target)
    {
        if (target.TryGetComponent(out TargetCollisionHandler handler))
            handler.OnScored -= HandleTargetScored;

        target.SetActive(false);
        Destroy(target);
    }
}
