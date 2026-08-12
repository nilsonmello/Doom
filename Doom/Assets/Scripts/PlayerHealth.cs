using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Escudo")]
    public PlayerGrabController grabController;

    void Start()
    {
        currentHealth = maxHealth;

        if (grabController == null)
            grabController = GetComponent<PlayerGrabController>();
    }

    public void TakeDamage(int amount, Vector3? sourcePosition)
    {
        if (grabController != null && grabController.TryAbsorbDamage(amount, sourcePosition))
            return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);

        if (currentHealth <= 0)
            Die();
    }

    public void TakeDamage(int amount)
    {
        TakeDamage(amount, null);
    }

    private void Die()
    {
        Debug.Log("Player morreu");
    }
}