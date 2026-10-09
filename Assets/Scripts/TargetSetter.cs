using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    public GameObject targetPrefab;
    public Transform rightController;

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger))
        {
            RaycastHit hit;
            Vector3 targetPosition;
            Quaternion targetRotation; // Criamos uma variável para guardar a rotação

            if (Physics.Raycast(rightController.position, rightController.forward, out hit, 10f))
            {
                targetPosition = hit.point;
                // hit.normal é a direção para onde a parede está "olhando"
                // Isso faz a parte de cima do cilindro (Vector3.up) colar certinho na parede ou chão
                targetRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            }
            else
            {
                targetPosition = rightController.position + (rightController.forward * 2f);
                // Se atirar no vazio, faz o alvo virar o rosto (-forward) de volta para o controle
                targetRotation = Quaternion.FromToRotation(Vector3.up, -rightController.forward);
            }

            // Agora instanciamos passando a rotação que calculamos
            Instantiate(targetPrefab, targetPosition, targetRotation);
        }
    }
}
