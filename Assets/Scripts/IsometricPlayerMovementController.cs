using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class IsometricPlayerMovementController : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 5f;
    public float acceleration = 20f;
    public float deceleration = 25f;

    private Rigidbody2D rbody;
    private IsometricCharacterRenderer isoRenderer;

    private Vector2 currentVelocity;

    private void Awake()
    {
        rbody = GetComponent<Rigidbody2D>();
        isoRenderer = GetComponentInChildren<IsometricCharacterRenderer>();
    }

    private void FixedUpdate()
    {
        // --- Input ---
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // Normalize first, then rotate input mapping by 45 degrees:
        // W=up-right, D=down-right, S=down-left, A=up-left.
        // Combos become cardinals (WD=right, WA=up, SA=left, SD=down).
        Vector2 input = new Vector2(h, v);
        input = Vector2.ClampMagnitude(input, 1f);

        Vector2 rotated = new Vector2(
            input.x + input.y,
            input.y - input.x
        );

        Vector2 isoInput = new Vector2(
            rotated.x - rotated.y,
            (rotated.x + rotated.y) * 0.5f
        );
        isoInput = isoInput.normalized * input.magnitude;

        // --- Target velocity ---
        Vector2 targetVelocity = isoInput * maxSpeed;

        // --- Acceleration / Deceleration ---
        // if (isoInput.magnitude > 0.01f)
        // {
        //     currentVelocity = Vector2.MoveTowards(
        //         currentVelocity,
        //         targetVelocity,
        //         acceleration * Time.fixedDeltaTime
        //     );
        // }
        // else
        // {
        //     currentVelocity = Vector2.MoveTowards(
        //         currentVelocity,
        //         Vector2.zero,
        //         deceleration * Time.fixedDeltaTime
        //     );
        // }

        // --- Move ---
        rbody.MovePosition(rbody.position + currentVelocity * Time.fixedDeltaTime);

        // --- Direction for visuals ---
        if (isoRenderer != null)
        {
            isoRenderer.SetDirection(currentVelocity);
        }
    }
}