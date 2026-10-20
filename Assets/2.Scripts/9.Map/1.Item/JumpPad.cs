using UnityEngine;
using Fusion;

public class JumpPad : MonoBehaviour
{
    public float jumpForce = 15f;
    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponentInParent<PlayerController2D>();
            if (player != null)
            {
                player.Jump(jumpForce);
            }
        }
    }
}
