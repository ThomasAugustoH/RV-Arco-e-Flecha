using UnityEngine;

public class TargetMovement : MonoBehaviour
{
    [Header("Parâmetros de Movimento")]
    public float speed = 0f; // Recebe o valor do GameManager
    public float movementDistance = 1.5f; // Quão longe ele vai para os lados
    
    private Vector3 startPosition;

    void Start()
    {
        // Memoriza o ponto exato onde o alvo nasceu na parede/ambiente
        startPosition = transform.position;
    }

    void Update()
    {
        // Se a velocidade for 0 (dificuldade Easy), interrompe a lógica e economiza processamento
        if (speed <= 0f) return;

        // Cria uma onda suave entre -1 e 1 baseada no tempo
        float offset = Mathf.Sin(Time.time * speed) * movementDistance;
        
        // Move o alvo no próprio eixo lateral (direita/esquerda). 
        // Se quiser que o alvo suba e desça, mude transform.right para transform.up
        transform.position = startPosition + (transform.right * offset);
    }
}