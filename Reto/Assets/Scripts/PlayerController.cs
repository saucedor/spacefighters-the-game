using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody rb;
    private Animator animator;

    private Vector3 input;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        rb.freezeRotation = true; // Para que no se caiga al moverlo
    }

    void Update()
    {
        // Obtener input de teclas (WASD / Flechas)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        input = new Vector3(h, 0, v).normalized;

        // Actualizar parámetro Speed en el Animator
        animator.SetFloat("Speed", input.magnitude);
    }

    void FixedUpdate()
    {
        // Mover con física
        rb.MovePosition(rb.position + input * moveSpeed * Time.fixedDeltaTime);

        // Rotar hacia la dirección de movimiento
        if (input.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(input, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 0.15f));
        }
    }
}
