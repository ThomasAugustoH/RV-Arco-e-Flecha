using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUIController : MonoBehaviour
{
    [Header("Referências")]
    public GameManager gameManager;
    public TargetSetter targetSetter;

    [Header("Painéis da interface")]
    public GameObject difficultyPanel;
    public GameObject gameplayPanel;

    private void Start()
    {
        if (difficultyPanel != null)
            difficultyPanel.SetActive(true);
        if (gameplayPanel != null)
            gameplayPanel.SetActive(false);

        if (targetSetter != null)
            targetSetter.canPlaceTargets = true;
    }

    public void ChooseEasy()
    {
        if (gameManager == null) return;

        gameManager.SetDifficultyEasy();
        StartGameplay();
    }

    public void ChooseMedium()
    {
        if (gameManager == null) return;

        gameManager.SetDifficultyMedium();
        StartGameplay();
    }

    public void ChooseHard()
    {
        if (gameManager == null) return;

        gameManager.SetDifficultyHard();
        StartGameplay();
    }

    private void StartGameplay()
    {
        difficultyPanel.SetActive(false);
        gameplayPanel.SetActive(true);

        if (targetSetter != null)
        {
            // Evita que o clique no menu também
            // seja interpretado como colocação de alvo.
            targetSetter.canPlaceTargets = false;
            StartCoroutine(EnableTargetPlacementNextFrame());
        }

        Debug.Log("Partida iniciada.");
    }

    private IEnumerator EnableTargetPlacementNextFrame()
    {
        yield return null;

        if (targetSetter != null)
            targetSetter.canPlaceTargets = true;
    }

    public void ResetTargets()
    {
        if (gameManager == null) return;

        gameManager.ResetTargets();
    }

    public void ResetScenario()
    {
        // Reinicia a cena inteira, voltando à seleção
        // de dificuldade e ao estado inicial dos objetos.
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }
}